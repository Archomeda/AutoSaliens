namespace AutoSaliens.Api.Models
{
    // https://github.com/SteamRE/SteamKit/blob/master/Resources/SteamLanguage/eresult.steamd
    internal enum EResult
    {
        Invalid = 0,
        OK = 1,
        Busy = 10,
        InvalidState = 11,
        AccessDenied = 15,
        Expired = 27,
        NoMatch = 42,
        TimeIsOutOfSync = 93
    }
}
