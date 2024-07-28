namespace liveorlive_server {
    public class AmmoDeck : Deck<AmmoType> {
        public int liveCount { get; private set; } = 0;
        public int blankCount { get; private set; } = 0;

        public override void refresh() {
            this.deck.Clear();

            Random random = new Random();
            this.liveCount = random.Next(1, 7);
            this.blankCount = random.Next(1, 7);

            for (int i = 0; i < this.liveCount; i++) {
                this.deck.Add(AmmoType.Live);
            }
            for (int i = 0; i < this.blankCount; i++) {
                this.deck.Add(AmmoType.Blank);
            }

            this.shuffle();
        }

        public int addAmmo(AmmoType type) {
            Random random = new Random();
            int count = random.Next(1, 7);
            for (int i = 0; i < count; i++) {
                this.deck.Add(type);
            }

            if (type == AmmoType.Live) {
                this.liveCount += count;
            } else if (type == AmmoType.Blank) {
                this.blankCount += count;
            }
            return count;
        }
    }
}
