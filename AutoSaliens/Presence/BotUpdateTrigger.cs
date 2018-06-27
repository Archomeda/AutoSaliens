using AutoSaliens.Api.Models;

namespace AutoSaliens.Presence
{
    internal class BotUpdateTrigger : IPresenceUpdateTrigger
    {
        private IPresence presence;

        public virtual void SetPresence(IPresence presence) =>
            this.presence = presence ?? throw new System.ArgumentNullException(nameof(presence));

        public virtual void SetSaliensPlayerState(PlayerInfoResponse playerInfo) =>
            this.presence.SetSaliensPlayerState(playerInfo);
    }
}
