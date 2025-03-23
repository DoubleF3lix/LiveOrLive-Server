namespace liveorlive_server.Deck
{
    public abstract class Deck<T>(Config config)
    {
        protected Config config = config;
        protected List<T> deck = [];

        public abstract void Refresh();

        public int Count { get { return deck.Count; } }

        // Fisher-Yates
        public void Shuffle() {
            Random random = new();
            for (int i = 0; i < deck.Count; i++)
            {
                int j = random.Next(i, deck.Count);
                (deck[j], deck[i]) = (deck[i], deck[j]);
            }
        }

        public T Pop() {
            T q = deck[^1];
            deck.RemoveAt(deck.Count - 1); // Removing from the end should be faster, but it really doesn't matter
            return q;
        }
    }
}
