using liveorlive_server.Enums;
using Tapper;

namespace liveorlive_server {
    [TranspilationSource]
    public class Settings {
        // Lobby
        // Private = True will hide game from lobby selector
        public bool Private { get; set; } = false;
        // How many players can be in a game. No cap on spectators.
        public int MaxPlayers { get; set; } = 8;

        // Chamber (self-explanatory)
        public int MinBlankRounds { get; set; } = 1;
        public int MinLiveRounds { get; set; } = 1;
        public int MaxBlankRounds { get; set; } = 4;
        public int MaxLiveRounds { get; set; } = 4;

        // Lives
        // DefaultLives is what players start with, MaxLives is the absolute max they can get with Extra Life items
        public int DefaultLives { get; set; } = 3;
        public int MaxLives { get; set; } = 8;
        // Allows burning Extra Life item (Extra Life can never exceed MaxLives)
        public bool AllowExtraLifeWhenFull { get; set; } = false;
        // But Life Gamble can if this is true
        public bool AllowLifeGambleExceedMax { get; set; } = true;

        // Item distribution
        // If false, MaxItemsPerRound is dealt every round, not exceeding MaxItems
        public bool RandomItemsPerRound { get; set; } = false;
        public int MinItemsPerRound { get; set; } = 3;
        public int MaxItemsPerRound { get; set; } = 3;
        public int MaxItems { get; set; } = 3;
        // Whether or not players get first dibs at looting a player on kill
        // This supercedes AllowLootingDead
        public bool LootItemsOnKill { get; set; } = false;
        // The maximum number of items that can be looted on kill
        public int MaxLootItemsOnKill { get; set; } = 2;
        // Allows the above to exceed MaxItems
        public bool AllowLootItemsExceedMax { get; set; } = false;

        // Item enable (self-explanatory)
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
        // Allows players to use Extra Lives on others. Same rules for using it on yourself apply.
        public bool AllowLifeDonation { get; set; } = true;
        // Allows players to come back. If false, once a player hits 0 lives, they're out for good
        public bool AllowPlayerRevival { get; set; } = true;
        // Stacking is linear, not multiplicative, so 2 2x items is 3 damage, not 4
        public bool AllowDoubleDamageStacking { get; set; } = false;
        // If false, players are immune to skips if they lost their last turn
        // Doesn't count if player lost their skip at the end of a round and it never triggered before it was taken away
        public bool AllowSequentialSkips { get; set; } = false;
        // Allows players to skip themselves
        public bool AllowSelfSkip { get; set; } = false;

        // Whether the ricochet badge should be shown on player cards
        public bool ShowRicochets { get; set; } = false;
        // Whether or not a counter should be displayed for how many ricochets are active (essentially useless if the above is true)
        public bool ShowRicochetsCounter { get; set; } = true;
        // Stops reverse turn order items from being dealt with only two players, and turns any existing ones into Invert
        public bool DisableDealReverseWhenTwoPlayers { get; set; } = true;

        // Whether or not a skip is discarded from all players at the end of a round
        public bool LoseSkipAfterRound { get; set; } = true;
        // Whether ricochet should take skip status into account when shooting the next player in the turn order
        // When true, shoots next player who can actually go in the turn order, otherwise always shoots the next non-dead player
        public bool RicochetIgnoreSkippedPlayers { get; set; } = true;
        // What percentage of players have to be left for sudden death to activate
        // Sudden death disables revival, and turns all Extra Life items into Double Damage
        public int SuddenDeathActivationPoint { get; set; } = 40; 

        // When enabled, taking a shot that kills a player doesn't end your turn
        public bool SecondWind { get; set; } = false;
        // Whether or not skip status is copied to the killer on kill. If so, turn immediately ends on kill and their next turn is lost.
        public bool CopySkipOnKill { get; set; } = true;
        // Allow other players (not the killer) to steal items from dead players
        public bool AllowLootingDead { get; set; } = false;
        // Whether or not dead players should be dealt items
        public bool RefreshDeadPlayerItems { get; set; } = true;
        // When true, dead player items are cleared before dealing, allowing essentially an open pot of items for players to steal from
        // A dead player also assumes control of these items when revived
        public bool ClearDeadPlayerItemsAfterRound { get; set; } = false;

        // Key is reward, value is weight (default is 50/50 for +2 or -1)
        public Dictionary<int, int> LifeGambleWeights = new() { { 2, 1 }, { -1, 1 } };

        /* Intended quirks:
         * Items can be disabled by setting MaxItems = 0
         * MaxItemsPerRound is used to give a fixed number of items when RandomItemsPerRound = false
         * If AllowLifeDonation = false but AllowPlayerRevival = true, players can be revived but only to one life
         * AllowDoubleDamageStacking has no limit, allowing for lethal combinations
         * When AllowExtraLifeWhenFull = true allows players to "burn" Extra Life items. Extra Life items cannot be used to exceed the max lives, however...
         * AllowLifeGambleExceedMax = true lets players go past MaxLives with no upper bound. If false, Life Gamble items can always be used, but a successful gamble will be capped at MaxItems
         * AllowSelfSkip = true, AllowSequentialSkips = false, and LoseSkipAfterRound = true allow for players to tactically skip themselves at a round end if they want a guarunteed turn next round
         * RicochetIgnoreSkippedPlayers = true has the potential to allow a player to shoot themselves on accident if everyone else is skipped
         * Sudden Death can be disabled with SuddenDeathActivationPoint = 0, and this value is useless if AllowPlayerRevival = false
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
