using System;
using PCL;
using PCL.Core.Logging;

namespace PCL
{
    public static class McLogFilter
    {
        /// <summary>
        ///     打码字符串中的 AccessToken。
        /// </summary>
        public static string FilterAccessToken(string raw, char filterChar)
        {
            // Fully remove known credentials, including short values and unlabelled occurrences.
            var login = ModLaunch.mcLoginLoader.output;
            return SecretRedactor.Redact(raw, [login.AccessToken, login.ClientToken]);
        }

        /// <summary>
        ///     打码字符串中的 Windows 用户名。
        /// </summary>
        public static string FilterUserName(string raw, char filterChar)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var userName = userProfile.Split(@"\").Last();
            var maskedProfile = userProfile.Replace(userName, new string(filterChar, userName.Length));
            return raw.Replace(userProfile, maskedProfile);
        }
    }
}
