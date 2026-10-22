using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.Logging;

namespace PCL.Core.Test.Logging;

[TestClass]
public sealed class SecretRedactorTest
{
    [TestMethod]
    [DataRow("accessToken short-token", "short-token")]
    [DataRow("AccessToken：abc", "abc")]
    [DataRow("refresh_token=refresh-secret&scope=offline_access", "refresh-secret")]
    [DataRow("{\"password\":\"secret with spaces\"}", "secret with spaces")]
    [DataRow("{\"refresh_token\":\"secret\\\"quoted\"}", "quoted")]
    [DataRow("Authorization: Bearer eyJ.test.sig", "eyJ.test.sig")]
    [DataRow("Authorization: Basic dXNlcjpwYXNz", "dXNlcjpwYXNz")]
    [DataRow("GET /?device_code=private-device-code&x=1", "private-device-code")]
    [DataRow("--accessToken \"secret with spaces\" --uuid public", "secret with spaces")]
    public void CredentialFormsAreFullyRemoved(string input, string secret)
    {
        var result = SecretRedactor.Redact(input);
        Assert.IsFalse(result.Contains(secret, StringComparison.Ordinal), result);
        StringAssert.Contains(result, SecretRedactor.Replacement);
        Assert.AreEqual(result, SecretRedactor.Redact(result));
    }

    [TestMethod]
    public void ShortKnownSecretsAreRemovedWithoutAFieldLabel()
    {
        Assert.AreEqual("request [REDACTED] / [REDACTED]",
            SecretRedactor.Redact("request abc / abc", ["abc"]));
        Assert.AreEqual("status=200; version=1.2.0", SecretRedactor.Redact("status=200; version=1.2.0"));
    }

    [TestMethod]
    public async Task LoggerRedactsBeforeWritingToDisk()
    {
        var folder = Path.Combine(Path.GetTempPath(), "WzbcLogTest-" + Guid.NewGuid().ToString("N"));
        try
        {
            string file;
            await using (var logger = new Logger(new LoggerConfiguration(folder, AutoDeleteOldFile: false)))
            {
                file = logger.CurrentLogFiles[0];
                logger.Log("refresh_token=never-write-this&status=200");
            }
            var written = await File.ReadAllTextAsync(file);
            Assert.IsFalse(written.Contains("never-write-this", StringComparison.Ordinal));
            StringAssert.Contains(written, "status=200");
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }
}
