using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PCL.Core.Logging;

/// <summary>Best-effort credential removal before text reaches a log sink.</summary>
public static class SecretRedactor
{
    public const string Replacement = "[REDACTED]";
    private const string Keys = @"access_?token|refresh_?token|id_?token|client_?token|device_?code|user_?code|password|passwd|authorization";
    private static readonly Regex Quoted = new(
        "(?<prefix>[\"']?(?:" + Keys + ")[\"']?\\s*(?:[:=：]|\\s)\\s*)(?<quote>[\"'])(?:\\\\.|(?!\\k<quote>).)*\\k<quote>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));
    private static readonly Regex Unquoted = new(
        @"(?<prefix>\b(?:" + Keys + @")\b\s*(?:[:=：]|\s)\s*)(?![""'])(?<value>[^\s&;,\[\]}""']+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));
    private static readonly Regex Bearer = new(
        @"(?<prefix>\b(?:Bearer|Basic)\s+)[A-Za-z0-9._~+/=\-]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));

    public static string Redact(string? message, IEnumerable<string?>? knownSecrets = null)
    {
        if (string.IsNullOrEmpty(message)) return string.Empty;
        if (knownSecrets is not null)
            foreach (var secret in knownSecrets)
                if (!string.IsNullOrEmpty(secret))
                    message = message.Replace(secret, Replacement, StringComparison.Ordinal);
        try
        {
            message = Bearer.Replace(message, m => m.Groups["prefix"].Value + Replacement);
            message = Quoted.Replace(message, m => m.Groups["prefix"].Value + m.Groups["quote"].Value
                + Replacement + m.Groups["quote"].Value);
            message = Unquoted.Replace(message, m => m.Groups["prefix"].Value + Replacement);
            return message;
        }
        catch (RegexMatchTimeoutException)
        {
            return "[REDACTED LOG ENTRY: credential scan timed out]";
        }
    }
}
