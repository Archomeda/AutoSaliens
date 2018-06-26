using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AutoSaliens
{
    internal static class UpdateChecker
    {
        public const string AppVeyorApiUrl = "https://ci.appveyor.com/api/projects/Archomeda/AutoSaliens/history";
        public const string AppVersion = /* APPVEYOR_START_VERSION */ null /* APPVEYOR_END_VERSION */;
        public const string AppBranch = /* APPVEYOR_START_BRANCH */ null /* APPVEYOR_END_BRANCH */;
        public const string AppDate = /* APPVEYOR_START_DATE */ null /* APPVEYOR_END_DATE */;

        public static Task<string> GetUpdateForBranch() => GetUpdateFor(AppBranch);

        public static Task<string> GetUpdateForStable() => GetUpdateFor("stable");

        private static async Task<string> GetUpdateFor(string branch)
        {
            if (string.IsNullOrWhiteSpace(AppBranch) || string.IsNullOrWhiteSpace(AppDate))
                return null;

            var uri = new Uri(AppVeyorApiUrl + $"?recordsNumber=50&branch={branch}");
            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("User-Agent", "AutoSaliens/1.0 (https://github.com/Archomeda/AutoAliens)");
                webClient.Headers.Add(HttpRequestHeader.Accept, "application/json");
                try
                {
                    var json = await webClient.DownloadStringTaskAsync(uri);
                    JArray builds = (JArray)((dynamic)JObject.Parse(json)).builds;
                    var appDate = DateTime.Parse(AppDate);
                    foreach (dynamic build in builds)
                    {
                        var buildDate = DateTime.Parse(build.committed.ToString());
                        if (buildDate <= appDate)
                            break;
                        if (build.status == "success")
                            return build.version;
                    }
                } catch (Exception) { }
            }
            return null;
        }
    }
}
