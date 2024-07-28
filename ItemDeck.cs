namespace liveorlive_server {
    public class ItemDeck : Deck<Item> {
        int multiplier;

        public ItemDeck(int playerCount) {
            // Need 4 of each item at minimum (7 items total), which is enough for 6 people
            this.multiplier = ((playerCount - 1) / 6) + 1; // Requires floor div, which is present here

            // Don't need to refresh since it's done at the start of each round anyway
        }

        // Remove all the old items and repopulate as needed for the player count
        public override void refresh() {
            this.deck.Clear();

            int uniqueItemCount = Enum.GetNames(typeof(Item)).Length;
            int totalItems = this.multiplier * 4;
            for (int i = 0; i < uniqueItemCount; i++) { // For each type of item...
                for (int j = 0; j < totalItems; j++) { // ...add 4 * multiplier of that item to the deck
                    this.deck.Add((Item)i);
                }
            }
            this.shuffle();
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
