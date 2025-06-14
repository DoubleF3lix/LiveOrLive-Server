using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using liveorlive_server.Deck;
using liveorlive_server.Enums;
using liveorlive_server.Models;
using liveorlive_server.Models.Results;
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
        public bool Private => Settings.Private;
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
        public string? CurrentTurn => _turnOrderManager.TryGetPlayerForCurrentTurn(out var player) ? player.Username : null;

        public int AmmoLeftInChamber => _ammoDeck.Count;

        /// <summary>
        /// Gets all chat messages for this lobby.
        /// </summary>
        [JsonIgnore]
        public List<ChatMessage> ChatMessages => _chat.Messages;
        /// <summary>
        /// Gets all game log messages for the current/most recent game for this lobby. Cleared when a new game starts.
        /// </summary>
        [JsonIgnore]
        public List<GameLogMessage> GameLogMessages => _gameLog.Messages;
        /// <summary>
        /// Gets the four most recent game log messages.
        /// </summary>
        [JsonIgnore]
        public List<GameLogMessage> RecentGameLogMessages => _gameLog.GetLastMessages(4);

        /// <summary>
        /// Adds a chat message. Does not verify the username is valid.
        /// </summary>
        /// <param name="author">The author of the message by username.</param>
        /// <param name="content">The message content.</param>
        /// <returns></returns>
        public ChatMessage AddChatMessage(string author, string content) => _chat.AddMessage(author, content);
        /// <summary>
        /// Adds a game log message to the lobby.
        /// </summary>
        /// <param name="content">The game log message content.</param>
        /// <returns></returns>
        public GameLogMessage AddGameLogMessage(string content) => _gameLog.AddMessage(content);

        private readonly Chat _chat = new();
        private readonly GameLog _gameLog = new();
        private TurnOrderManager _turnOrderManager;
        private ItemDeck _itemDeck;
        private AmmoDeck _ammoDeck;

        /// <summary>
        /// Creates a new lobby with the specified settings and name.
        /// </summary>
        /// <param name="id">The ID for ths lobby. Should be generated uniquely by the server.</param>
        /// <param name="settings">The settings for this lobby. Default settings are used if <c>null</c>.</param>
        /// <param name="name">The name for this lobby. The lobby ID is used if <c>null</c>.</param>
        public Lobby(string id, Settings? settings = null, string? name = null) {
            Id = id;
            Settings = settings ?? new Settings();
            Name = string.IsNullOrEmpty(name) ? Id : name;
            ResetManagers();
        }

        /// <summary>
        /// Resets the item deck, chamber, and turn order manager according to game settings and player list
        /// </summary>
        [MemberNotNull(nameof(_itemDeck), nameof(_ammoDeck), nameof(_turnOrderManager))]
        public void ResetManagers() {
            _itemDeck = new ItemDeck(Settings);
            _ammoDeck = new AmmoDeck(Settings);
            _turnOrderManager = new TurnOrderManager(Players);
        }

        /// <summary>
        /// Overrides the settings for this lobby, ignoring the input if the game is in progress.
        /// </summary>
        /// <param name="settings">A new configured <c>Settings</c> object to load.</param>
        public void SetSettings(Settings settings) {
            if (!GameStarted) {
                Settings = settings;
            }
        }

        /// <summary>
        /// Starts the game. <c>NewRound</c> should be called immediately after. This function only initializes the game state.
        /// </summary>
        public void StartGame() {
            GameStarted = true;
            ResetManagers();
            _gameLog.Clear();
            _itemDeck.Initialize(Players.Count);
        }

        /// <summary>
        /// Ends the game, resetting player state, and determining a winner.
        /// </summary>
        /// <returns>See <see cref="EndGameResult"/>.</returns>
        public EndGameResult EndGame() {
            var result = new EndGameResult();

            // If the game ended because people left, there is no winner
            if (Players.Count(player => player.InGame) <= 1) {
                result.Winner = null;
            } else {
                result.Winner = Players.FirstOrDefault(player => player.Lives >= 1)?.Username ?? "nobody";
            }

            GameStarted = false;
            // Filter out players who are no longer in the game, and reset those who are left
            Players = Players
                .Where(player => player.InGame)
                .Select(player => player.ResetDefaults())
                .ToList();

            return result;
        }

        /// <summary>
        /// Starts a new round, reloading the chamber and refreshing the item deck. <c>NewTurn</c> should be called immediately after.
        /// </summary>
        /// <returns>See <see cref="NewRoundResult"/></returns>
        public NewRoundResult NewRound() {
            var dealtItems = new Dictionary<string, List<Item>>();

            _itemDeck.Refresh();
            foreach (Player player in Players.Where(p => !p.IsSpectator)) {
                var items = _itemDeck.DealItemsToPlayer(player);
                dealtItems.Add(player.Username, items);
            }

            _ammoDeck.Refresh();
            return new NewRoundResult { 
                BlankRounds = _ammoDeck.BlankCount, 
                LiveRounds = _ammoDeck.LiveCount,
                DealtItems = dealtItems
            };
        }

        /// <summary>
        /// Moves the turn to the next player in-order. Handles passing through skipped players as well.
        /// </summary>
        /// <returns>An enumerable containing how the turn order progressed automatically (such as old turns ending, skips, etc.). See <see cref="NewTurnResult"/>.</returns>
        public IEnumerable<NewTurnResult> NewTurn() {
            // If there was a previous turn, mark them as ended
            if (_turnOrderManager.TryGetPlayerForCurrentTurn(out var previousPlayer)) {     
                yield return new EndTurnResult { PlayerUsername = previousPlayer.Username, EndDueToSkip = false };
            }

            _turnOrderManager.Advance();

            // Keep advancing until there's not a skip
            while (true) {
                var nextPlayer = _turnOrderManager.GetPlayerForCurrentTurn();
                yield return new StartTurnResult { PlayerUsername = nextPlayer.Username };

                if (nextPlayer.IsSkipped) {
                    nextPlayer.IsSkipped = false;
                    yield return new EndTurnResult { PlayerUsername = nextPlayer.Username, EndDueToSkip = true };
                    _turnOrderManager.Advance();
                } else {
                    break;
                }
            }
        }

        /// <summary>
        /// Shoots a player with a round from the chamber.
        /// </summary>
        /// <param name="shooter">The player who fired the shot</param>
        /// <param name="target">The player who was shot at. Can be the same as <c>shooter</c>.</param>
        /// <returns>The data about the shot, including what type, the damage, and whether or not they shot themselves. Doesn't include <c>shooter</c> or <c>target</c> as these are assumed to be owned by the caller. See <see cref="ShootPlayerResult"/>.</returns>
        public ShootPlayerResult ShootPlayer(Player shooter, Player target) {
            var bulletType = _ammoDeck.Pop();
            target.Lives -= (int)bulletType;

            return new ShootPlayerResult {
                BulletFired = bulletType,
                Damage = (int)bulletType,
                ShotSelf = shooter == target
            };
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
            if (TryGetPlayerByUsername(username, out var player)) {
                if (player.InGame) {
                    throw new Exception("Player already exists and is in-game");
                }
                player.InGame = true;
                player.connectionId = connectionId;
            } else {
                player = new Player(Settings, username, connectionId, false);
                Players.Add(player);
            }
            return player;
        }

        /// <summary>
        /// Attempts to fetch a player from this lobby by username.
        /// </summary>
        /// <param name="username">The username to search by.</param>
        /// <param name="player">The Player instance for the matched player, or <c>null</c> if not found.</param>
        /// <returns>A boolean matching if the player could be found.</returns>
        public bool TryGetPlayerByUsername(string username, [NotNullWhen(true)] out Player? player) {
            player = Players.FirstOrDefault(player => player.Username == username);
            return player != null;
        }
    }
}
