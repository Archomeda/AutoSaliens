using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AutoSaliens
{
    internal static class UpdateChecker
    {
        public const string GithubApiUrl = "https://api.github.com/repos/Archomeda/AutoSaliens/commits";
        public const string AppVersion = /* APPVEYOR_START_VERSION */ null /* APPVEYOR_END_VERSION */;
        public const string AppBranch = /* APPVEYOR_START_BRANCH */ null /* APPVEYOR_END_BRANCH */;
        public const string AppDate = /* APPVEYOR_START_DATE */ null /* APPVEYOR_END_DATE */;

        public static Task<bool> HasUpdateForBranch()
        {
            return HasUpdateFor(AppBranch);
        }

        public static Task<bool> HasUpdateForMaster()
        {
            return HasUpdateFor("master");
        }

        private static async Task<bool> HasUpdateFor(string branch)
        {
            if (string.IsNullOrWhiteSpace(AppBranch) || string.IsNullOrWhiteSpace(AppDate))
                return false;

            var uri = new Uri(GithubApiUrl + $"?since={AppDate}&sha={branch}");
            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("User-Agent", "AutoSaliens/1.0 (https://github.com/Archomeda/AutoAliens)");
                var json = await webClient.DownloadStringTaskAsync(uri);
                JArray commits = JArray.Parse(json);
                return commits.Count > 1;
            }
        }
    }
}
