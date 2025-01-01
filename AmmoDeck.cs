namespace liveorlive_server {
    public class AmmoDeck : Deck<AmmoType> {
        public int LiveCount { get; private set; } = 0;
        public int BlankCount { get; private set; } = 0;

        public override void Refresh() {
            this.deck.Clear();

            this.LiveCount = 0;
            this.BlankCount = 0;
            this.AddAmmo(AmmoType.Live);
            this.AddAmmo(AmmoType.Blank);

            this.Shuffle();
        }

        public int AddAmmo(AmmoType type) {
            Random random = new();
            int count = random.Next(1, 7);
            for (int i = 0; i < count; i++) {
                this.deck.Add(type);
            }

            if (type == AmmoType.Live) {
                this.LiveCount += count;
            } else if (type == AmmoType.Blank) {
                this.BlankCount += count;
            }
            return count;
        }

        // Used by check bullet item
        public AmmoType Peek() {
            return this.deck[^1]; // From the end
        }
    }
}
