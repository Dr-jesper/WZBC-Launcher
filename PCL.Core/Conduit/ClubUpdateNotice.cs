using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace PCL.Core.Conduit;

/// <summary>社团网站更新日志索引中的一条日期记录。</summary>
public sealed record ClubUpdateNotice(DateOnly Date, string Title, Uri Url)
{
    /// <summary>用于持久化去重的稳定标识；目前由日期文章路径提供。</summary>
    public string Id => Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public string DateText => Date.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture);
}

/// <summary>社团更新日志文章中可安全展示的纯文本正文。</summary>
public sealed record ClubUpdateArticle(ClubUpdateNotice Notice, string Body);

/// <summary>
/// 解析社团更新日志总览页。解析器只读取 article 内的日期文章链接，绝不执行或呈现远程 HTML。
/// </summary>
public static class ClubUpdateNoticeParser
{
    public const string IndexUrl = "https://wzbcmc.cn/updates/";
    public const string TrustedHost = "wzbcmc.cn";
    private const int MaxTitleLength = 200;
    private const int MaxBodyLength = 120_000;
    private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromMilliseconds(250);

    private static readonly Regex ArticleRegex = new(
        @"<article\b[^>]*>(?<content>.*?)</article\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexMatchTimeout);

    private static readonly Regex AnchorRegex = new(
        @"<a\b(?<attributes>[^>]*)>(?<text>.*?)</a\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexMatchTimeout);

    private static readonly Regex HrefRegex = new(
        @"\bhref\s*=\s*(?:""(?<double>[^""]*)""|'(?<single>[^']*)'|(?<bare>[^\s>]+))",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexMatchTimeout);

    private static readonly Regex ScriptOrStyleRegex = new(
        @"<(?:script|style)\b[^>]*>.*?</(?:script|style)\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexMatchTimeout);

    private static readonly Regex TagRegex = new(
        @"<[^>]*>", RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexMatchTimeout);

    private static readonly Regex WhitespaceRegex = new(
        @"\s+", RegexOptions.CultureInvariant, RegexMatchTimeout);

    private static readonly Regex DatePrefixRegex = new(
        @"^\d{4}[./-]\d{1,2}[./-]\d{1,2}\s*(?:[·•|:：\-–—]\s*)?",
        RegexOptions.CultureInvariant, RegexMatchTimeout);

    private static readonly Regex NavigationRegex = new(
        @"<(?:nav|div)\b[^>]*(?:class\s*=\s*(?:""[^""]*(?:breadcrumbs|tocCollapsible)[^""]*""|'[^']*(?:breadcrumbs|tocCollapsible)[^']*'))[^>]*>.*?</(?:nav|div)\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexMatchTimeout);

    private static readonly Regex MarkdownContainerRegex = new(
        @"<div\b[^>]*class\s*=\s*(?:""[^""]*\btheme-doc-markdown\b[^""]*""|'[^']*\btheme-doc-markdown\b[^']*')[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexMatchTimeout);

    private static readonly Regex HeaderRegex = new(
        @"<header\b[^>]*>.*?</header\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexMatchTimeout);

    private static readonly Regex CommentRegex = new(
        @"<!--.*?-->", RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexMatchTimeout);

    private static readonly Regex BlockTagRegex = new(
        @"<(?:br\b[^>]*>|/p\s*>|/li\s*>|/h[1-6]\s*>|/pre\s*>|/tr\s*>|/table\s*>|/ul\s*>|/ol\s*>|/blockquote\s*>|/div\s*>)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexMatchTimeout);

    private static readonly Regex ListItemRegex = new(
        @"<li\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, RegexMatchTimeout);

    /// <summary>从正文 HTML 中提取有效、去重并按日期倒序排列的记录。</summary>
    /// <param name="html">更新日志总览页的 HTML。</param>
    /// <param name="today">用于排除未来日期；省略时使用本机当前日期。</param>
    public static IReadOnlyList<ClubUpdateNotice> Parse(string? html, DateOnly? today = null)
    {
        try
        {
            return ParseCore(html, today);
        }
        catch (RegexMatchTimeoutException)
        {
            // Remote HTML is untrusted input. A pathological page should behave like a
            // malformed page and leave the next startup free to retry.
            return Array.Empty<ClubUpdateNotice>();
        }
    }

    private static IReadOnlyList<ClubUpdateNotice> ParseCore(string? html, DateOnly? today)
    {
        if (string.IsNullOrWhiteSpace(html))
            return Array.Empty<ClubUpdateNotice>();

        var article = ArticleRegex.Match(html).Groups["content"].Value;
        if (string.IsNullOrEmpty(article))
            return Array.Empty<ClubUpdateNotice>();

        var latestDate = today ?? DateOnly.FromDateTime(DateTime.Now);
        var notices = new Dictionary<DateOnly, ClubUpdateNotice>();
        foreach (Match anchor in AnchorRegex.Matches(article))
        {
            var hrefMatch = HrefRegex.Match(anchor.Groups["attributes"].Value);
            if (!hrefMatch.Success)
                continue;

            var href = hrefMatch.Groups["double"].Success
                ? hrefMatch.Groups["double"].Value
                : hrefMatch.Groups["single"].Success
                    ? hrefMatch.Groups["single"].Value
                    : hrefMatch.Groups["bare"].Value;
            if (!TryGetTrustedArticle(href, out var date, out var url) || date > latestDate)
                continue;

            var title = CleanText(anchor.Groups["text"].Value);
            if (title.Length == 0)
                continue;
            title = DatePrefixRegex.Replace(title, string.Empty).Trim();
            if (title.Length == 0)
                continue;
            if (title.Length > MaxTitleLength)
                title = title[..(MaxTitleLength - 1)].TrimEnd() + "…";

            // Docusaurus can expose the same article in more than one index entry. The first
            // valid title is stable enough for display; notification identity remains the date.
            notices.TryAdd(date, new ClubUpdateNotice(date, title, url));
        }

        return notices.Values.OrderByDescending(item => item.Date).ToArray();
    }

    /// <summary>取得总览页中应关注的最新记录；历史记录不会逐条补弹。</summary>
    public static ClubUpdateNotice? ParseLatest(string? html, DateOnly? today = null)
        => Parse(html, today).FirstOrDefault();

    /// <summary>
    /// 从受信任日期文章中提取可显示的纯文本。远程 HTML 只会被去标签和解码，绝不交给 WPF/XAML 执行。
    /// </summary>
    public static ClubUpdateArticle? ParseArticle(string? html, ClubUpdateNotice notice)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(html))
                return null;

            var article = ArticleRegex.Match(html).Groups["content"].Value;
            if (string.IsNullOrWhiteSpace(article))
                return null;

            article = NavigationRegex.Replace(article, "\n");
            var markdownContainer = MarkdownContainerRegex.Match(article);
            if (markdownContainer.Success)
                article = article[(markdownContainer.Index + markdownContainer.Length)..];

            article = HeaderRegex.Replace(article, "\n");
            article = CommentRegex.Replace(article, "\n");
            article = ScriptOrStyleRegex.Replace(article, "\n");
            article = ListItemRegex.Replace(article, "\n• ");
            article = BlockTagRegex.Replace(article, "\n");
            article = TagRegex.Replace(article, " ");
            article = WebUtility.HtmlDecode(article)
                .Replace('\u00a0', ' ')
                .Replace('\u200b', ' ')
                .Replace('\r', '\n');

            var lines = article.Split('\n')
                .Select(line => WhitespaceRegex.Replace(line, " ").Trim())
                .Where(line => line.Length > 0)
                .ToArray();
            if (lines.Length == 0)
                return null;

            var body = string.Join("\r\n", lines);
            if (body.Length > MaxBodyLength)
                body = body[..(MaxBodyLength - 1)].TrimEnd() + "…";
            return new ClubUpdateArticle(notice, body);
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    /// <summary>只在记录日期严格晚于已展示日期时提醒。</summary>
    public static bool ShouldNotify(ClubUpdateNotice notice, DateOnly? lastAcknowledgedDate)
        => !lastAcknowledgedDate.HasValue || notice.Date > lastAcknowledgedDate.Value;

    /// <summary>读取受信任的 HTTPS 日期文章链接，并拒绝跨域、非 HTTPS 和非日期路径。</summary>
    public static bool TryGetTrustedArticle(string? href, out DateOnly date, out Uri url)
    {
        date = default;
        url = null!;
        if (string.IsNullOrWhiteSpace(href) || href.Contains('\r') || href.Contains('\n'))
            return false;

        if (!Uri.TryCreate(href.Trim(), UriKind.RelativeOrAbsolute, out var candidate))
            return false;
        if (!candidate.IsAbsoluteUri)
        {
            if (!Uri.TryCreate(IndexUrl, UriKind.Absolute, out var baseUri) ||
                !Uri.TryCreate(baseUri, candidate, out candidate))
                return false;
        }

        if (candidate.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(candidate.Host, TrustedHost, StringComparison.OrdinalIgnoreCase) ||
            candidate.Port != 443 ||
            !string.IsNullOrEmpty(candidate.UserInfo) ||
            candidate.Query.Length > 0)
            return false;

        var path = candidate.AbsolutePath.TrimEnd('/');
        const string prefix = "/updates/";
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var dateText = path[prefix.Length..];
        if (dateText.Length != 10 ||
            !DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ||
            date < new DateOnly(2000, 1, 1))
        {
            date = default;
            return false;
        }

        url = new Uri($"{IndexUrl}{dateText}/", UriKind.Absolute);
        return true;
    }

    private static string CleanText(string html)
    {
        var text = ScriptOrStyleRegex.Replace(html, " ");
        text = TagRegex.Replace(text, " ");
        text = WebUtility.HtmlDecode(text);
        return WhitespaceRegex.Replace(text, " ").Trim();
    }
}
