using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows;
using PCL.Core.App;
using PCL.Core.Conduit;
using PCL.Core.Minecraft.Profile;

namespace PCL;

/// <summary>社团更新日志的一条可显示记录。</summary>
public sealed record ClubUpdateHistoryItem(ClubUpdateNotice Notice, string? Body, bool IsCached)
{
    public string HeaderText => $"{Notice.DateText} · {Notice.Title}" + (IsCached ? " · 缓存" : "");

    public string BodyText => string.IsNullOrWhiteSpace(Body)
        ? "正文暂时不可用，请打开原文查看。"
        : Body;

    public string SourceText => IsCached ? "本地缓存" : "已从社团网站刷新";
}

public static class ClubUpdates
{
    private const int MaxNoticeResponseBytes = 1024 * 1024;
    private const int MaxDetailResponseBytes = 512 * 1024;
    private const int MaxHistoryEntries = 64;
    private const int DetailConcurrency = 3;
    private static readonly HttpClient Client = CreateClient();
    private static readonly HttpClient NoticeClient = CreateNoticeClient();
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);
    private static readonly object HistoryWindowLock = new();
    private static int _startupNoticeCheckStarted;
    private static int _historyRefreshRunning;
    private static ClubUpdateHistoryWindow? _historyWindow;
    private static IReadOnlyList<ClubUpdateHistoryItem> _history = Array.Empty<ClubUpdateHistoryItem>();
    public static ClubRelease Latest { get; private set; }

    public static IReadOnlyList<ClubUpdateHistoryItem> History => _history;

    private static string HistoryCachePath => Path.Combine(ModBase.pathTemp, "Cache", "WzbcUpdateNotices.json");

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("WZBC-Launcher/" + ClubRelease.CurrentVersion);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private static HttpClient CreateNoticeClient()
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(8)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("WZBC-Launcher/" + ClubRelease.CurrentVersion);
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        return client;
    }

    public static async Task<ClubRelease> CheckAsync()
    {
        if (string.IsNullOrWhiteSpace(ClubCatalog.ReleaseRepository)) return Latest = null;
        var repository = new Uri(ClubCatalog.ReleaseRepository).AbsolutePath.Trim('/');
        var json = await Client.GetStringAsync($"https://api.github.com/repos/{repository}/releases?per_page=30");
        return Latest = ClubRelease.ParseLatest(json);
    }

    /// <summary>
    /// 启动后后台检查一次社团更新日志。无论通知开关是否开启，索引和正文都会进入本地缓存；关闭开关只禁止自动弹窗。
    /// </summary>
    public static void CheckStartupNotice()
    {
        if (!ClubCatalog.UpdateNoticesReady || Interlocked.Exchange(ref _startupNoticeCheckStarted, 1) != 0)
            return;

        try
        {
            var result = RefreshHistoryAsync().GetAwaiter().GetResult();
            var latest = result.Latest;
            if (!result.IndexFetched || !Config.Preference.ClubUpdateNoticeEnabled || latest is null)
                return;

            var lastDate = ParseAcknowledgedDate(States.System.ClubUpdateNoticeLastId);
            if (!ClubUpdateNoticeParser.ShouldNotify(latest, lastDate))
                return;

            // Fetch runs off the UI thread. The final check and dialog enqueue below run on the UI
            // thread so a login, launch or another modal prompt can never be interrupted by this notice.
            ModBase.RunInUi(() => PresentNoticeIfStillAvailable(latest));
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[Conduit] 更新日志检查失败（下次启动重试）", ModBase.LogLevel.Debug);
        }
    }

    /// <summary>显示已缓存的更新历史，并在窗口打开后异步刷新正文。</summary>
    public static void ShowHistory()
    {
        if (!ClubCatalog.UpdateNoticesReady)
        {
            ModMain.MyMsgBox("WZBC 更新日志源尚未接入。社团动态请查看官网。", "社团更新日志");
            return;
        }
        if (ModBase.isProgramEnded)
            return;

        ModBase.RunInUi(() =>
        {
            lock (HistoryWindowLock)
            {
                if (_historyWindow is { Parent: not null } existing)
                {
                    existing.Focus();
                    return;
                }
                if (ModMain.WaitingMyMsgBox.Any(item => item.Type == ModMain.MyMsgBoxType.ClubUpdateHistory))
                    return;

                ModMain.WaitingMyMsgBox.Add(new ModMain.MyMsgBoxConverter
                {
                    Type = ModMain.MyMsgBoxType.ClubUpdateHistory,
                    Button1 = "关闭",
                    ForceWait = false,
                    Title = "社团更新日志"
                });
                ModMain.MyMsgBoxTick();
            }
        });
    }

    internal static void RegisterHistoryWindow(ClubUpdateHistoryWindow window)
    {
        lock (HistoryWindowLock)
            _historyWindow = window;
    }

    internal static void HistoryWindowLoaded(ClubUpdateHistoryWindow window)
    {
        if (!IsCurrentHistoryWindow(window))
            return;
        MarkHistoryDisplayed(_history);
        _ = LoadHistoryCacheForWindowAsync(window);
        _ = RefreshHistoryForWindowAsync(window);
    }

    internal static void UnregisterHistoryWindow(ClubUpdateHistoryWindow window)
    {
        lock (HistoryWindowLock)
        {
            if (ReferenceEquals(_historyWindow, window))
                _historyWindow = null;
        }
    }

    /// <summary>由历史窗口的刷新按钮调用；关闭窗口后完成的网络任务不会再写入已销毁的 UI。</summary>
    public static void RefreshHistory(ClubUpdateHistoryWindow window)
    {
        if (window is null)
            return;
        _ = RefreshHistoryForWindowAsync(window);
    }

    private static async Task RefreshHistoryForWindowAsync(ClubUpdateHistoryWindow window)
    {
        if (!IsCurrentHistoryWindow(window) || Interlocked.CompareExchange(ref _historyRefreshRunning, 1, 0) != 0)
            return;

        try
        {
            SetWindowRefreshing(window, true, "刷新中……");
            var result = await RefreshHistoryAsync().ConfigureAwait(false);
            ModBase.RunInUi(() =>
            {
                if (!IsCurrentHistoryWindow(window))
                    return;
                window.SetHistory(result.Items, result.IndexFetched
                    ? "已刷新 " + DateTime.Now.ToString("HH:mm")
                    : "网络不可用 · 本地缓存" + (result.Items.Count == 0 ? " · 暂无记录" : ""));
                MarkHistoryDisplayed(result.Items);
            });
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[Conduit] 历史更新日志刷新失败", ModBase.LogLevel.Debug);
            ModBase.RunInUi(() =>
            {
                if (IsCurrentHistoryWindow(window))
                {
                    window.SetHistory(_history, "网络不可用 · 本地缓存" + (_history.Count == 0 ? " · 暂无记录" : ""));
                    MarkHistoryDisplayed(_history);
                }
            });
        }
        finally
        {
            Interlocked.Exchange(ref _historyRefreshRunning, 0);
            SetWindowRefreshing(window, false, null);
            // A window can be closed and reopened while the old request is in flight. Once the
            // shared flight is released, let the new visible window start its own refresh instead
            // of leaving it permanently on the initial "后台刷新中" state.
            ModBase.RunInUi(() =>
            {
                ClubUpdateHistoryWindow? current;
                lock (HistoryWindowLock)
                    current = !ReferenceEquals(_historyWindow, window) && _historyWindow is { IsVisible: true }
                        ? _historyWindow
                        : null;
                if (current is not null)
                    _ = RefreshHistoryForWindowAsync(current);
            });
        }
    }

    private static async Task LoadHistoryCacheForWindowAsync(ClubUpdateHistoryWindow window)
    {
        try
        {
            var cached = await LoadHistoryCacheAsync().ConfigureAwait(false);
            if (cached.Count == 0)
                return;
            ModBase.RunInUi(() =>
            {
                if (IsCurrentHistoryWindow(window) && _history.Count == 0)
                {
                    _history = cached;
                    window.SetHistory(cached, "显示缓存，刷新中……");
                    MarkHistoryDisplayed(cached);
                }
            });
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[Conduit] 预加载更新日志缓存失败", ModBase.LogLevel.Debug);
        }
    }

    private static void SetWindowRefreshing(ClubUpdateHistoryWindow window, bool isRefreshing, string? status)
        => ModBase.RunInUi(() =>
        {
            if (IsCurrentHistoryWindow(window))
            {
                if (status is not null)
                    window.SetStatus(status);
                window.SetRefreshing(isRefreshing);
            }
        });

    private static bool IsCurrentHistoryWindow(ClubUpdateHistoryWindow window)
    {
        lock (HistoryWindowLock)
            return ReferenceEquals(_historyWindow, window) && window.IsVisible;
    }

    private static void MarkHistoryDisplayed(IReadOnlyList<ClubUpdateHistoryItem> items)
    {
        var latest = items.OrderByDescending(item => item.Notice.Date).FirstOrDefault();
        if (latest is null)
            return;

        var oldDate = ParseAcknowledgedDate(States.System.ClubUpdateNoticeLastId);
        if (!oldDate.HasValue || latest.Notice.Date > oldDate.Value)
            States.System.ClubUpdateNoticeLastId = latest.Notice.Id;
    }

    private static async Task<HistoryRefreshResult> RefreshHistoryAsync()
    {
        if (!ClubCatalog.UpdateNoticesReady) return new HistoryRefreshResult([], false, null);
        await RefreshLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var cached = await LoadHistoryCacheAsync().ConfigureAwait(false);
            if (cached.Count == 0 && _history.Count > 0)
                cached = _history;
            var cachedById = cached.ToDictionary(item => item.Notice.Id, StringComparer.Ordinal);
            string indexHtml;
            try
            {
                indexHtml = await DownloadHtmlAsync(new Uri(ClubUpdateNoticeParser.IndexUrl), MaxNoticeResponseBytes)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, "[Conduit] 更新日志索引下载失败，继续使用缓存", ModBase.LogLevel.Debug);
                var fallback = cached.Select(item => item with { IsCached = true }).ToArray();
                _history = fallback;
                return new HistoryRefreshResult(fallback, false, null);
            }

            var notices = ClubUpdateNoticeParser.Parse(indexHtml, DateOnly.FromDateTime(DateTime.Now))
                .Take(MaxHistoryEntries)
                .ToArray();
            if (notices.Length == 0)
            {
                ModBase.Log("[Conduit] 更新日志索引没有有效日期记录，继续使用缓存", ModBase.LogLevel.Debug);
                var fallback = cached.Select(item => item with { IsCached = true }).ToArray();
                _history = fallback;
                return new HistoryRefreshResult(fallback, false, null);
            }

            using var detailGate = new SemaphoreSlim(DetailConcurrency, DetailConcurrency);
            var detailTasks = notices.Select(async notice =>
            {
                await detailGate.WaitAsync().ConfigureAwait(false);
                try
                {
                    var detailHtml = await DownloadHtmlAsync(notice.Url, MaxDetailResponseBytes).ConfigureAwait(false);
                    return ClubUpdateNoticeParser.ParseArticle(detailHtml, notice);
                }
                catch (Exception ex)
                {
                    ModBase.Log(ex, $"[Conduit] 更新日志正文下载失败：{notice.Id}", ModBase.LogLevel.Debug);
                    return null;
                }
                finally
                {
                    detailGate.Release();
                }
            }).ToArray();
            var details = await Task.WhenAll(detailTasks).ConfigureAwait(false);
            var merged = new List<ClubUpdateHistoryItem>(notices.Length + cached.Count);
            for (var i = 0; i < notices.Length; i++)
            {
                var detail = details[i];
                if (detail is not null)
                {
                    merged.Add(new ClubUpdateHistoryItem(notices[i], detail.Body, false));
                }
                else if (cachedById.TryGetValue(notices[i].Id, out var old) && !string.IsNullOrWhiteSpace(old.Body))
                {
                    merged.Add(old with { Notice = notices[i], IsCached = true });
                }
                else
                {
                    merged.Add(new ClubUpdateHistoryItem(notices[i], null, false));
                }
            }

            // 保留网站暂时未列出的旧正文，避免一次网络异常或站点整理让用户失去可读缓存。
            var currentIds = merged.Select(item => item.Notice.Id).ToHashSet(StringComparer.Ordinal);
            merged.AddRange(cached.Where(item => !currentIds.Contains(item.Notice.Id))
                .Select(item => item with { IsCached = true }));
            merged.Sort((left, right) => right.Notice.Date.CompareTo(left.Notice.Date));
            var result = merged.ToArray();
            _history = result;
            await SaveHistoryCacheAsync(result).ConfigureAwait(false);
            return new HistoryRefreshResult(result, true, notices[0]);
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private static async Task<string> DownloadHtmlAsync(Uri uri, int maxBytes)
    {
        if (!Uri.TryCreate(ClubUpdateNoticeParser.IndexUrl, UriKind.Absolute, out var indexUri) ||
            !Uri.TryCreate(uri.ToString(), UriKind.Absolute, out var requestedUri))
            throw new InvalidDataException("更新日志链接不是有效的绝对 URL");

        var isIndex = string.Equals(requestedUri.AbsoluteUri, indexUri.AbsoluteUri, StringComparison.Ordinal);
        Uri trustedUri;
        if (isIndex)
            trustedUri = indexUri;
        else if (!ClubUpdateNoticeParser.TryGetTrustedArticle(requestedUri.ToString(), out _, out trustedUri))
            throw new InvalidDataException("更新日志链接不在受信任的社团网站内");

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        using var response = await NoticeClient.GetAsync(trustedUri, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > maxBytes)
            throw new InvalidDataException($"社团更新日志页面超过 {maxBytes} 字节");

        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        using var content = new MemoryStream();
        var buffer = new byte[8192];
        var total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), timeout.Token).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > maxBytes)
                throw new InvalidDataException($"社团更新日志页面超过 {maxBytes} 字节");
            await content.WriteAsync(buffer.AsMemory(0, read), timeout.Token).ConfigureAwait(false);
        }

        return Encoding.UTF8.GetString(content.ToArray());
    }

    private static async Task<IReadOnlyList<ClubUpdateHistoryItem>> LoadHistoryCacheAsync()
    {
        try
        {
            if (!File.Exists(HistoryCachePath) || new FileInfo(HistoryCachePath).Length > 4 * MaxNoticeResponseBytes)
                return Array.Empty<ClubUpdateHistoryItem>();

            var json = await File.ReadAllTextAsync(HistoryCachePath).ConfigureAwait(false);
            var cache = JsonSerializer.Deserialize<HistoryCache>(json);
            if (cache?.Items is null)
                return Array.Empty<ClubUpdateHistoryItem>();

            var resultById = new Dictionary<string, ClubUpdateHistoryItem>(StringComparer.Ordinal);
            foreach (var item in cache.Items)
            {
                if (!DateOnly.TryParseExact(item.Id, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var date) ||
                    !ClubUpdateNoticeParser.TryGetTrustedArticle(item.Url, out var urlDate, out var url) ||
                    urlDate != date || string.IsNullOrWhiteSpace(item.Title) || item.Body?.Length > 120_000)
                    continue;
                var notice = new ClubUpdateNotice(date, item.Title, url);
                resultById.TryAdd(notice.Id, new ClubUpdateHistoryItem(notice, item.Body, true));
            }
            return resultById.Values.OrderByDescending(item => item.Notice.Date).ToArray();
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[Conduit] 读取更新日志缓存失败", ModBase.LogLevel.Debug);
            return Array.Empty<ClubUpdateHistoryItem>();
        }
    }

    private static async Task SaveHistoryCacheAsync(IReadOnlyList<ClubUpdateHistoryItem> items)
    {
        try
        {
            var directory = Path.GetDirectoryName(HistoryCachePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            var cache = new HistoryCache
            {
                Items = items.Select(item => new CachedNotice
                {
                    Id = item.Notice.Id,
                    Title = item.Notice.Title,
                    Url = item.Notice.Url.ToString(),
                    Body = item.Body
                }).ToList()
            };
            var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = false });
            var tempPath = HistoryCachePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, Encoding.UTF8).ConfigureAwait(false);
            File.Move(tempPath, HistoryCachePath, true);
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[Conduit] 保存更新日志缓存失败", ModBase.LogLevel.Debug);
        }
    }

    private static DateOnly? ParseAcknowledgedDate(string? value)
        => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date) ? date : null;

    private static bool IsSensitiveOperationActive()
    {
        var main = ModMain.frmMain;
        var visibleMessage = main is not null && main.PanMsg.Children.Count > 0 &&
                             main.PanMsgBackground.Visibility != Visibility.Collapsed;
        return ModBase.isProgramEnded || ModLaunch.isLaunching || ProfileService.IsCreatingProfile ||
               ModMain.frmLoginMs?.IsAuthenticating == true || ModMain.frmLoginAuth?.IsAuthenticating == true ||
               ModMain.WaitingMyMsgBox.Count > 0 || visibleMessage;
    }

    private static void PresentNoticeIfStillAvailable(ClubUpdateNotice notice)
    {
        try
        {
            if (!Config.Preference.ClubUpdateNoticeEnabled || IsSensitiveOperationActive())
            {
                ModBase.Log("[Conduit] 更新日志已发现，但展示前检测到登录、启动或其他弹窗，留待下次启动", ModBase.LogLevel.Debug);
                return;
            }

            var currentDate = ParseAcknowledgedDate(States.System.ClubUpdateNoticeLastId);
            if (!ClubUpdateNoticeParser.ShouldNotify(notice, currentDate))
                return;

            // The checks above establish that the dialog can be presented. Mark it before entering
            // the modal wait so closing the launcher while it is visible cannot show it again next
            // time. If enqueueing or displaying the dialog fails, restore the previous value.
            var previousId = States.System.ClubUpdateNoticeLastId;
            int result;
            try
            {
                States.System.ClubUpdateNoticeLastId = notice.Id;
                result = ModMain.MyMsgBox(
                    $"日期：{notice.DateText}\r\n\r\n{notice.Title}\r\n\r\n可点击“查看日志”阅读全部社团更新记录。",
                    "发现社团更新日志",
                    "查看日志",
                    "稍后");
            }
            catch
            {
                States.System.ClubUpdateNoticeLastId = previousId;
                throw;
            }

            // The standard button closes the notice before this callback queues the history panel.
            if (result == 1)
                ShowHistory();
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[Conduit] 展示更新日志提示失败（下次启动重试）", ModBase.LogLevel.Debug);
        }
    }

    private sealed class HistoryCache
    {
        public List<CachedNotice> Items { get; set; } = [];
    }

    private sealed class CachedNotice
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public string? Body { get; set; }
    }

    private sealed record HistoryRefreshResult(
        IReadOnlyList<ClubUpdateHistoryItem> Items,
        bool IndexFetched,
        ClubUpdateNotice? Latest);
}
