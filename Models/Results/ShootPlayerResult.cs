using liveorlive_server.Enums;

namespace liveorlive_server.Models.Results {
    public class ShootPlayerResult {
        public BulletType BulletFired { get; set; }
        public int Damage { get; set; }
        public bool ShotSelf { get; set; }
    }
}
