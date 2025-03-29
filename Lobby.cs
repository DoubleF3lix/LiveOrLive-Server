using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using liveorlive_server.Deck;
using Tapper;

namespace liveorlive_server {
    [TranspilationSource]
    public class Lobby {
        public string id = GenerateId();
        public string name;
        public bool hidden = false;
        public readonly long creationTime = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;

        public Config Config { get; private set; }
        public readonly List<Player> players = [];

        public string? host;
        public bool gameStarted = new Random().Next(2) == 0;

        [JsonIgnore]
        public Chat chat = new();

        private TurnOrderManager turnOrderManager;
        private ItemDeck itemDeck;
        private AmmoDeck ammoDeck;

        public Lobby(Config? config = null, string? name = null) {
            this.Config = config ?? new Config();
            this.name = name ?? this.id;
            this.ResetManagers();
        }

        [MemberNotNull(nameof(turnOrderManager), nameof(itemDeck), nameof(ammoDeck))]
        public void ResetManagers() {
            this.itemDeck = new ItemDeck(this.Config, this.players.Count);
            this.ammoDeck = new AmmoDeck(this.Config);
            this.turnOrderManager = new TurnOrderManager();
        }

        public void SetConfig(Config config) {
            if (!this.gameStarted) {
                this.Config = config;
            }
        }

        public bool TryGetPlayerByUsername(string username, [NotNullWhen(true)] out Player? player) {
            player = this.players.FirstOrDefault(player => player.username == username);
            return player != null;
        }

        public static string GenerateId() {
            var rand = new Random();
            string? id;
            do {
                /* id = Guid.NewGuid()
                    .ToString("N")
                    .ToUpper()
                    .Replace("0", "")
                    .Replace("O", "")
                    .Replace("I", "")
                    .Replace("1", "")
                    .Replace("5", "")
                    .Replace("S", "")
                    .Substring(0, 4); */
                id = rand.Next(1000, 10000).ToString();
            } while (id != "" && Server.TryGetLobbyById(id, out _));
            return id;
        }
    }
}
