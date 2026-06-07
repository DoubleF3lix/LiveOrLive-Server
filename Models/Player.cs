using LiveOrLiveServer.Enums;
using LiveOrLiveServer.Models.Dto;

namespace LiveOrLiveServer.Models {
    public class Player(string username, string? connectionId, int defaultLives) : ConnectedClient(username, connectionId) {
        // Needed to keep track of players who have left without kicking them for disconnects
        public bool InGame { get; set; } = true;
        public int Lives { get; set; } = defaultLives;
        public List<Item> Items { get; set; } = [];
        public bool IsSkipped { get; set; } = false;
        public bool IsRicochet { get; set; } = false;
        public bool ImmuneToSkip { get; set; } = false;
        public int ReviveCount { get; set; } = 0;
        public bool Eliminated { get; set; } = false;

        private readonly int _defaultLives = defaultLives;

        /// <summary>
        /// Resets this player to defaults (clears items, resets lives, etc.).
        /// </summary>
        /// <returns>Returns this instance, to optionally simplify using <c>.Select()</c> in LINQ.</returns>
        public Player ResetDefaults() {
            InGame = true;
            Lives = _defaultLives;
            Items = [];
            IsSkipped = false;
            IsRicochet = false;
            ImmuneToSkip = true;
            ReviveCount = 0;
            Eliminated = false;

            return this;
        }

        public PlayerDto ToDto(bool showRicochets) {
            return new PlayerDto {
                Username = Username,
                JoinTime = JoinTime,
                ClientType = ClientType,
                InGame = InGame,
                Lives = Lives,
                Items = Items,
                IsSkipped = IsSkipped,
                IsRicochet = IsRicochet && showRicochets,
                ImmuneToSkip = ImmuneToSkip,
                ReviveCount = ReviveCount,
                Eliminated = Eliminated,
            };
        }
    }
}
