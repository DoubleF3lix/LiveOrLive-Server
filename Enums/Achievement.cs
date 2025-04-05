using System.ComponentModel;

namespace liveorlive_server.Enums {
    public enum Achievement {
        [Description("Skip yourself by killing a skipped player twice in a game")]
        Whoops,
        [Description("Use Adrenaline 3 times in a game and live (in a row)")]
        HighRoller,
        [Description("Use Adrenaline 3 times in a game and lose (in a row)")]
        LosingToYourself,
        [Description("Use Adrenaline 4 turns in a row")]
        Addiction,
        [Description("Have 7+ lives at once")]
        Overclock,
        [Description("Give someone else a life when you only have one")]
        Martyr,
        [Description("Revive 3 people in a game")]
        GuardianAngel,
        [Description("After being revived, win a game")]
        ZeroToHero,
        [Description("Give someone a life and immediately shoot them")]
        MockTrial,
        [Description("Steal someone's life and use it on yourself when you already have max lives")]
        CruelAndUnusual,
        [Description("Shoot yourself with 50/50 odds and survive three times in a game")]
        FiftyFiftyFifty,
        [Description("With only one life left, shoot yourself with 50/50 odds and survive")]
        Cliffhanger,
        [Description("Die on your first turn without using any items")]
        Beatdown,
        [Description("Die in the first round")]
        CutOff,
        [Description("Shoot yourself with 25% odds for survival")]
        Deathwish,
        [Description("Successfully shoot someone else with less than 50% odds")]
        FatChance,
        [Description("Use every single item at least once in a game")]
        WasteNot,
        [Description("Use all your items in a single turn (at least 3)")]
        WantNot,
        [Description("After using an invert item, shoot yourself with a live round")]
        Backfire,
        [Description("Successfully shoot another player with double damage")]
        DoubleTrouble,
        [Description("Eliminate 3 players in a game")]
        HatTrick,
        [Description("Eliminate 2 players in a single round")]
        Sweep,
        [Description("With only one life left, skip yourself by killing another skipped player")]
        InsurancePolicy,
        [Description("Fire 3 rounds in a single turn")]
        RealFortune,
        [Description("Survive 6 self-shots in a single game")]
        RussianRoulette,
        [Description("With 1 other player left, shoot the other player with a blank round")]
        ImmediateDanger,
        [Description("Shoot 3 people with blank rounds consecutively")]
        Stormtrooper,
        [Description("Take 10 shots in a single game")]
        Bulletproof,
        [Description("Get shot with a live round by 3 different people in a row")]
        FiringSquad,
        [Description("With one life left, shoot yourself with a blank, and then shoot someone else and eliminate them")]
        DeadManWalking,
        [Description("Get revived and eliminate the person who killed you")]
        Phoenix,
        [Description("Use Adrenaline with a single life left and survive")]
        CourtingDeath,
        [Description("Have 3 of the same item at once")]
        Hoarder,
        [Description("Skip a player who just used a skip in their last turn")]
        Karma,
        [Description("Use a Chamber Check with one item left in the chamber")]
        WhatsMyLine,
        [Description("Hold onto a Chamber Check for an entire game without using it")]
        FortuneFavorsTheBold,
        [Description("Win a game without using an item")]
        IronWill,
        [Description("Give a life to someone who goes on to win the game")]
        Kingmaker,
        [Description("Win the game with 4+ lives")]
        Juggernaut,
        [Description("Survive 3 turns with only one life")]
        LivingOnTheEdge,
        [Description("Win the game without being given a life")]
        SoloAct,
        [Description("Win the game with 5 unused items")]
        Overstock,
        [Description("Use more damage than necessary to kill someone")]
        Overkill,
        [Description("Use more damage than necessary to kill someone and win the game")]
        Overoverkill,
        [Description("Use a Chamber Check and shoot yourself with a live round")]
        Amnesia,
        [Description("Survive every self-shot you make in a game (must make at least 3)")]
        PlotArmor,
        [Description("Reverse the turn order and then get shot")]
        Jinx,
        [Description("Use chamber check, invert, and double damage in the same turn")]
        PerfectStorm,
        [Description("Invert a round and eliminate someone with it")]
        Magician,
        [Description("Steal Adrenaline from someone and lose")]
        ThreeSidedCoin,
        [Description("Steal someone's skip and use it on them")]
        Opportunist, // "Opportunity", but said in a high pitched opera-like voice
        [Description("Hold onto a +1 Life item for 2 turns in a row")]
        EscapeHatch,
        [Description("Reverse the turn order and shoot the person who was supposed to go next")]
        BoomerangBullet,
        [Description("Steal an Adrenaline from someone with only one life left")]
        Rehabbed,
        [Description("Reverse the turn order twice in one turn")]
        PeakEfficiency,
        [Description("Play through a complete round by yourself")]
        NoTimeToLive,
        [Description("Die by Adrenaline twice in one game")]
        TheCharity,
        [Description("Don't take a turn for 3 rounds")]
        NotYourLuckyDay,
        [Description("Have 3 skips expire by round-end in one game")]
        YourLuckyDay,
        [Description("Kill yourself twice and still win a game")]
        RecklessAbandon,
        [Description("With at least two other players, skip everyone else and go twice in a row")]
        FullRotation,
        [Description("Shoot someone who gave you a life with their last turn")]
        ColdHeart,
        [Description("Shoot yourself with a live round on accident due to all opponents having Ricochet")]
        ReboundRelationship,
        [Description("Avoid getting shot with Ricochet by skipping yourself")]
        Ricochad,
        [Description("Get killed due to a ricochet")]
        HoldMeCloserEd,
        [Description("With only two players left at one life each and a 1/1 chamber, kill your opponent and win the game without using any items")]
        UltimateVictory
    }
}
