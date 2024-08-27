namespace liveorlive_server {
    public class AmmoDeck : Deck<AmmoType> {
        public int liveCount { get; private set; } = 0;
        public int blankCount { get; private set; } = 0;

        public override void refresh() {
            this.deck.Clear();

            this.liveCount = 0;
            this.blankCount = 0;
            this.addAmmo(AmmoType.Live);
            this.addAmmo(AmmoType.Blank);

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

        // Used by check bullet item
        public AmmoType peek() {
            return this.deck[this.deck.Count - 1];
        }
    }
}
