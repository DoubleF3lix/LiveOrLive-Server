using LiveOrLiveServer.Enums;

namespace LiveOrLiveServer.Models.Results {
    public class ShootPlayerResult {
        public BulletType BulletFired { get; set; }
        public int Damage { get; set; }
        public bool ShotSelf { get; set; }
        public bool Killed { get; set; }
        public bool Eliminated { get; set; }
        
        // The player shot may not necessarily be the one we aimed at
        public required Player PlayerShot { get; set; }
        public required List<Player> Ricochets { get; set; }
    }
}
