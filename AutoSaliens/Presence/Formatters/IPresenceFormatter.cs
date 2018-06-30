using AutoSaliens.Api.Models;
using DiscordRPC;

namespace AutoSaliens.Presence.Formatters
{
    internal interface IPresenceFormatter
    {
        RichPresence FormatPresence(PlayerInfoResponse playerInfo, DiscordPresence presence);
    }
}
