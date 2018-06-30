using System.Collections.Generic;

namespace AutoSaliens.Api.Models
{
    internal class BossStatus
    {
        public long BossHp { get; set; }

        public long BossMaxHp { get; set; }

        public List<BossPlayer> BossPlayers { get; set; }
    }
}
