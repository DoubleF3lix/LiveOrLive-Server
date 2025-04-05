using liveorlive_server.Enums;

namespace liveorlive_server.Deck
{
    // This deck seeks to emulate an actual deck of cards
    // That is, players can hoard items, and only cards that are not in play can be 
    public class ItemDeck(Settings settings) : Deck<Item>(settings) {
        const int ITEM_PER_DECK = 4; // Number of each item per deck

        public void Populate(int playerCount) {
            var enabledItems = this.settings.GetEnabledItems().ToList();
            var uniqueItemCount = enabledItems.Count;
            var itemsPerDeck = uniqueItemCount * ITEM_PER_DECK;
            // Decks needed is total max items across all players divided by how many items are in one deck, ceiling'd
            var deckCount = (int)Math.Ceiling((double)(this.settings.MaxItems * playerCount) / itemsPerDeck);

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
            var itemsToDealCount = settings.RandomItemsPerRound ? new Random().Next(settings.MinItemsPerRound, settings.MaxBlankRounds) : settings.MaxItemsPerRound;
            var itemsToDeal = Math.Min(settings.MaxItems - player.Items.Count, itemsToDealCount);
            var output = new List<Item>(itemsToDeal);
            output.AddRange(Enumerable.Range(0, itemsToDeal).Select(_ => Pop()));
            player.Items.AddRange(output);
        }
    }
}
