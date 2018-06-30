namespace AutoSaliens.Api.Models
{
    internal class ReportBossDamageResponse
    {
        public BossStatus BossStatus { get; set; }

        public bool WaitingForPlayers { get; set; }

        public bool GameOver { get; set; }

        public int NumLaserUses { get; set; }

        public int NumTeamHeals { get; set; }
    }
}
