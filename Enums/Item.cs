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
        Ricochet
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
                _ => "null"
            };
        }
    }
}
