using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.Conduit;

namespace PCL.Core.Test.Conduit;

[TestClass]
public sealed class ClubUpdateNoticeTest
{
    [TestMethod]
    public void ParsesArticleOnlySortsDatesAndDeduplicatesSameDate()
    {
        const string html = """
            <nav>
              <a href="/updates/2026-01-01/">导航不属于正文</a>
            </nav>
            <article>
              <h1>更新日志</h1>
              <ul>
                <li><a href="/updates/2026-08-18/"><span>2026.08.18</span> · <strong>SHOU 服务器更新公告</strong></a></li>
                <li><a href="https://wzbcmc.cn/updates/2026-09-28/"><span>2026.09.28</span> · 生存服务器新增插件</a></li>
                <li><a href="/updates/2026-09-28/">2026.09.28 · 同日的装饰性改写</a></li>
                <li><a href="/updates/2026-07-12/">2026.07.12 · 更早记录</a></li>
                <li><a href="/updates/2026-10-03/">2026.10.03 · 尚未发布</a></li>
              </ul>
            </article>
            """;

        var notices = ClubUpdateNoticeParser.Parse(html, new DateOnly(2026, 10, 2));

        Assert.AreEqual(3, notices.Count);
        Assert.AreEqual(new DateOnly(2026, 9, 28), notices[0].Date);
        Assert.AreEqual("生存服务器新增插件", notices[0].Title);
        Assert.AreEqual(new DateOnly(2026, 8, 18), notices[1].Date);
        Assert.AreEqual(new DateOnly(2026, 7, 12), notices[2].Date);
        Assert.AreEqual("https://wzbcmc.cn/updates/2026-09-28/", notices[0].Url.ToString());
    }

    [TestMethod]
    public void MalformedOrMissingArticleProducesNoNotice()
    {
        Assert.AreEqual(0, ClubUpdateNoticeParser.Parse("<main><a href=\"/updates/2026-09-28/\">孤立链接</a></main>").Count);
        Assert.AreEqual(0, ClubUpdateNoticeParser.Parse("<article><a href=\"/updates/2026-09-31/\">不存在的日期</a></article>").Count);
        Assert.IsNull(ClubUpdateNoticeParser.ParseLatest("<article><p>没有日期文章</p></article>", new DateOnly(2026, 10, 2)));
    }

    [TestMethod]
    [DataRow("https://wzbcmc.cn/updates/2026-09-28/", true)]
    [DataRow("/updates/2026-09-28/", true)]
    [DataRow("https://wzbcmc.cn:444/updates/2026-09-28/", false)]
    [DataRow("http://wzbcmc.cn/updates/2026-09-28/", false)]
    [DataRow("https://wzbcmc.cn.attacker.test/updates/2026-09-28/", false)]
    [DataRow("https://attacker.test/updates/2026-09-28/", false)]
    [DataRow("https://wzbcmc.cn/updates/2026-09-28/?next=https://attacker.test/", false)]
    [DataRow("//attacker.test/updates/2026-09-28/", false)]
    public void ArticleLinksRequireTrustedHttpsOriginAndDatePath(string href, bool expected)
        => Assert.AreEqual(expected, ClubUpdateNoticeParser.TryGetTrustedArticle(href, out _, out _));

    [TestMethod]
    public void NotificationOnlyAdvancesForStrictlyNewerDate()
    {
        var notice = ClubUpdateNoticeParser.ParseLatest(
            "<article><a href=\"/updates/2026-09-28/\">2026.09.28 · 最新</a></article>",
            new DateOnly(2026, 10, 2))!;

        Assert.IsTrue(ClubUpdateNoticeParser.ShouldNotify(notice, null));
        Assert.IsFalse(ClubUpdateNoticeParser.ShouldNotify(notice, new DateOnly(2026, 9, 28)));
        Assert.IsFalse(ClubUpdateNoticeParser.ShouldNotify(notice, new DateOnly(2026, 10, 1)));
        Assert.IsTrue(ClubUpdateNoticeParser.ShouldNotify(notice, new DateOnly(2026, 9, 27)));
    }

    [TestMethod]
    public void ParsesDetailArticleAsSafePlainText()
    {
        var notice = new ClubUpdateNotice(
            new DateOnly(2026, 10, 2),
            "域名、资源与 SMP",
            new Uri("https://wzbcmc.cn/updates/2026-10-02/"));
        const string html = """
            <article>
              <nav>不要把面包屑放进正文</nav>
              <div class="tocCollapsible_ETCw">页面总览</div>
              <div class="theme-doc-markdown markdown">
                <header><h1>2026.10.02 · 域名、资源与 SMP 更新</h1></header>
                <p>服务器入口已更新。</p>
                <h2>连接地址</h2>
                <ul><li><code>smp.shoumc.com</code></li><li>create.shoumc.com</li></ul>
                <script>alert('不要执行');</script>
              </div>
            </article>
            """;

        var parsed = ClubUpdateNoticeParser.ParseArticle(html, notice);

        Assert.IsNotNull(parsed);
        StringAssert.Contains(parsed!.Body, "服务器入口已更新。");
        StringAssert.Contains(parsed.Body, "• smp.shoumc.com");
        StringAssert.Contains(parsed.Body, "• create.shoumc.com");
        Assert.IsFalse(parsed.Body.Contains("面包屑", StringComparison.Ordinal));
        Assert.IsFalse(parsed.Body.Contains("alert", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DetailWithoutArticleBodyIsIgnored()
    {
        var notice = new ClubUpdateNotice(
            new DateOnly(2026, 10, 2),
            "标题",
            new Uri("https://wzbcmc.cn/updates/2026-10-02/"));

        Assert.IsNull(ClubUpdateNoticeParser.ParseArticle("<main><p>不是文章正文</p></main>", notice));
    }
}
