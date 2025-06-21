using liveorlive_server.Enums;
using liveorlive_server.Models;

namespace liveorlive_server.Deck {
    /// <summary>
    /// This deck seeks to emulate an actual deck of cards.
    /// That is, players can hoard items, and only cards that are not in play can be dealt.
    /// </summary>
    /// <param name="settings">Lobby settings, so we know how to define how many and what type of items to deal.</param>
    public class ItemDeck(Settings settings) : Deck<Item>(settings) {
        // How many of each type of item should go in one deck
        const int ITEM_PER_DECK = 4; // Number of each item per deck

        // Used to ensure all players are dealt the same amount of items on deal, regardless of random count settings
        private int numItemsToDeal = 0;

        /// <summary>
        /// Initializes the item deck with a deck size according to the player count, and fills it with enabled items according to lobby settings.
        /// </summary>
        /// <param name="playerCount">How many players are in the game (do not include spectators).</param>
        public void Initialize(int playerCount) {
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
        }

        /// <summary>
        /// Refreshes the item deck, which means shuffling and refreshing the <c>numItemstoDeal</c> property.
        /// </summary>
        public override void Refresh() {
            Shuffle();
            this.numItemsToDeal = settings.RandomItemsPerRound ? 
                new Random().Next(settings.MinItemsPerRound, settings.MaxBlankRounds) 
                : settings.MaxItemsPerRound;
        }

        /// <summary>
        /// Use when an item is used to add it back into the deck to be pulled later.
        /// </summary>
        /// <param name="item">The item to add back.</param>
        public void PutItemBack(Item item) {
            this.deck.Add(item);
        }

        // We're guarunteed to have enough items according to Initialize logic
        /// <summary>
        /// Deal items to a player according to <c>numItemsToDeal</c> (set in <c>Initialize</c> according to lobby settings).
        /// </summary>
        /// <param name="player">The player to deal the items to.</param>
        /// <returns>The items that were dealt to the player.</returns>
        public List<Item> DealItemsToPlayer(Player player) {
            var itemsToDeal = Math.Min(settings.MaxItems - player.Items.Count, this.numItemsToDeal);
            var output = new List<Item>(itemsToDeal);
            output.AddRange(Enumerable.Range(0, itemsToDeal).Select(_ => Pop()));
            player.Items.AddRange(output);
            return output;
        }
    }
}
