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
            _deck.Clear();

            Random random = new();
            BlankCount = random.Next(_settings.MinBlankRounds, _settings.MaxBlankRounds);
            LiveCount = random.Next(_settings.MinLiveRounds, _settings.MaxLiveRounds);

            for (int i = 0; i < BlankCount; i++)
            {
                _deck.Add(BulletType.Blank);
            }
            for (int i = 0; i < LiveCount; i++)
            {
                _deck.Add(BulletType.Live);
            }

            Shuffle();
        }

        /// <summary>
        /// Used by the <c>ChamberCheck</c> item. Returns the last element in the deck, as items are popped from the end.
        /// </summary>
        /// <returns>The <c>BulletType</c> of the next bullet to be fired.</returns>
        public BulletType PeekChamber() {
            return _deck[^1];
        }

        /// <summary>
        /// Inverts the chamber round, making live rounds blank and blank rounds live.
        /// </summary>
        public void InvertChamber() {
            if (_deck[^1] == BulletType.Live) {
                _deck[^1] = BulletType.Blank;
            } else {
                _deck[^1] = BulletType.Live;
            }
        }
    }
}
