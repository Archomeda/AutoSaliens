namespace AutoSaliens.Bot
{
    internal enum BotState
    {
        /// <summary>
        /// Invalid state.
        /// </summary>
        Invalid,

        /// <summary>
        /// The bot is resuming from where the player left of.
        /// </summary>
        Resume,

        /// <summary>
        /// Not on a planet nor in a zone.
        /// </summary>
        Idle,

        /// <summary>
        /// On a planet.
        /// </summary>
        OnPlanet,

        /// <summary>
        /// In a zone.
        /// </summary>
        InZone,

        /// <summary>
        /// In a zone, ending.
        /// </summary>
        InZoneEnd,

        /// <summary>
        /// Forced to leave zone.
        /// </summary>
        ForcedZoneLeave
    }
}
