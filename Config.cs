using Tapper;

namespace liveorlive_server {
    [TranspilationSource]
    public class Config {
        public bool Private { get; set; } = false;
        public int MaxPlayers { get; set; } = 8;

        public int DefaultLives { get; set; } = 3;
        public int MaxLives { get; set; } = 8;

        public bool RandomItemsPerRound { get; set; } = false;
        public int MinItemsPerRound { get; set; } = 3;
        public int MaxItemsPerRound { get; set; } = 3;
        public int MaxItems { get; set; } = 4;

        public int MinBlankRounds { get; set; } = 1;
        public int MinLiveRounds { get; set; } = 1;
        public int MaxBlankRounds { get; set; } = 4;
        public int MaxLiveRounds { get; set; } = 4;

        public bool AllowLifeDonation { get; set; } = true;
        public bool AllowPlayerRevival { get; set; } = true;
        public bool AllowDoubleDamageStacking { get; set; } = false;
        public bool AllowDoubleSkips { get; set; } = false;
        public bool AllowExtraLifeWhenFull { get; set; } = false;
        public bool AllowSelfSkip { get; set; } = false;

        public bool LoseSkipAfterRound { get; set; } = true;
        public bool LootItemsOnKill { get; set; } = false;
        public bool CopySkipOnKill { get; set; } = true;
    }
}
