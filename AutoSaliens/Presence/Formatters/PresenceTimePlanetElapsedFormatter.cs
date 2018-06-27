using System;
using AutoSaliens.Api.Models;
using DiscordRPC;

namespace AutoSaliens.Presence.Formatters
{
    internal class PresenceTimePlanetElapsedFormatter : PresenceGenericFormatter
    {
        protected override Timestamps FormatTimestamps(PlayerInfoResponse playerInfo, DiscordPresence presence)
        {
            bool hasActivePlanet = !string.IsNullOrWhiteSpace(playerInfo.ActivePlanet);

            if (hasActivePlanet)
            {
                return new Timestamps
                {
                    Start = (DateTime.Now - playerInfo.TimeOnPlanet).ToUniversalTime()
                };
            }
            return null;
        }
    }
}
