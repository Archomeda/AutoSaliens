using System.Diagnostics;
#if NETCOREAPP2_0
using System.Runtime.InteropServices;
#endif

namespace AutoSaliens.Utils
{
    internal static class Browser
    {
        public static void OpenDefault(string url)
        {
#if NETCOREAPP2_0
            // https://stackoverflow.com/a/38604462
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Process.Start("xdg-open", url);
#else
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
#endif
        }
    }
}
