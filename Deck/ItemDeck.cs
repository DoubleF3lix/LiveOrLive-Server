using liveorlive_server.Enums;

namespace liveorlive_server.Deck
{
    // This deck seeks to emulate an actual deck of cards
    // That is, players can hoard items, and only cards that are not in play can be 
    public class ItemDeck : Deck<Item>
    {
        const int ITEM_PER_DECK = 4; // Number of each item per deck

        private readonly int playerCount;

        public ItemDeck(Config config, int playerCount) : base(config) {
            this.playerCount = playerCount;

            int uniqueItemCount = Enum.GetNames(typeof(Item)).Length;
            int itemsPerDeck = uniqueItemCount * ITEM_PER_DECK;
            // Decks needed is total max items across all players divided by how many items are in one deck, ceiling'd
            int deckCount = (int)Math.Ceiling((double)(this.config.MaxItems * this.playerCount) / itemsPerDeck);

            for (int i = 0; i < uniqueItemCount; i++)
            {
                for (int j = 0; j < ITEM_PER_DECK * deckCount; j++)
                {
                    deck.Add((Item)i);
                }
            }
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
