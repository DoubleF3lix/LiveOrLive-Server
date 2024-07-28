namespace liveorlive_server {
    public abstract class Deck<T> {
        protected List<T> deck = new List<T>();

        public abstract void refresh();

        // Fisher-Yates
        public void shuffle() {
            Random random = new Random();
            for (int i = 0; i < this.deck.Count; i++) {
                int j = random.Next(i, this.deck.Count);
                T temp = this.deck[i];
                this.deck[i] = this.deck[j];
                this.deck[j] = temp;
            }
        }

        public T pop() {
            T q = this.deck[this.deck.Count - 1];
            this.deck.RemoveAt(this.deck.Count - 1); // Removing from the end should be faster, but it really doesn't matter
            return q;
        }
    }
}
