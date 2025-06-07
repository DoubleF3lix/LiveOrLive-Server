using liveorlive_server.Enums;
using liveorlive_server.Models;

namespace liveorlive_server.Deck {
    /// <summary>
    /// Represents the chamber, filled with live or blank rounds.
    /// </summary>
    /// <param name="settings">Lobby settings, so we know how many of each type of round we can load.</param>
    public class AmmoDeck(Settings settings) : Deck<BulletType>(settings) {
        /// <summary>
        /// How many blank rounds were loaded in the chamber on refresh.
        /// </summary>
        public int BlankCount { get; private set; } = 0;
        /// <summary>
        /// How many live rounds were loaded in the chamber on refresh.
        /// </summary>
        public int LiveCount { get; private set; } = 0;

        /// <summary>
        /// Clears and reloads the chamber according to lobby settings. Sets <c>BlankCount</c> and <c>LiveCount</c> to the loaded counts.
        /// </summary>
        public override void Refresh() {
            this.deck.Clear();

            Random random = new();
            this.BlankCount = random.Next(settings.MinBlankRounds, settings.MaxBlankRounds);
            this.LiveCount = random.Next(settings.MinLiveRounds, settings.MaxLiveRounds);

            for (int i = 0; i < this.BlankCount; i++)
            {
                this.deck.Add(BulletType.Blank);
            }
            for (int i = 0; i < this.LiveCount; i++)
            {
                this.deck.Add(BulletType.Live);
            }

            this.Shuffle();
        }

        /// <summary>
        /// Used by the <c>ChamberCheck</c> item. Returns the last element in the deck, as items are popped from the end.
        /// </summary>
        /// <returns>The <c>BulletType</c> of the next bullet to be fired.</returns>
        public BulletType Peek() {
            return this.deck[^1];
        }
    }
}
