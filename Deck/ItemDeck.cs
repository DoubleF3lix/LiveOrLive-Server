using liveorlive_server.Enums;

namespace liveorlive_server.Deck
{
    // This deck seeks to emulate an actual deck of cards
    // That is, players can hoard items, and only cards that are not in play can be 
    public class ItemDeck(Settings config) : Deck<Item>(config) {
        const int ITEM_PER_DECK = 4; // Number of each item per deck

        public void Populate(int playerCount) {
            var enabledItems = this.config.GetEnabledItems().ToList();
            var uniqueItemCount = enabledItems.Count;
            var itemsPerDeck = uniqueItemCount * ITEM_PER_DECK;
            // Decks needed is total max items across all players divided by how many items are in one deck, ceiling'd
            var deckCount = (int)Math.Ceiling((double)(this.config.MaxItems * playerCount) / itemsPerDeck);

            foreach (var item in enabledItems) {
                for (int j = 0; j < ITEM_PER_DECK * deckCount; j++) {
                    this.deck.Add(item);
                }
            }

            Shuffle();
        }

        public override void Refresh() {
            Shuffle();
        }

        // Should be used when an item is used
        public void PutItemBack(Item item) {
            deck.Add(item);
        }

        // We're guarunteed to have enough items according to constructor logic
        public void DealItemsToPlayer(Player player) {
            var itemsToDealCount = config.RandomItemsPerRound ? new Random().Next(config.MinItemsPerRound, config.MaxBlankRounds) : config.MaxItemsPerRound;
            var itemsToDeal = Math.Min(config.MaxItems - player.Items.Count, itemsToDealCount);
            var output = new List<Item>(itemsToDeal);
            output.AddRange(Enumerable.Range(0, itemsToDeal).Select(_ => Pop()));
            player.Items.AddRange(output);
        }
    }
}
