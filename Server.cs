using liveorlive_server.Extensions;
using liveorlive_server.HubPartials;
using liveorlive_server.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;


namespace liveorlive_server {
    public class Server {
        readonly JsonSerializerOptions JSON_OPTIONS = new() { IncludeFields = true, WriteIndented = true };
        readonly WebApplication app;

        public static HashSet<Lobby> Lobbies = [];
        
        public Server() {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddSignalR();
            builder.Services.AddCors(options => {
                options.AddPolicy(name: "_allowClientOrigins", policy => {
                    policy
                        .WithOrigins("http://doublef3lix.github.io", "https://doublef3lix.github.io")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            this.app = builder.Build();
            app.UseCors("_allowClientOrigins");

            app.MapHub<LiveOrLiveHub>("");

            Lobbies.Add(new Lobby(name: "Jellyfish Sparkle"));
            Lobbies.Add(new Lobby(name: "Onion Creek"));
            Lobbies.Add(new Lobby(name: "Butterscotch Pancake"));
            Lobbies.Add(new Lobby());
            Lobbies.Add(new Lobby(name: "Pastrami Pizza"));

            app.MapGet("/lobbies", () => { return JsonSerializer.Serialize(Lobbies.Where(lobby => !lobby.hidden), JSON_OPTIONS); });
            app.MapPost("/lobbies", (CreateLobbyRequest request) => {
                if (request.Username == null) {
                    return Results.BadRequest("Username not supplied");
                }
                if (request.Username.Length > 20 || request.Username.Length < 3) {
                    return Results.BadRequest("Username must be between 3 and 20 characters");
                }

                // Config is default initialized by auto-binding magic if it isn't set
                // And if any properties are missing, they get their default values too
                var newLobby = new Lobby(request.Config, request.LobbyName);
                Lobbies.Add(newLobby);

                return Results.Ok(new CreateLobbyResponse { LobbyId = newLobby.id });
            });
            app.MapGet("/verify-lobby-connection-info", (HttpContext context) => {
                var lobbyId = context.GetStringQueryParam("lobbyId");
                var username = context.GetStringQueryParam("username");

                var errorMessage = ValidateLobbyConnectionInfo(lobbyId, username);
                if (errorMessage != null) {
                    return Results.BadRequest(errorMessage);
                }

                return Results.Ok();
            });
        }

        public static string? ValidateLobbyConnectionInfo(string? lobbyId, string? username) {
            if (lobbyId == null || username == null) {
                return "Missing lobbyId or username";
            }

            var lobby = Lobbies.FirstOrDefault(lobby => lobby.id == lobbyId);
            if (lobby == null) {
                return "Couldn't locate lobby";
            }

            if (username.Length > 20 || username.Length < 3) {
                return "Username must be between 3 and 20 characters";
            }

            var existingPlayerWithUsername = lobby.players.FirstOrDefault(player => player.username == username);
            if (existingPlayerWithUsername != null && existingPlayerWithUsername.inGame) {
                return "Username is already taken";
            }

            // No error
            return null;
        }

        public static Lobby GetLobbyById(string lobbyId) {
            if (TryGetLobbyById(lobbyId, out var result)) {
                return result;
            }
            throw new InvalidOperationException();
        }

        public static bool TryGetLobbyById(string? lobbyId, [NotNullWhen(true)] out Lobby? lobby) {
            lobby = Lobbies.FirstOrDefault(lobby => lobby.id == lobbyId);
            return lobby != null;
        }

        public async Task Start(string url, int port) {
            await this.app.RunAsync($"http://{url}:{port}");
        }

        /*
        // All clients are held in here, and this function only exits when they disconnect
        private async Task ClientConnection(WebSocket webSocket, string ID) {
            // Sometimes, clients can get stuck in a bugged closed state. This will attempt to purge them.
            // Two phase to avoid looping over while removing it
            List<Client> clientsToRemove = [];
            foreach (Client c in this.connectedClients) {
                if (c.webSocket.State == WebSocketState.Closed) {
                    clientsToRemove.Add(c);
                }
            }
            foreach (Client c in clientsToRemove) {
                this.connectedClients.Remove(c);
            }

            Client client = new(webSocket, this, ID);
            this.connectedClients.Add(client);

            // Constantly check for messages
            var buffer = new byte[1024 * 4];
            try {
                while (true) {
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    // Server only supports stringified JSON
                    if (result.MessageType == WebSocketMessageType.Text) {
                        // Decode it to an object and pass it off
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        try {
                            IClientPacket packet = JsonConvert.DeserializeObject<IClientPacket>(message, new PacketJSONConverter())!;
                            await this.PacketReceived(client, packet);
                        } catch (JsonSerializationException e) {
                            await Console.Out.WriteLineAsync($"ERROR: Got malformed packet! \n{e.Message}\n");
                        }
                    } else if (result.MessageType == WebSocketMessageType.Close || webSocket.State == WebSocketState.Aborted) {
                        await webSocket.CloseAsync(
                            result.CloseStatus ?? WebSocketCloseStatus.InternalServerError,
                            result.CloseStatusDescription, CancellationToken.None);
                        break;
                    }
                }
            } catch (WebSocketException) {
                // Abormal disconnection, finally block has us covered
            } finally {
                this.connectedClients.Remove(client);
                await this.HandlePlayerDisconnect(client);
                client.OnDisconnect();
            }
        }

        private async Task HandlePlayerDisconnect(Client client) {
            if (client.player == null) {
                return;
            }

            // If they're the host, try to pass that status on to someone else
            // If they don't have a player assigned, don't bother
            if (client.player.username == this.gameData.host) {
                if (this.connectedClients.Any(client => client.player != null)) {
                    // Guarunteed to exist due to above condition, safe to use !
                    Player newHost = this.connectedClients[0].player!;
                    this.gameData.host = newHost.username;
                    await this.Broadcast(new HostSetPacket { username = this.gameData.host });
                } else {
                    this.gameData.host = null;
                }
            }

            // Tell everyone they left for UI updating purposes
            await this.Broadcast(new PlayerLeftPacket { username = client.player.username });
            client.player.inGame = false;

            // If the game hasn't started, just remove them entirely
            if (!this.gameData.gameStarted) {
                Player? playerToRemove = this.gameData.GetPlayerByUsername(client.player.username);
                if (playerToRemove != null) {
                    this.gameData.players.Remove(playerToRemove);
                }
            } else {
                // If there is only one actively connected player and the game is in progress, end it
                if (this.connectedClients.Where(client => client.player != null).Count() <= 1) {
                    await Console.Out.WriteLineAsync("Everyone has left the game. Ending with no winner.");
                    await this.EndGame();
                    // Otherwise, if the current turn left, make them forfeit their turn
                } else if (client.player != null && client.player.username == this.gameData.CurrentTurn) {
                    await this.ForfeitTurn(client.player);
                }
            }
        }

        private async Task PacketReceived(Client sender, IClientPacket packet) {
            await Console.Out.WriteLineAsync($"Received packet from {sender}: {packet}");
            // Before the player is in the game but after they're connected
            if (sender.player == null) {
                switch (packet) {
                    case JoinGamePacket joinGamePacket:
                        // Check max length
                        if (joinGamePacket.username.Length > 20) {
                            await sender.SendMessage(new PlayerJoinRejectedPacket { reason = "That username is too long. Please choose another." });
                            return;
                        } else if (joinGamePacket.username.Length < 3) {
                            await sender.SendMessage(new PlayerJoinRejectedPacket { reason = "That username is too short. Please choose another." });
                            return;
                        }

                        // Check if the username was available
                        bool usernameTaken = this.gameData.players.Any(player => player.username == joinGamePacket.username);
                        if (!usernameTaken) {
                            sender.player = new Player(new Config(), joinGamePacket.username, "", this.gameData.gameStarted);
                            this.gameData.players.Add(sender.player);
                            // If it's not, check if the username is still logged in. If so, error, if not, assume it's the player logging back in
                        } else {
                            Player takenPlayer = this.gameData.players.First(player => player.username == joinGamePacket.username);
                            if (takenPlayer.inGame) {
                                await sender.SendMessage(new PlayerJoinRejectedPacket { reason = "That username is already taken. Please choose another." });
                                return;
                            } else {
                                sender.player = takenPlayer;
                            }
                        }

                        // Either the client is new, or they're taking an existing player object
                        // Either way, we're good to go at this point
                        sender.player.inGame = true;

                        await this.Broadcast(new PlayerJoinedPacket { player = sender.player });
                        // If they're the first player, mark them as the host
                        if (this.gameData.GetActivePlayers().Count == 1) {
                            // TODO make SetHostPacket send a chat message
                            await this.Broadcast(new HostSetPacket { username = sender.player.username });
                            this.gameData.host = sender.player.username;
                        }

                        break;
                    default:
                        throw new Exception($"Invalid packet type (without player instance) of \"{packet.packetType}\". Did you forget to implement this?");
                }
            } else {
                switch (packet) {
                    case SendNewChatMessagePacket sendNewChatMessagePacket:
                        ChatMessage message = this.chat.AddMessage(sender.player, sendNewChatMessagePacket.content);
                        await this.Broadcast(new NewChatMessageSentPacket { message = message });
                        break;
                    case ChatMessagesRequestPacket getChatMessagesPacket:
                        await sender.SendMessage(new ChatMessagesSyncPacket { messages = this.chat.GetMessages() });
                        break;
                    case GameLogMessagesRequestPacket getGameLogMessagesPacket:
                        await sender.SendMessage(new GameLogMessagesSyncPacket { messages = this.gameLog.GetMessages() });
                        break;
                    case GameDataRequestPacket gameInfoRequestPacket:
                        await sender.SendMessage(new GameDataSyncPacket { gameData = this.gameData });
                        break;
                    case StartGamePacket startGamePacket:
                        if (sender.player.username == this.gameData.host) {
                            if (this.gameData.gameStarted) {
                                await sender.SendMessage(new ActionFailedPacket { reason = "The game is already started. How did you even manage that?" });
                            } else if (this.connectedClients.Count >= 2 && this.gameData.GetActivePlayers().Count >= 2) {
                                await this.StartGame();
                            } else {
                                await sender.SendMessage(new ActionFailedPacket { reason = "There needs to be at least 2 players to start a game" });
                            }
                        }
                        break;
                    case KickPlayerPacket kickPlayerPacket:
                        if (sender.player.username == this.gameData.host) {
                            if (sender.player.username == kickPlayerPacket.username) {
                                await sender.SendMessage(new ActionFailedPacket { reason = "Why are you trying to kick yourself? Sounds painful." });
                                return;
                            }

                            // Search for the correct client and kick them
                            Client? clientToKick = this.connectedClients.Find(client => client.player != null && client.player.username == kickPlayerPacket.username);
                            // Ignore so we don't crash trying to kick a ghost player
                            if (clientToKick == null || clientToKick.player == null) {
                                return;
                            }
                            Player target = clientToKick.player;
                            // End this players turn (checks for game end)
                            await this.PostActionTransition(true);
                            // Eliminate them to handle adjusting turn order (have to do this after otherwise we skip two players)
                            this.gameData.EliminatePlayer(target);
                            // Discard the player entirely since they're likely not welcome back
                            this.gameData.players.Remove(target);

                            // Send currentTurn to avoid a game data sync (otherwise UI doesn't work properly)
                            await this.Broadcast(new PlayerKickedPacket { username = target.username, currentTurn = this.gameData.CurrentTurn });
                            await this.SendGameLogMessage($"{target.username} has been kicked.");

                            // Actually disconnected them, which runs handleClientDisconnect
                            // This removes the client from connectedClients, and checks for game end or host transference (though host transfer should never occur on kick since the host cannot kick themselves)
                            await clientToKick.webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "playerKicked", new CancellationToken());
                        }
                        break;
                    case ShootPlayerPacket shootPlayerPacket:
                        // Make sure it's the players turn
                        if (sender.player == this.gameData.GetCurrentPlayerForTurn()) {
                            Player? target = this.gameData.GetPlayerByUsername(shootPlayerPacket.target);

                            if (target == null) {
                                await sender.SendMessage(new ActionFailedPacket { reason = "Invalid player for steal item target" });
                            } else {
                                bool isTurnEndingMove = await this.ShootPlayer(sender.player, target);
                                await this.PostActionTransition(isTurnEndingMove);
                            }
                        }
                        break;
                    case UseSkipItemPacket useSkipItemPacket:
                        if (sender.player == this.gameData.GetCurrentPlayerForTurn()) {
                            Player? target = this.gameData.GetPlayerByUsername(useSkipItemPacket.target);
                            await this.UseSkipItem(sender, target);
                        }
                        break;
                    case UseDoubleDamageItemPacket useDoubleDamageItemPacket:
                        if (sender.player == this.gameData.GetCurrentPlayerForTurn()) {
                            await this.UseDoubleDamageItem(sender);
                        }
                        break;
                    case UseCheckBulletItemPacket useCheckBulletItemPacket:
                        if (sender.player == this.gameData.GetCurrentPlayerForTurn()) {
                            await this.UseCheckBulletItem(sender);
                        }
                        break;
                    case UseRebalancerItemPacket useRebalancerItemPacket:
                        if (sender.player == this.gameData.GetCurrentPlayerForTurn()) {
                            await this.UseRebalancerItem(sender, useRebalancerItemPacket.ammoType);
                        }
                        break;
                    case UseAdrenalineItemPacket useAdrenalineItemPacket:
                        if (sender.player == this.gameData.GetCurrentPlayerForTurn()) {
                            await this.UseAdrenalineItem(sender);
                        }
                        break;
                    case UseAddLifeItemPacket useAddLifeItemPacket:
                        if (sender.player == this.gameData.GetCurrentPlayerForTurn()) {
                            await this.UseAddLifeItem(sender);
                        }
                        break;
                    case UseQuickshotItemPacket useQuickshotItemPacket:
                        if (sender.player == this.gameData.GetCurrentPlayerForTurn()) {
                            await this.UseQuickshotItem(sender);
                        }
                        break;
                    case UseStealItemPacket useStealItemPacket:
                        if (sender.player == this.gameData.GetCurrentPlayerForTurn()) {
                            Player? target = this.gameData.GetPlayerByUsername(useStealItemPacket.target);
                            await this.UseStealItem(sender, target, useStealItemPacket.item, useStealItemPacket.ammoType, useStealItemPacket.skipTarget);
                        }
                        break;
                    default:
                        throw new Exception($"Invalid packet type (with player instance) of \"{packet.packetType}\". Did you forget to implement this?");
                }
            }
        }

        private async Task<bool> UseSkipItem(Client sender, Player? target, bool checkForItem = true) {
            if (sender.player == null) {
                await sender.SendMessage(new ActionFailedPacket { reason = "You... don't exist?" });
                return false;
            }

            if (target == null) {
                await sender.SendMessage(new ActionFailedPacket { reason = "Invalid player for steal item target" });
                return false;
            }

            if (target.isSkipped) {
                await sender.SendMessage(new ActionFailedPacket { reason = $"{target.username} has already been skipped!" });
                return false;
            }
            if (!checkForItem || sender.player.items.Remove(Item.Skip)) {
                await this.Broadcast(new SkipItemUsedPacket { target = target.username });
                await this.SendGameLogMessage($"{target.username} has been skipped by {sender.player.username}");
                target.isSkipped = true;
                return true;
            } else {
                await sender.SendMessage(new ActionFailedPacket { reason = "You don't have a Skip item!" });
                return false;
            }
        }

        private async Task<bool> UseDoubleDamageItem(Client sender, bool checkForItem = true) {
            if (sender.player == null) {
                await sender.SendMessage(new ActionFailedPacket { reason = "You... don't exist?" });
                return false;
            }

            if (this.gameData.damageForShot != 1) {
                await sender.SendMessage(new ActionFailedPacket { reason = "You've already used a Double Damage item for this shot!" });
                return false;
            }
            if (!checkForItem || sender.player.items.Remove(Item.DoubleDamage)) {
                await this.Broadcast(new DoubleDamageItemUsedPacket());
                await this.SendGameLogMessage($"{sender.player.username} has used a Double Damage item");
                this.gameData.damageForShot = 2;
                return true;
            } else {
                await sender.SendMessage(new ActionFailedPacket { reason = "You don't have a Double Damage item!" });
                return false;
            }
        }

        private async Task<bool> UseCheckBulletItem(Client sender, bool checkForItem = true) {
            if (sender.player == null) {
                await sender.SendMessage(new ActionFailedPacket { reason = "You... don't exist?" });
                return false;
            }

            if (!checkForItem || sender.player.items.Remove(Item.CheckBullet)) {
                BulletType peekResult = this.gameData.PeekAmmoFromChamber();
                await sender.SendMessage(new CheckBulletItemResultPacket { result = peekResult });
                await this.Broadcast(new CheckBulletItemUsedPacket());
                await this.SendGameLogMessage($"{sender.player.username} has used a Chamber Check item");
                return true;
            } else {
                await sender.SendMessage(new ActionFailedPacket { reason = "You don't have a Chamber Check item!" });
                return false;
            }
        }

        private async Task<bool> UseRebalancerItem(Client sender, BulletType ammoType, bool checkForItem = true) {
            if (sender.player == null) {
                await sender.SendMessage(new ActionFailedPacket { reason = "You... don't exist?" });
                return false;
            }

            if (!checkForItem || sender.player.items.Remove(Item.Rebalancer)) {
                int count = this.gameData.AddAmmoToChamberAndShuffle(ammoType);
                await this.Broadcast(new RebalancerItemUsedPacket { ammoType = ammoType, count = count });
                await this.SendGameLogMessage($"{sender.player.username} has used a Rebalancer item and added {count} {ammoType.ToString().ToLower()} rounds");
                return true;
            } else {
                await sender.SendMessage(new ActionFailedPacket { reason = "You don't have a Rebalancer item!" });
                return false;
            }
        }

        private async Task<bool> UseAdrenalineItem(Client sender, bool checkForItem = true) {
            if (sender.player == null) {
                await sender.SendMessage(new ActionFailedPacket { reason = "You... don't exist?" });
                return false;
            }

            if (!checkForItem || sender.player.items.Remove(Item.Adrenaline)) {
                int result = new Random().Next(2) * 2 - 1; // Coin flip
                await this.Broadcast(new AdrenalineItemUsedPacket { result = result });
                await this.SendGameLogMessage($"{sender.player.username} has used an Adrenaline item and {(result > 0 ? "gained" : "lost")} a life");
                sender.player.lives += result;
                await this.CheckAndEliminatePlayer(sender.player);

                // This is only here to handle game end (no shot was taken so a round won't be started, and it won't move to the next turn)
                await this.PostActionTransition(false);

                return true;
            } else {
                await sender.SendMessage(new ActionFailedPacket { reason = "You don't have an Adrenaline item!" });
                return false;
            }
        }

        // Can't be stolen, don't need to worry about not checking
        private async Task<bool> UseAddLifeItem(Client sender) {
            if (sender.player == null) {
                await sender.SendMessage(new ActionFailedPacket { reason = "You... don't exist?" });
                return false;
            }

            if (sender.player.items.Remove(Item.AddLife)) {
                await this.Broadcast(new AddLifeItemUsedPacket());
                await this.SendGameLogMessage($"{sender.player.username} has used a +1 Life item");
                sender.player.lives++;
                return true;
            } else {
                await sender.SendMessage(new ActionFailedPacket { reason = "You don't have a +1 Life item!" });
                return false;
            }
        }

        private async Task<bool> UseQuickshotItem(Client sender, bool checkForItem = true) {
            if (sender.player == null) {
                await sender.SendMessage(new ActionFailedPacket { reason = "You... don't exist?" });
                return false;
            }

            if (sender.player == null) {
                await sender.SendMessage(new ActionFailedPacket { reason = "You... don't exist?" });
                return false;
            }

            if (this.gameData.quickshotEnabled) {
                await sender.SendMessage(new ActionFailedPacket { reason = "You've already used a Quickshot item for this turn!" });
                return true;
            }
            if (!checkForItem || sender.player.items.Remove(Item.Quickshot)) {
                await this.Broadcast(new QuickshotItemUsedPacket());
                await this.SendGameLogMessage($"{sender.player.username} has used a Quickshot item");
                this.gameData.quickshotEnabled = true;
                return true;
            } else {
                await sender.SendMessage(new ActionFailedPacket { reason = "You don't have a Quickshot item!" });
                return false;
            }
        }

        // Can't be stolen, don't need to worry about not checking
        private async Task<bool> UseStealItem(Client sender, Player? target, Item item, BulletType? ammoType, string? skipTargetUsername) {
            if (sender.player == null) {
                await sender.SendMessage(new ActionFailedPacket { reason = "You... don't exist?" });
                return false;
            }

            if (target == null) {
                await sender.SendMessage(new ActionFailedPacket { reason = "Invalid player for steal item target" });
                return false;
            }

            // Ensure nullable parameters aren't null when they shouldn't be
            if (item == Item.Rebalancer && ammoType == null) {
                await sender.SendMessage(new ActionFailedPacket { reason = "ERROR: ammoType was null when stealing Rebalancer. Please raise a GitHub issue." });
                return false;
            } else if (item == Item.Skip && skipTargetUsername == null) {
                await sender.SendMessage(new ActionFailedPacket { reason = "ERROR: skipTarget was null when stealing Skip. Please raise a GitHub issue." });
            }

            Player? skipTarget = this.gameData.GetPlayerByUsername(skipTargetUsername);
            if (item == Item.Skip && skipTarget == null) {
                await sender.SendMessage(new ActionFailedPacket { reason = "Invalid player for skip target" });
                return false;
            }

            // Not Remove since we only remove if it the child item was a success
            if (sender.player.items.Contains(Item.Pickpocket)) {
                bool useSuccess = item switch {
                    // skipTarget/ammoType is definitely not null (checks above), is safe
                    Item.Skip => await this.UseSkipItem(sender, skipTarget!, false),
                    Item.DoubleDamage => await this.UseDoubleDamageItem(sender, false),
                    Item.CheckBullet => await this.UseCheckBulletItem(sender, false),
                    Item.Rebalancer => await this.UseRebalancerItem(sender, (BulletType)ammoType!, false),
                    Item.Adrenaline => await this.UseAdrenalineItem(sender, false),
                    Item.Quickshot => await this.UseQuickshotItem(sender, false),
                    _ => false
                };

                if (useSuccess) {
                    sender.player.items.Remove(Item.Pickpocket);
                    target.items.Remove(item);
                    await this.Broadcast(new StealItemUsedPacket { target = target.username, item = item });

                    // Let each item function handle printing logs
                    // await this.sendGameLogMessage($"{sender.player.username} has used a Pickpocket item and stole {(item == Item.Adrenaline ? "an" : "a")} {item.ToString().ToLower()} item from {target.username}");

                    return true;
                }
            } else {
                await sender.SendMessage(new ActionFailedPacket { reason = "You don't have a Pickpocket item!" });
            }
            return false;
        }

        private async Task<bool> ShootPlayer(Player shooter, Player target) {
            BulletType shot = this.gameData.PullAmmoFromChamber();
            await this.Broadcast(new PlayerShotAtPacket { target = target.username, ammoType = shot, damage = this.gameData.damageForShot });

            if (shooter != target) {
                await this.SendGameLogMessage($"{shooter.username} shot {target.username} with a {shot.ToString().ToLower()} round.");
            } else {
                await this.SendGameLogMessage($"{shooter.username} shot themselves with a {shot.ToString().ToLower()} round.");
            }

            bool isTurnEndingMove = true;
            // If it's a live round, regardless of who, the turn is over
            if (shot == BulletType.Live) {
                target.lives -= this.gameData.damageForShot;
                bool wasEliminated = await this.CheckAndEliminatePlayer(target);
                if (wasEliminated && GameData.LOOTING && shooter != target) {
                    shooter.items.AddRange(target.items);
                    await this.SyncGameData(); // TODO do this proper... somehow
                } else if (wasEliminated && GameData.VENGEANCE && shooter != target) {
                    if (target.isSkipped) {
                        // Should probably be a different packet, but this is fine
                        await this.Broadcast(new SkipItemUsedPacket { target = shooter.username });
                        shooter.isSkipped = true;
                    }
                }
            } else if (target == shooter) { // Implied it was a blank round
                isTurnEndingMove = false;
            }

            this.gameData.damageForShot = 1;

            // If they had a quickshot item, don't end their turn no matter what
            if (this.gameData.quickshotEnabled) {
                isTurnEndingMove = false;
                this.gameData.quickshotEnabled = false;
            }

            // In case this triggers a round end, we pass along whether or not it was a turn ending shot, so that when a new round starts, it's still that players turn
            return isTurnEndingMove;
        }

        private async Task<bool> CheckAndEliminatePlayer(Player player) {
            if (player.lives <= 0) {
                this.gameData.EliminatePlayer(player);
                await this.SendGameLogMessage($"{player.username} has been eliminated.");
                return true;
            }
            return false;
        }

        private async Task PostActionTransition(bool isTurnEndingMove) {
            // Check for game end (if there's one player left standing)
            // Make sure the game is still going in case this triggers twice
            if (this.gameData.players.Count(player => player.isSpectator == false) <= 1 && this.gameData.gameStarted) {
                await this.EndGame();
                return;
            }

            // Check for round end
            if (this.gameData.GetAmmoInChamberCount() <= 0) {
                await this.StartNewRound();
            }

            if (isTurnEndingMove) {
                await this.NextTurn();
            }
        }

        // Player shoots themselves once and does not get to go again if it was blank
        private async Task ForfeitTurn(Player player) {
            await this.ShootPlayer(player, player);
            await this.PostActionTransition(true);
        }

        private async Task StartGame() {
            // Gives items, trigger refresh
            this.gameData.StartGame();
            await this.Broadcast(new GameStartedPacket());

            this.gameLog.Clear();
            await this.Broadcast(new GameLogMessagesSyncPacket { messages = this.gameLog.GetMessages() });

            await this.StartNewRound();
            await this.NextTurn();
        }

        private async Task NextTurn() {
            // Send the turn end packet for the previous player (if there was one) automaticaly
            if (this.gameData.CurrentTurn != null) {
                await this.Broadcast(new TurnEndedPacket { username = this.gameData.CurrentTurn });
            }

            Player playerForTurn = this.gameData.StartNewTurn();
            await this.Broadcast(new TurnStartedPacket { username = playerForTurn.username });

            if (playerForTurn.isSkipped) {
                await this.SendGameLogMessage($"{playerForTurn.username} has been skipped.");
                playerForTurn.isSkipped = false;
                await this.NextTurn();
            }

            // If the player left the game, have them shoot themselves and move on
            if (!playerForTurn.inGame) {
                await this.ForfeitTurn(playerForTurn);
            }
        }

        private async Task StartNewRound() {
            // Items are dealt out here
            List<int> ammoCounts = this.gameData.NewRound();

            await this.SendGameLogMessage($"A new round has started. The chamber has been loaded with {ammoCounts[0]} live round{(ammoCounts[0] != 1 ? "s" : "")} and {ammoCounts[1]} blank{(ammoCounts[1] != 1 ? "s" : "")}.");

            // Auto-apply +1 Life items
            foreach (Player player in this.gameData.players) {
                int extraLives = player.items.RemoveAll(item => item == Item.AddLife);
                player.lives += extraLives;
                if (extraLives > 0) {
                    await this.SendGameLogMessage($"{player.username} has been given {extraLives} {(extraLives > 1 ? "lives" : "life")} from items");
                }
            }

            await this.Broadcast(new NewRoundStartedPacket {
                players = this.gameData.players,
                liveCount = ammoCounts[0],
                blankCount = ammoCounts[1]
            });
        }

        private async Task EndGame() {
            // There's almost certainly at least one player left when this runs (used to just wipe if there was nobody left, but that situation requires 2 people to DC at once which I don't care enough to account for)
            Player? possibleWinner = this.gameData.players.Find(player => player.lives >= 1);
            string winner = possibleWinner != null ? possibleWinner.username : "nobody";

            // Copy any data we may need (like players)
            GameData newGameData = new() {
                players = this.gameData.players
                    .Where(player => player.inGame)
                    .Select(player => {
                        player.isSpectator = false;
                        player.isSkipped = false;
                        player.items.Clear();
                        player.lives = 3;
                        return player;
                    })
                    .ToList(),
                host = this.gameData.host
            };
            this.gameData = newGameData;
            await this.SyncGameData();

            await this.SendGameLogMessage($"The game has ended. The winner is {winner}.");
        }

        private async Task SyncGameData() {
            await this.Broadcast(new GameDataSyncPacket { gameData = this.gameData });
        }

        private async Task SendGameLogMessage(string content) {
            GameLogMessage message = new(content);
            this.gameLog.AddMessage(message);
            await this.Broadcast(new NewGameLogMessageSentPacket {
                message = message
            });
        }

        private async Task Broadcast(IServerPacket packet) {
            await Console.Out.WriteLineAsync($"Broadcasting {JsonConvert.SerializeObject(packet)}");
            foreach (Client client in this.connectedClients) {
                if (client.player?.inGame ?? false) {
                    await client.SendMessage(packet, false);
                }
            }
        }
        */
    }
}
