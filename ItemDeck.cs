namespace liveorlive_server {
    public class ItemDeck(int playerCount) : Deck<Item> {
        // Need 4 of each item at minimum (7 items total), which is enough for 6 people
        // Requires floor div, which is present here
        // Don't need to refresh since it's done at the start of each round anyway
        readonly int multiplier = ((playerCount - 1) / 6) + 1;

        // Remove all the old items and repopulate as needed for the player count
        public override void Refresh() {
            this.deck.Clear();

            int uniqueItemCount = Enum.GetNames(typeof(Item)).Length;
            int totalItems = this.multiplier * 4;
            for (int i = 0; i < uniqueItemCount; i++) { // For each type of item...
                for (int j = 0; j < totalItems; j++) { // ...add 4 * multiplier of that item to the deck
                    this.deck.Add((Item)i);
                }
            }
            this.Shuffle();
        }

        // Handles refreshing the list
        public List<Item> GetSetForPlayer() {
            if (this.deck.Count < 4) {
                this.Refresh();
            }

            List<Item> output = new(4);
            for (int i = 0; i < 4; i++) {
                output.Add(this.Pop());
            }
            return output;
        }
    }
}
