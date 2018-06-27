using System;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;

namespace AutoSaliens
{
    internal static class UpdateChecker
    {
        public const string AppVeyorApiUrl = "https://ci.appveyor.com/api/projects/Archomeda/AutoSaliens/history";
        // Don't change the comments in the constants below, they are needed for replacing the values during AppVeyor compilations
        public const string AppVersion = /* APPVEYOR_START_VERSION */ null /* APPVEYOR_END_VERSION */;
        public const string AppBranch = /* APPVEYOR_START_BRANCH */ null /* APPVEYOR_END_BRANCH */;
        public const string AppDate = /* APPVEYOR_START_DATE */ null /* APPVEYOR_END_DATE */;

        public static bool HasUpdate => !string.IsNullOrWhiteSpace(UpdateVersion);

        public static string UpdateVersion { get; private set; }

        public static bool HasUpdateBranch => !string.IsNullOrWhiteSpace(UpdateVersionBranch);

        public static string UpdateVersionBranch { get; private set; }


        public static async Task<bool> CheckForNewUpdates()
        {
            if (string.IsNullOrWhiteSpace(AppDate))
                return false;

            var prevBranch = UpdateVersionBranch;
            var prev = UpdateVersion;

            if (AppBranch != "stable")
                UpdateVersionBranch = await GetUpdateFor(AppBranch);
            UpdateVersion = await GetUpdateFor("stable");

            return prevBranch != UpdateVersionBranch || prev != UpdateVersion;
        }

        private static async Task<string> GetUpdateFor(string branch)
        {
            if (string.IsNullOrWhiteSpace(AppDate))
                return null;

            var appDate = DateTime.Parse(AppDate);
            var url = AppVeyorApiUrl.SetQueryParam("recordsNumber", 50)
                .SetQueryParam("branch", branch)
                .WithHeader("Accept", "application/json");
                try
                {
                    var json = await url.GetStringAsync();
                    var builds = JObject.Parse(json)["builds"].AsJEnumerable();
                    foreach (JObject build in builds)
                    {
                        var buildDate = build["committed"].Value<DateTime>();
                        if (buildDate <= appDate)
                            break;
                        if (build.ContainsKey("pullRequestId"))
                            continue;
                        if (build["status"].Value<string>() == "success")
                            return build["version"].Value<string>();
                    }
                } catch (Exception) { }
            return null;
        }
    }
}
