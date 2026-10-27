using System.Net.Http;

namespace PCL.Core.Conduit;

/// <summary>Shared public HTTP identity for WZBC requests and downloads.</summary>
public static class ClubHttpIdentity
{
    public const string UserAgent = "WZBC-Launcher/" + ClubRelease.CurrentVersion;
    private const string BrowserCompatibility = " Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36 Edg/136.0.0.0";

    public static string GetUserAgent(bool browserCompatibility = false, string customUserAgent = "")
        => !string.IsNullOrEmpty(customUserAgent) ? customUserAgent
            : browserCompatibility ? UserAgent + BrowserCompatibility : UserAgent;

    public static void Apply(HttpRequestMessage request)
    {
        // Replace inherited identity rather than appending a second launcher product.
        request.Headers.UserAgent.Clear();
        request.Headers.UserAgent.ParseAdd(UserAgent);
    }
}
