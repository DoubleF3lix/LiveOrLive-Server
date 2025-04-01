using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using liveorlive_server.Deck;
using Tapper;

namespace liveorlive_server {
    [TranspilationSource]
    public class Lobby {
        public string Id { get; set; } = GenerateId();
        public string Name { get; set; }
        public bool Hidden { get; set; } = false;
        public long CreationTime { get; } = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;

        public Config Config { get; private set; }
        public List<Player> Players { get; private set; } = [];

        public string? Host { get; set; }
        public bool GameStarted { get; set; } = new Random().Next(2) == 0; // false

        private readonly Chat chat = new();
        private TurnOrderManager turnOrderManager;
        private ItemDeck itemDeck;
        private AmmoDeck ammoDeck;

        [JsonIgnore]
        public List<ChatMessage> ChatMessages => this.chat.Messages;
        public ChatMessage AddChatMessage(string author, string content) => this.chat.AddMessage(author, content); 

        public Lobby(Config? config = null, string? name = null) {
            this.Config = config ?? new Config();
            this.Name = name ?? this.Id;
            this.ResetManagers();
        }

        [MemberNotNull(nameof(turnOrderManager), nameof(itemDeck), nameof(ammoDeck))]
        public void ResetManagers() {
            this.itemDeck = new ItemDeck(this.Config, this.Players.Count);
            this.ammoDeck = new AmmoDeck(this.Config);
            this.turnOrderManager = new TurnOrderManager();
        }

        public void SetConfig(Config config) {
            if (!this.GameStarted) {
                this.Config = config;
            }
        }

        public bool TryGetPlayerByUsername(string username, [NotNullWhen(true)] out Player? player) {
            player = this.Players.FirstOrDefault(player => player.Username == username);
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
