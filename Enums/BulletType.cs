using Tapper;

namespace liveorlive_server.Enums {
    [TranspilationSource]
    public enum BulletType {
        Blank,
        Live
    }

    public static class BulletTypeExtension {
        public static string? ToFriendlyString(this BulletType item) {
            return item switch {
                BulletType.Blank => "blank",
                BulletType.Live => "live",
                _ => "null"
            };
        }
    }
}