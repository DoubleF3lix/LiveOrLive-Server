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

        public Settings Config { get; private set; }
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

        public Lobby(Settings? config = null, string? name = null) {
            this.Config = config ?? new Settings();
            this.Name = name ?? this.Id;
            this.ResetManagers();
        }

        [MemberNotNull(nameof(turnOrderManager), nameof(itemDeck), nameof(ammoDeck))]
        public void ResetManagers() {
            this.itemDeck = new ItemDeck(this.Config);
            this.ammoDeck = new AmmoDeck(this.Config);
            this.turnOrderManager = new TurnOrderManager();
        }

        public void SetConfig(Settings config) {
            if (!this.GameStarted) {
                this.Config = config;
            }
        }

        // Handles assigning an existing player, otherwise makes a new one
        // Callers of this should check the player doesn't exist first
        public Player AddPlayer(string connectionId, string username) {
            if (this.TryGetPlayerByUsername(username, out var player)) {
                if (player.InGame) {
                    throw new Exception("Player already exists and is in-game");
                }
                player.InGame = true;
                player.connectionId = connectionId;
            } else {
                player = new Player(this.Config, username, connectionId, this.GameStarted);
                this.Players.Add(player);
                this.itemDeck.Populate(this.Players.Count); // TODO temp
                this.itemDeck.DealItemsToPlayer(player);
            }
            return player;
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
