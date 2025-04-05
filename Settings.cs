using liveorlive_server.Enums;
using Tapper;

namespace liveorlive_server {
    [TranspilationSource]
    public class Settings {
        // Lobby
        public bool Private { get; set; } = false;
        public int MaxPlayers { get; set; } = 8;

        // Chamber
        public int MinBlankRounds { get; set; } = 1;
        public int MinLiveRounds { get; set; } = 1;
        public int MaxBlankRounds { get; set; } = 4;
        public int MaxLiveRounds { get; set; } = 4;

        // Lives
        public int DefaultLives { get; set; } = 3;
        public int MaxLives { get; set; } = 8;

        // Item distribution
        public bool RandomItemsPerRound { get; set; } = false;
        public int MinItemsPerRound { get; set; } = 3;
        public int MaxItemsPerRound { get; set; } = 3;
        public int MaxItems { get; set; } = 3;

        // Item
        public bool EnableReverseTurnOrderItem { get; set; } = true;
        public bool EnableRackChamberItem { get; set; } = true;
        public bool EnableExtraLifeItem { get; set; } = true;
        public bool EnablePickpocketItem { get; set; } = true;
        public bool EnableLifeGambleItem { get; set; } = true;
        public bool EnableInvertItem { get; set; } = true;
        public bool EnableChamberCheckItem { get; set; } = true;
        public bool EnableDoubleDamageItem { get; set; } = true;
        public bool EnableSkipItem { get; set; } = true;
        public bool EnableRicochetItem { get; set; } = false;

        // Gameplay
        public bool AllowLifeDonation { get; set; } = true;
        public bool AllowPlayerRevival { get; set; } = true;
        public bool AllowDoubleDamageStacking { get; set; } = false;
        public bool AllowSequentialSkips { get; set; } = false;
        public bool AllowExtraLifeWhenFull { get; set; } = false;
        public bool AllowLifeGambleExceedMax { get; set; } = true;
        public bool AllowSelfSkip { get; set; } = false;

        public bool LoseSkipAfterRound { get; set; } = true;
        public bool CopySkipOnKill { get; set; } = true;
        public bool RicochetIgnoreSkippedPlayers { get; set; } = true;

        public bool LootItemsOnKill { get; set; } = false;
        public int MaxLootItemsOnKill { get; set; } = 2;
        public bool AllowLootItemsExceedMax { get; set; } = false;

        /* Intended quirks:
         * Items can be disabled by setting MaxItems = 0
         * MaxItemsPerRound is used to give a fixed number of items when RandomItemsPerRound = false
         * If AllowLifeDonation = false but AllowPlayerRevival = true, players can be revived but only to one life
         * AllowDoubleDamageStacking has no limit, allowing for lethal combinations
         * When AllowExtraLifeWhenFull = true allows players to "burn" Extra Life items. Extra Life items cannot be used to exceed the max lives, however...
         * AllowLifeGambleExceedMax = true lets players go past MaxLives with no upper bound. If false, Life Gamble items can always be used, but a successful gamble will be capped at MaxItems
         * AllowSelfSkip = true, AllowSequentialSkips = false, and LoseSkipAfterRound = true allow for players to tactically skip themselves at a round end if they want a guarunteed turn next round
         * RicochetIgnoreSkippedPlayers = true has the potential to allow a player to shoot themselves on accident if everyone else is skipped
         */
        public void Normalize() {
            this.MaxPlayers = Math.Clamp(this.MaxPlayers, 2, 100);

            // Min <= Max = [1..12]
            this.MaxBlankRounds = Math.Clamp(this.MaxBlankRounds, 1, 12);
            this.MaxLiveRounds = Math.Clamp(this.MaxLiveRounds, 1, 12);
            this.MinBlankRounds = Math.Clamp(this.MinBlankRounds, 1, this.MaxBlankRounds);
            this.MinLiveRounds = Math.Clamp(this.MinLiveRounds, 1, this.MaxLiveRounds);

            this.MaxLives = Math.Clamp(this.MaxLives, 1, 100);
            this.DefaultLives = Math.Clamp(this.DefaultLives, 0, this.MaxLives);

            // MinPR <= MaxPR <= Max = [1..100]
            this.MaxItems = Math.Clamp(this.MaxItems, 0, 100);
            this.MaxItemsPerRound = Math.Clamp(this.MaxItemsPerRound, 0, this.MaxItems);
            this.MinItemsPerRound = Math.Clamp(this.MinItemsPerRound, 0, this.MaxItemsPerRound);
            if (!RandomItemsPerRound) {
                this.MinItemsPerRound = this.MaxItemsPerRound;
            }
        }

        public IEnumerable<Item> GetEnabledItems() {
            if (this.EnableReverseTurnOrderItem) yield return Item.ReverseTurnOrder;
            if (this.EnableRackChamberItem) yield return Item.RackChamber;
            if (this.EnableExtraLifeItem) yield return Item.ExtraLife;
            if (this.EnablePickpocketItem) yield return Item.Pickpocket;
            if (this.EnableLifeGambleItem) yield return Item.LifeGamble;
            if (this.EnableInvertItem) yield return Item.Invert;
            if (this.EnableChamberCheckItem) yield return Item.ChamberCheck;
            if (this.EnableDoubleDamageItem) yield return Item.DoubleDamage;
            if (this.EnableSkipItem) yield return Item.Skip;
            if (this.EnableRicochetItem) yield return Item.Richochet;
        }
    }
}
