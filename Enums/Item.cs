using Tapper;

namespace liveorlive_server.Enums {
    [TranspilationSource]
    public enum Item {
        ReverseTurnOrder,
        RackChamber,
        ExtraLife,
        Pickpocket,
        LifeGamble, // Also called Adrenaline
        Invert,
        ChamberCheck,
        DoubleDamage,
        Skip,
        Ricochet,
        Trenchcoat,
        Misfire,
        Hypnotize,
        PocketPistol
    }

    public static class ItemExtension {
        public static string? ToFriendlyString(this Item item) {
            return item switch {
                Item.ReverseTurnOrder => "Reverse Turn Order",
                Item.RackChamber => "Rack Chamber",
                Item.ExtraLife => "Extra Life",
                Item.Pickpocket => "Pickpocket",
                Item.LifeGamble => "Life Gamble",
                Item.Invert => "Invert",
                Item.ChamberCheck => "Chamber Check",
                Item.DoubleDamage => "Double Damage",
                Item.Skip => "Skip",
                Item.Ricochet => "Ricochet",
                Item.Trenchcoat => "Trenchcoat",
                Item.Misfire => "Misfire",
                Item.Hypnotize => "Hypnotize",
                Item.PocketPistol => "Pocket Pistol",
                _ => "null"
            };
        }
    }
}
