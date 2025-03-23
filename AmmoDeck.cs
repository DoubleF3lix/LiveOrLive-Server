using liveorlive_server.Enums;

namespace liveorlive_server
{
    public class AmmoDeck(Config config) : Deck<BulletType>(config) {
        public int BlankCount { get; private set; } = 0;
        public int LiveCount { get; private set; } = 0;

        public override void Refresh() {
            this.deck.Clear();

            Random random = new();
            this.BlankCount = random.Next(1, config.MaxBlankRounds);
            this.LiveCount = random.Next(1, config.MaxLiveRounds);

            for (int i = 0; i < this.BlankCount; i++) {
                this.deck.Add(BulletType.Blank);
            }
            for (int i = 0; i < this.LiveCount; i++) {
                this.deck.Add(BulletType.Live);
            }

            this.Shuffle();
        }

        // Used by check bullet item
        public BulletType Peek() {
            return this.deck[^1]; // From the end
        }
    }
}
