namespace liveorlive_server.Deck {
    /// <summary>
    /// Abstract class to implement a deck, of which items can be loaded in any order, shuffled, and popped from the end.
    /// </summary>
    /// <typeparam name="T">The type of the deck items, likely <c>BulletType</c> or <c>Item</c>.</typeparam>
    /// <param name="settings">Derived classes should take <c>Settings</c> to dynamically adjust to lobby settings.</param>
    public abstract class Deck<T>(Settings settings) {
        protected Settings settings = settings;
        protected List<T> deck = [];

        /// <summary>
        /// Refresh the deck, possibly emptying and reloading its contents. Each derived class can define how it refreshes.
        /// </summary>
        public abstract void Refresh();

        /// <summary>
        /// Gets the number of elements contained in the deck
        /// </summary>
        public int Count { get { return deck.Count; } }

        /// <summary>
        /// Shuffles the deck according to the Fisher-Yates algorithm
        /// </summary>
        public void Shuffle() {
            Random random = new();
            for (int i = 0; i < deck.Count; i++) {
                int j = random.Next(i, deck.Count);
                (deck[j], deck[i]) = (deck[i], deck[j]);
            }
        }

        /// <summary>
        /// Removes and returns an item from the end of the deck.
        /// </summary>
        /// <returns>The last item of the deck</returns>
        public T Pop() {
            T q = deck[^1];
            deck.RemoveAt(deck.Count - 1); // Removing from the end should be faster, but it really doesn't matter
            return q;
        }
    }
}
