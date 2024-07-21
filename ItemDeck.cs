namespace liveorlive_server {
    public class ItemDeck {
        int multiplier;
        List<Item> deck = new List<Item>();

        public ItemDeck(int playerCount) {
            // Need 4 of each item at minimum (7 items total), which is enough for 6 people
            this.multiplier = ((playerCount - 1) / 6) + 1; // Requires floor div, which is present here
            this.refresh();
        }

        // Remove all the old items and repopulate as needed for the player count
        public void refresh() {
            this.deck.Clear();

            int uniqueItemCount = Enum.GetNames(typeof(Item)).Length;
            int totalItems = this.multiplier * 4;
            for (int i = 0; i < uniqueItemCount; i++) { // For each type of item...
                for (int j = 0; j < totalItems; j++) { // ...add 4 * multiplier of that item to the deck
                    this.deck.Add((Item)j);
                }
            }
            this.shuffle();
        }

        // Randomly swap everything around (Fisher-Yates)
        public void shuffle() {
            Random random = new Random();
            for (int i = 0; i < this.deck.Count; i++) {
                int j = random.Next(i, this.deck.Count);
                Item temp = this.deck[i];
                this.deck[i] = this.deck[j];
                this.deck[j] = temp;
            }
        }

        public Item pop() {
            Item q = this.deck[this.deck.Count - 1];
            this.deck.RemoveAt(this.deck.Count - 1); // Removing from the end should be faster, but it really doesn't matter
            return q;
        }

        // Handles refreshing the list
        public List<Item> getSetForPlayer() {
            if (this.deck.Count < 4) {
                this.refresh();
            }

            List<Item> output = new List<Item>(4);
            for (int i = 0; i < 4; i++) {
                output.Add(this.pop());
            }
            return output;
        }
    }
}
