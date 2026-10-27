using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.Conduit;
using PCL.Core.IO.Net.Http;

namespace PCL.Core.Test.Conduit;

[TestClass]
public sealed class ClubHttpIdentityTest
{
    [TestMethod]
    [DataRow(false, "")]
    [DataRow(true, "")]
    [DataRow(false, "DownloadTool/1.0")]
    [DataRow(true, "DownloadTool/1.0")]
    public void DownloadIdentityRetainsBrowserAndCustomModes(bool browser, string custom)
    {
        var actual = ClubHttpIdentity.GetUserAgent(browser, custom);
        using var request = new HttpRequestMessage();
        request.Headers.UserAgent.ParseAdd(actual);
        if (custom.Length > 0)
            Assert.AreEqual(custom, actual);
        else
        {
            StringAssert.StartsWith(actual, "WZBC-Launcher/" + ClubRelease.CurrentVersion);
            Assert.AreEqual(browser, actual.Contains("Mozilla/5.0"));
            Assert.IsFalse(actual.Contains("PCL"));
        }
    }

    [TestMethod]
    public async Task SenderReplacesInheritedIdentityWithoutInventingReferer()
    {
        using var handler = new CaptureHandler();
        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PCL-CE/1.0");
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.invalid/test");
        request.Headers.UserAgent.ParseAdd("PCL-Community/PCL2-CE");
        using var response = await request.SendAsync(client, enableLogging: false, retryTimes: 0);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(ClubHttpIdentity.UserAgent, handler.UserAgent);
        Assert.IsNull(handler.Referer);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? UserAgent { get; private set; }
        public string? Referer { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            UserAgent = request.Headers.UserAgent.ToString();
            Referer = request.Headers.Referrer?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
