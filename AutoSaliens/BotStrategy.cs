using System;

namespace AutoSaliens
{
    /// <summary>
    /// Automation strategy. Lower value has higher priority when choosing a planet or zone.
    /// Some flags are incompatible with each other. If this happens, the higher priority flag will be chosen.
    /// </summary>
    [Flags]
    internal enum BotStrategy
    {
        /// <summary>
        /// Mainly focus on the current joined planet.
        /// </summary>
        FocusCurrentPlanet = 0x1,

        /// <summary>
        /// Go for planets in a randomized way.
        /// </summary>
        FocusRandomPlanet = 0x2,

        /// <summary>
        /// Go for planets and zones that contain bosses.
        /// </summary>
        FocusBosses = 0x4,


        /// <summary>
        /// Go for the most difficult planets first.
        /// </summary>
        MostDifficultPlanetsFirst = 0x10,

        /// <summary>
        /// Go for the least difficult planets first.
        /// </summary>
        LeastDifficultPlanetsFirst = 0x20,

        /// <summary>
        /// Go for the most completed planet first.
        /// </summary>
        MostCompletedPlanetsFirst = 0x100,

        /// <summary>
        /// Go for the least completed planets first.
        /// </summary>
        LeastCompletedPlanetsFirst = 0x200,


        /// <summary>
        /// Go for the most difficult zones first.
        /// </summary>
        MostDifficultZonesFirst = 0x1000,

        /// <summary>
        /// Go for the least difficult zones first.
        /// </summary>
        LeastDifficultZonesFirst = 0x2000,

        /// <summary>
        /// Go for the most completed first.
        /// </summary>
        MostCompletedZonesFirst = 0x10000,

        /// <summary>
        /// Go for the least completed zones first.
        /// </summary>
        LeastCompletedZonesFirst = 0x20000,


        /// <summary>
        /// Go for planets and zones in a top-down ordered way.
        /// </summary>
        TopDown = 0x100000,

        /// <summary>
        /// Go for planets and zones in a bottom-up ordered way.
        /// </summary>
        BottomUp = 0x200000,
    }
}
