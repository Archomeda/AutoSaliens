using System;
using AutoSaliens.Api.Models;
using DiscordRPC;

namespace AutoSaliens.Presence.Formatters
{
    internal abstract class PresenceFormatterBase : IPresenceFormatter
    {
        public virtual RichPresence FormatPresence(PlayerInfoResponse playerInfo, DiscordPresence presence)
        {
            if (playerInfo == null)
                throw new ArgumentNullException(nameof(playerInfo));
            if (presence == null)
                throw new ArgumentNullException(nameof(presence));

            return new RichPresence
            {
                Details = this.FormatDetails(playerInfo, presence),
                State = this.FormatState(playerInfo, presence),
                Timestamps = this.FormatTimestamps(playerInfo, presence),
                Assets = this.GetAssets(playerInfo, presence)
            };
        }

        protected abstract string FormatDetails(PlayerInfoResponse playerInfo, DiscordPresence presence);

        protected abstract string FormatState(PlayerInfoResponse playerInfo, DiscordPresence presence);

        protected abstract Timestamps FormatTimestamps(PlayerInfoResponse playerInfo, DiscordPresence presence);

        protected abstract Assets GetAssets(PlayerInfoResponse playerInfoResponse, DiscordPresence presence);
    }
}
