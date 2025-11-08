using LiveOrLiveServer.Deck;
using LiveOrLiveServer.Enums;
using LiveOrLiveServer.Models;
using LiveOrLiveServer.Models.Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Tapper;

namespace LiveOrLiveServer {
    [TranspilationSource]
    public class Lobby {
        /// <summary>
        /// Unique ID for the lobby across all lobbies on the server.
        /// </summary>
        public string Id { get; }
        /// <summary>
        /// The name for the lobby. May be the same as <see cref="Id"/>.
        /// </summary>
        public string Name { get; set; }
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
        /// The list of spectators connected to this lobby.
        /// </summary>
        public List<Spectator> Spectators { get; private set; } = [];

        /// <summary>
        /// The lobby host, by username.
        /// </summary>
        public string? Host { get; set; }
        /// <summary>
        /// Whether or not the game is in progress.
        /// </summary>
        public bool GameStarted { get; set; } = false;
        /// <summary>
        /// Tracks whether or not sudden death is enabled
        /// </summary>
        public bool SuddenDeathActivated { get; set; } = false;

        /// <summary>
        /// Tracks how much damage the next shot should do, if it's a live (modified by double damage).
        /// </summary>
        [JsonIgnore]
        public int DamageForShot { get; set; } = 1;

        /// <summary>
        /// A merging of <see cref="Players"/> and <see cref="Spectators"/>.
        /// </summary>
        [JsonIgnore]
        public List<ConnectedClient> Clients => [.. Players.Cast<ConnectedClient>().Union(Spectators)];

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
        /// Adds a chat message. Does not verify the username is valid.
        /// </summary>
        /// <param name="author">The author of the message by username.</param>
        /// <param name="content">The message content.</param>
        /// <returns>The author and content as <see cref="ChatMessage"/.></returns>
        public ChatMessage AddChatMessage(string author, string content) => _chat.AddMessage(author, content);
        /// <summary>
        /// Adds a game log message to the lobby.
        /// </summary>
        /// <param name="content">The game log message content.</param>
        /// <returns>The passed string cast to a <see cref="GameLogMessage"/>.</returns>
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
        /// <returns>The turn order for the started game.</returns>
        public List<string> StartGame() {
            GameStarted = true;
            ResetManagers();
            _gameLog.Clear();
            _itemDeck.Initialize(Players.Count);

            return _turnOrderManager.TurnOrder;
        }

        /// <summary>
        /// Ends the game, resetting player state, and determining a winner.
        /// </summary>
        /// <returns>See <see cref="EndGameResult"/>.</returns>
        public EndGameResult EndGame() {
            string? winner;

            // If the game ended because people left, there is no winner
            if (Players.Count(player => player.InGame) <= 1) {
                winner = null;
            } else {
                winner = Players.FirstOrDefault(player => player.Lives >= 1)?.Username ?? "nobody";
            }

            GameStarted = false;
            // Filter out players who are no longer in the game, and reset those who are left
            // Need to update here so the pre-game can display accurate information about player counts
            var purgedPlayers = Players.Where(player => !player.InGame).Select(player => player.Username).ToList();
            Players = [.. Players
                .Where(player => player.InGame)
                .Select(player => player.ResetDefaults())];

            return new EndGameResult {
                Winner = winner,
                PurgedPlayers = purgedPlayers
            };
        }

        /// <summary>
        /// Starts a new round, reloading the chamber and refreshing the item deck. <c>NewTurn</c> should be called immediately after.
        /// </summary>
        /// <returns>See <see cref="NewRoundResult"/></returns>
        public NewRoundResult NewRound() {
            var dealtItems = new Dictionary<string, List<Item>>();

            _itemDeck.Refresh();
            foreach (Player player in Players) {
                var items = _itemDeck.DealItemsToPlayer(player);
                if (Settings.DisableDealReverseAndRicochetWhenTwoPlayers) {
                    items = items.Select(item => (item == Item.Ricochet || item == Item.ReverseTurnOrder) ? Item.Invert : item).ToList();
                }
                dealtItems.Add(player.Username, items);

                if (Settings.LoseSkipAfterRound) {
                    player.IsSkipped = false;
                    // Don't reset immunity, provides buffs to players skipped at the wrong time (TODO or maybe make it a setting?)
                }
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

            // No longer immune to being skipped once they get a turn
            if (!Settings.AllowSequentialSkips) {
                _turnOrderManager.GetPlayerForCurrentTurn().ImmuneToSkip = false;
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
            var damage = (int)bulletType * DamageForShot;
            AddLives(target, -damage);
            // Always reset anyways, no point in checking
            DamageForShot = 1;

            return new ShootPlayerResult {
                BulletFired = bulletType,
                Damage = damage,
                ShotSelf = shooter == target,
                Killed = target.Lives <= 0,
                Eliminated = target.Eliminated
            };
        }

        /// <summary>
        /// Removes an item from the specified player and puts it back in the item deck, if successful.
        /// </summary>
        /// <param name="player">The player to remove the item from.</param>
        /// <param name="item">The item to remove.</param>
        /// <returns>Whether or not the item was removed and put back in the deck successfully.</returns>
        public bool RemoveItemFromPlayer(Player player, Item item) {
            var success = player.Items.Remove(item);
            if (success) {
                _itemDeck.PutItemBack(item);
            }
            return success;
        }

        /// <summary>
        /// Reverses the turn order. Wrapper around <see cref="TurnOrderManager.ReverseTurnOrder"/>.
        /// </summary>
        public void ReverseTurnOrder() {
            _turnOrderManager.ReverseTurnOrder();
        }

        /// <summary>
        /// Removes a bullet from the chamber.
        /// </summary>
        /// <returns>The bullet that was removed. See <see cref="RackChamberResult"/>.</returns>
        public RackChamberResult RackChamber() {
            return new RackChamberResult {
                BulletType = _ammoDeck.Pop()
            };
        }

        /// <summary>
        /// Add lives to a player, taking into account settings
        /// </summary>
        /// <param name="player">The player to add lives to.</param>
        /// <param name="add">The amount of lives to add. Can be negative.</param>
        /// <param name="allowExceedMax">Whether or not <see cref="Settings.MaxLives"/> should be bypassed. Used for <see cref="Settings.AllowLifeGambleExceedMax"/>.</param>
        public void AddLives(Player player, int add, bool allowExceedMax = false) {
            player.Lives = Math.Clamp(player.Lives + add, 0, allowExceedMax ? int.MaxValue : Settings.MaxLives);

            // This is also used for players getting shot or life gambling, so we need to check for dead status.
            if (player.Lives <= 0) {
                if (player.ReviveCount >= Settings.MaxPlayerRevives) {
                    player.Eliminated = true;
                }
            }
        }

        /// <summary>
        /// Gives an extra life to the specified player. Does not go past <see cref="Settings.MaxLives"/>.
        /// </summary>
        /// <param name="player">The player to give a life to.</param>
        public void GiveExtraLife(Player player) {
            if (player.Lives <= 0) {
                player.ReviveCount++;
            }
            AddLives(player, 1);
        }

        /// <summary>
        /// Gives/removes lives from the specified player according to <see cref="Settings.LifeGambleWeights"/>.
        /// </summary>
        /// <param name="player">The player to gamble lives on.</param>
        /// <returns>The gain/loss of lives for the player. See <see cref="LifeGambleResult"/>.</returns>
        public LifeGambleResult LifeGamble(Player player) {
            var random = new Random();
            var options = new List<int>();

            foreach (var weights in Settings.LifeGambleWeights) {
                options.AddRange(Enumerable.Repeat(weights.Key, weights.Value));
            }
            var roll = options[random.Next(options.Count)];
            AddLives(player, roll, Settings.AllowLifeGambleExceedMax);

            return new LifeGambleResult {
                LifeChange = roll,
                Dead = player.Lives <= 0,
                Eliminated = player.Eliminated
            };
        }

        /// <summary>
        /// Inverts the chamber round. Wrapper around <see cref="AmmoDeck.InvertChamber"/>.
        /// </summary>
        public void InvertChamber() {
            _ammoDeck.InvertChamber();
        }

        /// <summary>
        /// Peeks the chamber round. Wrapper around <see cref="AmmoDeck.PeekChamber"/>.
        /// </summary>
        /// <returns>The current chamber round type. See <see cref="ChamberCheckResult"/>.</returns>
        public ChamberCheckResult PeekChamber() {
            var result = _ammoDeck.PeekChamber();
            return new ChamberCheckResult {
                ChamberRoundType = result
            };
        }

        /// <summary>
        /// Enables double damage (increases by 1, doesn't check for stacking).
        /// </summary>
        public void SetDoubleDamage() {
            DamageForShot += 1;
        }
        
        /// <summary>
        /// Marks the specified player as skipped.
        /// </summary>
        /// <param name="player">The player to skip.</param>
        public void SkipPlayer(Player player) {
            player.IsSkipped = true;
            if (!Settings.AllowSequentialSkips) {
                player.ImmuneToSkip = true;
            }
        }

        /// <summary>
        /// Marks a player as ricocheted.
        /// </summary>
        /// <param name="player">The player to protect with ricochet.</param>
        public void RicochetPlayer(Player player) {
            player.IsRicochet = true;
        }

        /// <summary>
        /// Registers a <see cref="ConnectedClient"/> object with the lobby by username. Handles assigning an existing, not in-game client, otherwise makes a new object. Call this when a client is connecting to the lobby.
        /// Callers of this should check the client doesn't already exist in the lobby first.
        /// </summary>
        /// <param name="connectionId">Connection ID of the player from SignalR, to be associated with the <see cref="ConnectedClient"/> instance.</param>
        /// <param name="username">Username of the client.</param>
        /// <returns>Either <see cref="Player"/> or <see cref="Spectator"/>, depending on <see cref="GameStarted"/>.</returns>
        /// <exception cref="Exception">Thrown if a client with that username already exists and is in-game.</exception>
        public ConnectedClient AddClient(string connectionId, string username) {
            if (TryGetClientByUsername(username, out var client)) {
                // Mark as in-game if player. All Spectators are assumed to be in-game.
                switch (client) {
                    case Player player when player.InGame:
                        throw new Exception("Client already exists and is in-game");
                    case Player player:
                        player.InGame = true;
                        break;
                }
                client.ConnectionId = connectionId;
            } else {
                client = GameStarted
                    ? new Spectator(username, connectionId)
                    : new Player(username, connectionId, Settings.DefaultLives);

                if (client is Player newPlayer) {
                    Players.Add(newPlayer);
                } else if (client is Spectator newSpectator) {
                    Spectators.Add(newSpectator);
                }
            }
            return client;
        }

        public ChangePlayerToSpectatorResult ChangePlayerToSpectator(Player player) {
            if (GameStarted) {
                _turnOrderManager.RemovePlayer(player);
            }
            Players.Remove(player);
            var newSpectator = new Spectator(player.Username, player.ConnectionId);
            Spectators.Add(newSpectator);

            return new ChangePlayerToSpectatorResult {
                NewSpectator = newSpectator,
                ForfeitTurn = CurrentTurn == player.Username
            };
        }

        public ChangeSpectatorToPlayerResult ChangeSpectatorToPlayer(Spectator spectator) {
            Spectators.Remove(spectator);
            var newPlayer = new Player(spectator.Username, spectator.ConnectionId, Settings.DefaultLives);
            Players.Add(newPlayer);

            return new ChangeSpectatorToPlayerResult {
                NewPlayer = newPlayer
            };
        }

        /// <summary>
        /// Attempts to fetch a player from this lobby by username.
        /// </summary>
        /// <param name="username">The username to search by.</param>
        /// <param name="player">The <see cref="Player"/> instance for the matched player, or <c>null</c> if not found.</param>
        /// <returns>A boolean matching if the player could be found.</returns>
        public bool TryGetPlayerByUsername(string username, [NotNullWhen(true)] out Player? player) {
            player = Players.FirstOrDefault(player => player.Username == username);
            return player != null;
        }

        /// <summary>
        /// Attempts to fetch a client from this lobby by username.
        /// </summary>
        /// <param name="username">The username to search by.</param>
        /// <param name="client">The <see cref="ConnectedClient"/> instance for the matched client, or <c>null</c> if not found.</param>
        /// <returns>A boolean matching if the client could be found.</returns>
        public bool TryGetClientByUsername(string username, [NotNullWhen(true)] out ConnectedClient? client) {
            client = Clients.FirstOrDefault(client => client.Username == username);
            return client != null;
        }
    }
}
