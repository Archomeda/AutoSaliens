using System;
using AutoSaliens.Api.Models;
using AutoSaliens.Presence.Formatters;

namespace AutoSaliens.Presence
{
    internal interface IPresence
    {
        event EventHandler PresenceActivated;

        event EventHandler PresenceDeactivated;


        IPresenceFormatter Formatter { get; set; }

        bool IsPresenceActive { get; }

        ILogger Logger { get; set; }

        IPresenceUpdateTrigger UpdateTrigger { get; set; }


        void Start();

        void Stop();

        void SetSaliensPlayerState(PlayerInfoResponse playerInfo);
    }
}
