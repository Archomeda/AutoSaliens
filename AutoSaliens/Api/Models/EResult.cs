namespace AutoSaliens.Api.Models
{
    // https://github.com/SteamRE/SteamKit/blob/master/Resources/SteamLanguage/eresult.steamd
    internal enum EResult
    {
        Invalid = 0,
        OK = 1,
        Fail = 2,
        InvalidParam = 8,
        Busy = 10,
        InvalidState = 11,
        AccessDenied = 15,
        Banned = 17,
        Expired = 27,
        NoMatch = 42,
        ValueOutOfRange = 78,
        UnexpectedError = 79,
        RateLimitExceeded = 84,
        TimeIsOutOfSync = 93
    }
}
