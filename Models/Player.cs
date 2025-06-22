using liveorlive_server.Enums;
using Tapper;

namespace liveorlive_server.Models {
    [TranspilationSource]
    public class Player(Settings config, string username, string? connectionId) : ConnectedClient(username, connectionId) {
        // Needed to keep track of players who have left without kicking them for disconnects
        public bool InGame { get; set; } = true;
        public int Lives { get; set; } = config.DefaultLives;
        public List<Item> Items { get; set; } = new(config.MaxItems);
        public bool IsSkipped { get; set; } = false;
        public bool IsRicochet { get; set; } = false;

        private readonly Settings config = config;

        /// <summary>
        /// Resets this player to defaults (clears items, resets lives, etc.).
        /// </summary>
        /// <returns>Returns <c>this</c> to simplify using <c>.Select()</c> in LINQ.</returns>
        public Player ResetDefaults() {
            InGame = true;
            Lives = config.DefaultLives;
            Items = new(config.MaxItems);
            IsSkipped = false;
            IsRicochet = false;

            return this;
        }
    }
}
