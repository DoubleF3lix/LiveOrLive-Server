using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using liveorlive_server.Deck;
using liveorlive_server.Enums;
using Tapper;

namespace liveorlive_server {
    [TranspilationSource]
    public class Lobby {
        /// <summary>
        /// Unique ID for the lobby across all lobbies on the server.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The name for the lobby. May be the same as <c>Id</c>.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Whether or not this lobby will show in the lobby selector.
        /// </summary>
        public bool Private => this.Settings.Private;
        /// <summary>
        /// Unix timestamp for lobby creation.
        /// </summary>
        public long CreationTime { get; } = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;

        /// <summary>
        /// The settings for this lobby.
        /// </summary>
        public Settings Settings { get; private set; }
        /// <summary>
        /// The list of players connected to this lobby.
        /// </summary>
        public List<Player> Players { get; private set; } = [];

        /// <summary>
        /// The lobby host, by username.
        /// </summary>
        public string? Host { get; set; }
        /// <summary>
        /// Whether or not the game is in progress.
        /// </summary>
        public bool GameStarted { get; set; } = false;

        /// <summary>
        /// The turn order for this lobby.
        /// </summary>
        public List<string> TurnOrder => _turnOrderManager.TurnOrder;
        /// <summary>
        /// Wrapper around <c>TryGetPlayerForCurrentTurn(out var player).Username</c>. Used for clients so we don't have to resend the entire player object. This can be <c>null</c> if the game hasn't been started yet, as the turn order is not initialized until game start.
        /// </summary>
        public string? CurrentTurn => this._turnOrderManager.TryGetPlayerForCurrentTurn(out var player) ? player.Username : null;

        /// <summary>
        /// Gets all chat messages for this lobby.
        /// </summary>
        [JsonIgnore]
        public List<ChatMessage> ChatMessages => this._chat.Messages;
        /// <summary>
        /// Gets all game log messages for the current/most recent game for this lobby. Cleared when a new game starts.
        /// </summary>
        [JsonIgnore]
        public List<GameLogMessage> GameLogMessages => this._gameLog.Messages;
        /// <summary>
        /// Gets the four most recent game log messages.
        /// </summary>
        [JsonIgnore]
        public List<GameLogMessage> RecentGameLogMessages => this._gameLog.GetLastMessages(4);

        /// <summary>
        /// Adds a chat message. Does not verify the username is valid.
        /// </summary>
        /// <param name="author">The author of the message by username.</param>
        /// <param name="content">The message content.</param>
        /// <returns></returns>
        public ChatMessage AddChatMessage(string author, string content) => this._chat.AddMessage(author, content);
        /// <summary>
        /// Adds a game log message to the lobby.
        /// </summary>
        /// <param name="content">The game log message content.</param>
        /// <returns></returns>
        public GameLogMessage AddGameLogMessage(string content) => this._gameLog.AddMessage(content);

        private readonly Chat _chat = new();
        private readonly GameLog _gameLog = new();
        private TurnOrderManager _turnOrderManager;
        private ItemDeck _itemDeck;
        private AmmoDeck _ammoDeck;

        private Player CurrentPlayerForTurn => this._turnOrderManager.GetPlayerForCurrentTurn();

        /// <summary>
        /// Creates a new lobby with the specified settings and name.
        /// </summary>
        /// <param name="id">The ID for ths lobby. Should be generated uniquely by the server.</param>
        /// <param name="settings">The settings for this lobby. Default settings are used if <c>null</c>.</param>
        /// <param name="name">The name for this lobby. The lobby ID is used if <c>null</c>.</param>
        public Lobby(string id, Settings? settings = null, string? name = null) {
            this.Id = id;
            this.Settings = settings ?? new Settings();
            this.Name = string.IsNullOrEmpty(name) ? this.Id : name;
            this.ResetManagers();
        }

        /// <summary>
        /// Resets the item deck, chamber, and turn order manager according to game settings and player list
        /// </summary>
        [MemberNotNull(nameof(_itemDeck), nameof(_ammoDeck), nameof(_turnOrderManager))]
        public void ResetManagers() {
            this._itemDeck = new ItemDeck(this.Settings);
            this._ammoDeck = new AmmoDeck(this.Settings);
            this._turnOrderManager = new TurnOrderManager(this.Players);
        }

        /// <summary>
        /// Overrides the settings for this lobby, ignoring the input if the game is in progress.
        /// </summary>
        /// <param name="settings">A new configured <c>Settings</c> object to load.</param>
        public void SetSettings(Settings settings) {
            if (!this.GameStarted) {
                this.Settings = settings;
            }
        }

        /// <summary>
        /// Starts the game. <c>NewRound</c> should be called immediately after. This function only initializes the game state.
        /// </summary>
        public void StartGame() {
            this.GameStarted = true;
            this.ResetManagers();
            this._gameLog.Clear();
            this._itemDeck.Initialize(this.Players.Count);
        }

        /// <summary>
        /// Starts a new round, reloading the chamber and refreshing the item deck. <c>NewTurn</c> should be called immediately after.
        /// </summary>
        /// <returns>A two-tuple with the blank and live counts for the chamber reload, respectively</returns>
        public (int, int) NewRound() {
            this._itemDeck.Refresh();
            foreach (Player player in this.Players.Where(p => !p.IsSpectator)) {
                this._itemDeck.DealItemsToPlayer(player);
            }

            this._ammoDeck.Refresh();
            return (this._ammoDeck.BlankCount, this._ammoDeck.LiveCount);
        }

        public string? EndTurn() {
            if (this._turnOrderManager.TryGetPlayerForCurrentTurn(out var previousPlayer)) {
                return previousPlayer.Username; 
            }
            return null;
        }

        /// <summary>
        /// Moves the turn to the next player in-order. Handles passing through skipped players as well.
        /// </summary>
        /// <param name="onTurnStart">Called with the username of a player when their turn begins. May be called multiple times depending on skips.</param>
        /// <param name="onTurnEnd">Called with the username of a player when their turn ends, and a boolean to mark if the turn ended due to a skip. May be called multiple times depending on skips or the existence of a prior turn.</param>
        public void NewTurn(Action<string> onTurnStart, Action<string, bool> onTurnEnd) {
            // If there was a previous turn, mark them as ended
            if (this._turnOrderManager.TryGetPlayerForCurrentTurn(out var previousPlayer)) {                
                onTurnEnd(previousPlayer.Username, false);
            }

            this._turnOrderManager.Advance();

            // Keep advancing until there's not a skip
            var nextPlayer = this._turnOrderManager.GetPlayerForCurrentTurn();

            while (true) {
                onTurnStart(nextPlayer.Username);
                if (nextPlayer.IsSkipped) {
                    nextPlayer.IsSkipped = false;
                    onTurnEnd(nextPlayer.Username, true);
                    nextPlayer = this._turnOrderManager.GetPlayerForCurrentTurn();
                } else {
                    break;
                }
            }
        }

        /// <summary>
        /// Fires (pops) a round from the chamber. Wrapper around the ammo deck pop method.
        /// </summary>
        /// <returns>The bullet fired from the chamber</returns>
        public BulletType FireGun() {
            return this._ammoDeck.Pop();
        }

        public void PostActionTransition() {

        }

        /// <summary>
        /// Registers a <c>Player</c> object with the lobby by username. Handles assigning an existing, not in-game player, otherwise makes a new object. Call this when a player is connecting to the lobby.
        /// Callers of this should check the player doesn't already exist in the lobby first.
        /// If the game is in progress and the username isn't already registered with this lobby, they will be marked as a spectator.
        /// </summary>
        /// <param name="connectionId">Connection ID of the player from SignalR, to be associated with the <c>Player</c> instance.</param>
        /// <param name="username">Username of the player</param>
        /// <returns>A new <c>Player</c> instance with the passed username and connection ID</returns>
        /// <exception cref="Exception">Thrown if the player already exists and is in-game.</exception>
        public Player AddPlayer(string? connectionId, string username) {
            if (this.TryGetPlayerByUsername(username, out var player)) {
                if (player.InGame) {
                    throw new Exception("Player already exists and is in-game");
                }
                player.InGame = true;
                player.connectionId = connectionId;
            } else {
                player = new Player(this.Settings, username, connectionId, false);
                this.Players.Add(player);
            }
            return player;
        }

        /// <summary>
        /// Attempts to fetch a player from this lobby by username.
        /// </summary>
        /// <param name="username">The username to search by.</param>
        /// <param name="player">The Player instance for the matched player, or <c>null</c> if not found.</param>
        /// <returns></returns>
        public bool TryGetPlayerByUsername(string username, [NotNullWhen(true)] out Player? player) {
            player = this.Players.FirstOrDefault(player => player.Username == username);
            return player != null;
        }
    }
}
