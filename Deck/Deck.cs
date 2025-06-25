using liveorlive_server.Models;

namespace liveorlive_server.Deck {
    /// <summary>
    /// Abstract class to implement a deck, of which items can be loaded in any order, shuffled, and popped from the end.
    /// </summary>
    /// <typeparam name="T">The type of the deck items, likely <c>BulletType</c> or <c>Item</c>.</typeparam>
    /// <param name="settings">Derived classes should take <c>Settings</c> to dynamically adjust to lobby settings.</param>
    public abstract class Deck<T>(Settings settings) {
        protected Settings _settings = settings;
        protected List<T> _deck = [];

        /// <summary>
        /// Refresh the deck, possibly emptying and reloading its contents. Each derived class can define how it refreshes.
        /// </summary>
        public abstract void Refresh();

        /// <summary>
        /// Gets the number of elements contained in the deck
        /// </summary>
        public int Count { get { return _deck.Count; } }

        /// <summary>
        /// Shuffles the deck according to the Fisher-Yates algorithm
        /// </summary>
        public void Shuffle() {
            Random random = new();
            for (int i = 0; i < _deck.Count; i++) {
                int j = random.Next(i, _deck.Count);
                (_deck[j], _deck[i]) = (_deck[i], _deck[j]);
            }
        }

        /// <summary>
        /// Removes and returns an item from the end of the deck.
        /// </summary>
        /// <returns>The last item of the deck</returns>
        public T Pop() {
            T q = _deck[^1];
            _deck.RemoveAt(_deck.Count - 1); // Removing from the end should be faster, but it really doesn't matter
            return q;
        }
    }
}
