using System;
using System.Linq;
using System.Text.Json;
using PCL.Core.Utils;

namespace PCL.Core.Conduit;

public sealed record ClubRelease(string Tag, string Title, string Notes, string PageUrl)
{
    public const string CurrentVersion = "1.2.0-wzbc.3";
    public const string CurrentCodename = "zyf";
    public const string CurrentTag = "v" + CurrentVersion;

    public bool IsNewerThan(string currentTag)
    {
        if (!TryParseTag(Tag, out var candidate, out var candidateSeries) ||
            !TryParseTag(currentTag, out var current, out var currentSeries)) return false;
        return CompareReleaseVersions(candidate!, candidateSeries, current!, currentSeries) > 0;
    }

    public static ClubRelease? ParseLatest(string json, string? repository = null, string? legacyRepository = null)
    {
        repository ??= ClubCatalog.ReleaseRepository;
        if (!IsReleaseRepository(repository))
            return null;
        // A legacy origin is accepted only when explicitly configured. WZBC never
        // falls back to SHOU-PCL's releases simply because it is the source project.
        var allowLegacy = IsReleaseRepository(legacyRepository);
        using var document = JsonDocument.Parse(json);
        ClubRelease? latest = null;
        foreach (var release in document.RootElement.EnumerateArray()
                     .Where(r => !r.GetProperty("draft").GetBoolean())
                     .OrderByDescending(r => r.GetProperty("published_at").GetDateTimeOffset()))
        {
            var tag = release.GetProperty("tag_name").GetString() ?? "";
            var url = release.GetProperty("html_url").GetString() ?? "";
            if (!TryParseTag(tag, out _, out _)
                || !(url.StartsWith(repository.TrimEnd('/') + "/releases/tag/", StringComparison.Ordinal)
                     || (allowLegacy && url.StartsWith(legacyRepository!.TrimEnd('/') + "/releases/tag/", StringComparison.Ordinal)))) continue;
            var candidate = new ClubRelease(tag, release.GetProperty("name").GetString() ?? tag, release.GetProperty("body").GetString() ?? "", url);
            if (latest is null || candidate.IsNewerThan(latest.Tag)) latest = candidate;
        }
        return latest;
    }

    private static bool IsReleaseRepository(string? repository)
        => Uri.TryCreate(repository, UriKind.Absolute, out var source)
           && source.Scheme == "https" && source.Host == "github.com" && source.IsDefaultPort
           && source.UserInfo.Length == 0 && source.Query.Length == 0 && source.Fragment.Length == 0
           && source.AbsolutePath.Trim('/').Split('/') is [ { Length: > 0 }, { Length: > 0 } ];

    private static bool TryParseTag(string tag, out SemVer? version, out TagSeries series)
    {
        var prefix = tag.StartsWith("v", StringComparison.Ordinal) ? "v" :
            tag.StartsWith("conduit-v", StringComparison.Ordinal) ? "conduit-v" : null;
        if (prefix is null || !SemVer.TryParse(tag[prefix.Length..], out version))
        {
            version = null;
            series = default;
            return false;
        }

        series = prefix == "v" ? TagSeries.Formal : TagSeries.Legacy;
        return true;
    }

    private static int CompareReleaseVersions(SemVer candidate, TagSeries candidateSeries, SemVer current,
        TagSeries currentSeries)
    {
        // The formal v* series is newer than every legacy conduit-v* tag. This
        // prefix boundary is intentional: a future formal v2.15.1 is still
        // newer than old conduit-v2.15.1-club.* migration tags.
        if (candidateSeries != currentSeries)
            return candidateSeries == TagSeries.Formal ? 1 : -1;

        return candidate.CompareTo(current);
    }

    private enum TagSeries
    {
        Formal,
        Legacy
    }
}
