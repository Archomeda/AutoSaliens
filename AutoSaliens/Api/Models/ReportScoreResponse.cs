namespace AutoSaliens.Api.Models
{
    internal class ReportScoreResponse
    {
        public int NewLevel { get; set; }

        public string NewScore { get; set; }

        public string NextLevelScore { get; set; }

        public int OldLevel { get; set; }

        public string OldScore { get; set; }
    }
}
