using liveorlive_server.Enums;
using Tapper;

namespace liveorlive_server.Models {
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
        // Whether or not the client should show how many rounds of each type have been fired
        // Can't really stop modified clients from tracking this since it's trivial, so we'll keep it completely client side
        public bool ShowFiredRoundsTally { get; set; } = false; // TODO

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
        public bool LootItemsOnKill { get; set; } = false; // TODO
        // The maximum number of items that can be looted on kill
        // TODO make select loot items menu (have item list displayed like item use, but have a number input next to each with a max selection amount) and display on kill according to this setting
        // Need to add packet to transfer items on loot
        public int MaxLootItemsOnKill { get; set; } = 2; // TODO
        // Allows the above to exceed MaxItems
        public bool AllowLootItemsExceedMax { get; set; } = false; // TODO

        // Item enablement (self-explanatory)
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
        public bool EnableTrenchcoatItem { get; set; } = false; // TODO (add functions and packets for usage)
        public bool EnableMisfireItem { get; set; } = false; // TODO implement
        public bool EnableHypnotizeItem { get; set; } = false; // TODO implement
        public bool EnablePocketPistolItem { get; set; } = false; // TODO implement, configurable odds

        // Gameplay
        public bool AnnounceChamberCheckResults { get; set; } = true;
        // Allows players to use Extra Lives on others. Same rules for using it on yourself apply.
        public bool AllowLifeDonation { get; set; } = true;
        // Allows players to come back. If false, once a player hits 0 lives, they're out for good
        // -1 is infinite, 0 to disable
        public int MaxPlayerRevives { get; set; } = -1;
        // Stacking is linear, not multiplicative, so 2 2x items is 3 damage, not 4
        public bool AllowDoubleDamageStacking { get; set; } = false; // TODO
        // If false, players are immune to skips if they lost their last turn
        // Doesn't count if player lost their skip at the end of a round and it never triggered before it was taken away
        public bool AllowSequentialSkips { get; set; } = false; // TODO
        // Allows players to skip themselves
        public bool AllowSelfSkip { get; set; } = false; // TODO

        // Whether the ricochet badge should be shown on player cards
        public bool ShowRicochets { get; set; } = false; // TODO
        // Whether or not a counter should be displayed for how many ricochets are active (essentially useless if the above is true)
        public bool ShowRicochetsCounter { get; set; } = true; // TODO
        // Stops reverse turn order items from being dealt with only two players, and turns any existing ones into Invert
        public bool DisableDealReverseAndRicochetWhenTwoPlayers { get; set; } = true; // TODO

        // Whether or not a skip is discarded from all players at the end of a round
        public bool LoseSkipAfterRound { get; set; } = true; // TODO
        // Whether ricochet should take skip status into account when shooting the next player in the turn order
        // When true, shoots next player who can actually go in the turn order, otherwise always shoots the next non-dead player
        public bool RicochetIgnoreSkippedPlayers { get; set; } = true; // TODO
        // What percentage of players have to be left for sudden death to activate
        // Sudden death disables revival, and turns all Extra Life items into Double Damage
        public int SuddenDeathActivationPoint { get; set; } = 40; // TODO

        // When enabled, taking a shot that kills a player doesn't end your turn
        public bool SecondWind { get; set; } = false; // TODO
        // Whether or not skip status is copied to the killer on kill. If so, turn immediately ends on kill and their next turn is lost.
        public bool CopySkipOnKill { get; set; } = true; // TODO
        // Allow other players (not the killer) to steal items from dead players
        public bool AllowLootingDead { get; set; } = false; // TODO
        // Whether or not dead players should be dealt items
        public bool RefreshDeadPlayerItems { get; set; } = true; // TODO
        // When true, dead player items are cleared before dealing, allowing essentially an open pot of items for players to steal from
        // A dead player also assumes control of these items when revived
        public bool ClearDeadPlayerItemsAfterRound { get; set; } = false; // TODO

        // Key is reward, value is weight (default is 50/50 for +2 or -1)
        public Dictionary<int, int> LifeGambleWeights = new() { { 2, 1 }, { -1, 1 } }; // TODO

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
            MaxPlayers = Math.Clamp(MaxPlayers, 2, 64);

            // Min <= Max = [1..12]
            MaxBlankRounds = Math.Clamp(MaxBlankRounds, 1, 12);
            MaxLiveRounds = Math.Clamp(MaxLiveRounds, 1, 12);
            MinBlankRounds = Math.Clamp(MinBlankRounds, 1, MaxBlankRounds);
            MinLiveRounds = Math.Clamp(MinLiveRounds, 1, MaxLiveRounds);

            MaxLives = Math.Clamp(MaxLives, 1, 32);
            DefaultLives = Math.Clamp(DefaultLives, 0, MaxLives);

            MaxItems = Math.Clamp(MaxItems, 0, 64);
            MaxItemsPerRound = Math.Clamp(MaxItemsPerRound, 0, MaxItems);
            MinItemsPerRound = Math.Clamp(MinItemsPerRound, 0, MaxItemsPerRound);
            if (!RandomItemsPerRound) {
                MinItemsPerRound = MaxItemsPerRound;
            }

            if (MaxPlayerRevives < 0) {
                MaxPlayerRevives = int.MaxValue;
            }
        }

        public IEnumerable<Item> GetEnabledItems() {
            if (EnableReverseTurnOrderItem) yield return Item.ReverseTurnOrder;
            if (EnableRackChamberItem) yield return Item.RackChamber;
            if (EnableExtraLifeItem) yield return Item.ExtraLife;
            if (EnablePickpocketItem) yield return Item.Pickpocket;
            if (EnableLifeGambleItem) yield return Item.LifeGamble;
            if (EnableInvertItem) yield return Item.Invert;
            if (EnableChamberCheckItem) yield return Item.ChamberCheck;
            if (EnableDoubleDamageItem) yield return Item.DoubleDamage;
            if (EnableSkipItem) yield return Item.Skip;
            if (EnableRicochetItem) yield return Item.Ricochet;
            if (EnableTrenchcoatItem) yield return Item.Trenchcoat; 
            if (EnableMisfireItem) yield return Item.Misfire;
            if (EnableHypnotizeItem) yield return Item.Hypnotize;
            if (EnablePocketPistolItem) yield return Item.PocketPistol;
        }
    }
}
