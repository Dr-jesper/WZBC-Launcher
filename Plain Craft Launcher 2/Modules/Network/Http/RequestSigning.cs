using System.Net;
using System.Net.Http;
using PCL.Core.App;
using PCL.Core.Conduit;

namespace PCL.Network;

public static class RequestSigning
{
    internal static string SecretCdnSign(string urlWithMark)
    {
        if (!urlWithMark.EndsWithF("{CDN}"))
            return urlWithMark;
        return urlWithMark.Replace("{CDN}", "").Replace(" ", "%20");
    }
    
    /// <summary>
    ///     设置请求身份与下载服务 Headers。
    /// </summary>
    internal static void SecretHeadersSign(string url, ref HttpRequestMessage client, bool useBrowserUserAgent = false,
        string customUserAgent = "")
    {
        client.Version = HttpVersion.Version20;
        client.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        if (Uri.TryCreate(url, UriKind.Absolute, out var parsedUri) 
            && (parsedUri.Host == "api.curseforge.com"
            || parsedUri.Host == "edge.forgecdn.net"
            || parsedUri.Host == "mediafilez.forgecdn.net"))
        {
            client.Headers.Add("x-api-key", Secrets.CurseForgeAPIKey);
        }
        var userAgent = ClubHttpIdentity.GetUserAgent(useBrowserUserAgent, customUserAgent);
        client.Headers.UserAgent.Clear();
        client.Headers.Add("User-Agent", userAgent);
    }
}
