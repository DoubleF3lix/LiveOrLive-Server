using System.Net;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;


namespace liveorlive_server {
    public class Server {
        WebApplication app;

        List<Client> connectedClients = new List<Client>();
        GameData gameData = new GameData();
        GameLog gameLog = new GameLog();
        Chat chat = new Chat();

        public Server() {
            this.app = WebApplication.CreateBuilder().Build();
            this.app.UseWebSockets();
            
            // app.MapGet("/", () => new Microsoft.AspNetCore.Mvc.JsonResult("Test complete, can connect"));

            this.app.MapGet("/", async context => {
                // Make sure all incoming requests are WebSocket requests, otherwise send 400
                if (context.WebSockets.IsWebSocketRequest) {
                    // Get the request and pass it off to our handler
                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await this.ClientConnection(webSocket, context.Connection.Id);
                } else {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
            });
        }

        public async Task start(string url, int port) {
            await this.app.RunAsync($"http://{url}:{port}");
        }

        // All clients are held in here, and this function only exits when they disconnect
        private async Task ClientConnection(WebSocket webSocket, string ID) {
            // Sometimes, clients can get stuck in a bugged closed state. This will attempt to purge them.
            // Two phase to avoid looping over while removing it
            List<Client> clientsToRemove = new List<Client>();
            foreach (Client c in this.connectedClients) {
                if (c.webSocket.State == WebSocketState.Closed) {
                    clientsToRemove.Add(c);
                }
            }
            foreach (Client c in clientsToRemove) {
                this.connectedClients.Remove(c);
            }

            Client client = new Client(webSocket, this, ID);
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
                        ClientPacket packet = JsonConvert.DeserializeObject<ClientPacket>(message, new PacketJSONConverter())!;
                        await this.packetReceived(client, packet);
                    } else if (result.MessageType == WebSocketMessageType.Close || webSocket.State == WebSocketState.Aborted) {
                        await webSocket.CloseAsync(
                            result.CloseStatus.HasValue ? result.CloseStatus.Value : WebSocketCloseStatus.InternalServerError,
                            result.CloseStatusDescription, CancellationToken.None);
                        break;
                    }
                }
            } catch (WebSocketException) {
                // Abormal disconnection, finally block has us covered
            } finally {
                this.connectedClients.Remove(client);
                await this.handleClientDisconnect(client);
                client.onDisconnect();
            }
        }

        public async Task handleClientDisconnect(Client client) {
            // If they're the host, try to pass that status on to someone else
            // If they don't have a player assigned, don't bother
            if (client.player?.username == this.gameData.host) {
                if (this.connectedClients.Count(client => client.player != null) > 0) {
                    // Guarunteed to exist due to above condition, safe to use !
                    Player newHost = this.connectedClients[0].player!;
                    this.gameData.host = newHost.username;
                    await this.broadcast(new HostSetPacket { username = this.gameData.host });
                } else {
                    this.gameData.host = null;
                }
            }

            // If the game hasn't started, just remove them entirely
            if (!this.gameData.gameStarted) {
                if (client.player != null) {
                    this.gameData.players.Remove(this.gameData.getPlayerByUsername(client.player.username));
                    await this.syncGameData();
                }
            } else {
                // If there is only one actively connected player and the game is in progress, end it
                if (this.connectedClients.Where(client => client.player != null).Count() <= 1) {
                    await Console.Out.WriteLineAsync("Everyone has left the game. Ending with no winner.");
                    await this.endGame();
                    // Otherwise, if the current turn left, make them forfeit their turn
                } else if (client.player != null && client.player.username == this.gameData.currentTurn) {
                    await this.forfeitTurn(client.player);
                }
            }
        }

        public async Task packetReceived(Client sender, ClientPacket packet) {
            await Console.Out.WriteLineAsync($"Received packet from {sender.ToString()}: {packet}");
            // Before the player is in the game but after they're connected
            if (sender.player == null) {
                switch (packet) {
                    case JoinGamePacket joinGamePacket:
                        // Check max length
                        if (joinGamePacket.username.Length > 20) {
                            await sender.sendMessage( new PlayerJoinRejectedPacket { reason = "That username is too long. Please choose another." });
                            return;
                        } else if (joinGamePacket.username.Length < 3) {
                            await sender.sendMessage(new PlayerJoinRejectedPacket { reason = "That username is too short. Please choose another." });
                            return;
                        }

                        // Check if the username was available
                        bool usernameTaken = this.gameData.players.Any(player => player.username == joinGamePacket.username);
                        if (!usernameTaken) {
                            sender.player = new Player(joinGamePacket.username, false, this.gameData.gameStarted);
                            this.gameData.players.Add(sender.player);
                        // If it's not, check if the username is still logged in. If so, error, if not, assume it's the player logging back in
                        } else {
                            Player takenPlayer = this.gameData.players.First(player => player.username == joinGamePacket.username);
                            if (takenPlayer.inGame) {
                                await sender.sendMessage(new PlayerJoinRejectedPacket { reason = "That username is already taken. Please choose another." });
                                return;
                            } else {
                                sender.player = takenPlayer;
                            }
                        }

                        // Either the client is new, or they're taking an existing player object
                        // Either way, we're good to go at this point
                        sender.player.inGame = true;

                        await this.broadcast(new PlayerJoinedPacket { player = sender.player });
                        // If they're the first player, mark them as the host
                        if (this.gameData.getActivePlayers().Count == 1) {
                            // TODO make SetHostPacket send a chat message
                            await this.broadcast(new HostSetPacket { username = sender.player.username });
                            this.gameData.host = sender.player.username;
                        }

                        break;
                    default:
                        throw new Exception($"Invalid packet type (without player instance) of \"{packet.packetType}\". Did you forget to implement this?");
                   }
            } else {
                switch (packet) {
                    case SendNewChatMessagePacket sendNewChatMessagePacket:
                        ChatMessage message = this.chat.addMessage(sender.player, sendNewChatMessagePacket.content);
                        await this.broadcast(new NewChatMessageSentPacket { message = message });
                        break;
                    case ChatMessagesRequestPacket getChatMessagesPacket:
                        await sender.sendMessage(new ChatMessagesSyncPacket { messages = this.chat.getMessages() });
                        break;
                    case GameLogMessagesRequestPacket getGameLogMessagesPacket:
                        await sender.sendMessage(new GameLogMessagesSyncPacket { messages = this.gameLog.getMessages() });
                        break;
                    case GameDataRequestPacket gameInfoRequestPacket:
                        await sender.sendMessage(new GameDataSyncPacket { gameData = this.gameData });
                        break;
                    // Host only packet
                    case StartGamePacket startGamePacket:
                        if (sender.player.username == this.gameData.host) {
                            if (this.gameData.gameStarted) {
                                await sender.sendMessage(new ActionFailedPacket { reason = "The game is already started. How did you even manage that?" });
                            } else if (this.connectedClients.Count >= 2 && this.gameData.getActivePlayers().Count >= 2) {
                                await this.startGame();
                            } else {
                                await sender.sendMessage(new ActionFailedPacket { reason = "There needs to be at least 2 players to start a game" });
                            }
                        }
                        break;
                    case KickPlayerPacket kickPlayerPacket:
                        if (sender.player.username == this.gameData.host) {
                            if (sender.player.username == kickPlayerPacket.username) {
                                await sender.sendMessage(new ActionFailedPacket { reason = "Why are you trying to kick yourself? Sounds painful." });
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
                            await this.postActionTransition(true);
                            // Eliminate them to handle adjusting turn order (have to do this after otherwise we skip two players)
                            this.gameData.eliminatePlayer(target);
                            // Discard the player entirely since they're likely not welcome back
                            this.gameData.players.Remove(target);

                            // Send currentTurn to avoid a game data sync (otherwise UI doesn't work properly)
                            await this.broadcast(new PlayerKickedPacket { username = target.username, currentTurn = this.gameData.currentTurn });
                            await this.sendGameLogMessage($"{target.username} has been kicked.");

                            // Actually disconnected them, which runs handleClientDisconnect
                            // This removes the client from connectedClients, and checks for game end or host transference (though host transfer should never occur on kick since the host cannot kick themselves)
                            await clientToKick.webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "playerKicked", new CancellationToken());
                        }
                        break;
                    case ShootPlayerPacket shootPlayerPacket:
                        // Make sure it's the players turn
                        if (sender.player == this.gameData.getCurrentPlayerForTurn()) {
                            // TODO turn this into a function so that nextTurn(inGame == false) can copy the logic
                            bool isTurnEndingMove = await this.shootPlayer(sender.player, this.gameData.getPlayerByUsername(shootPlayerPacket.target));

                            // If they had a quickshot item, don't end their turn no matter what
                            if (this.gameData.quickshotEnabled) {
                                isTurnEndingMove = false;
                                this.gameData.quickshotEnabled = false;
                            }

                            await this.postActionTransition(isTurnEndingMove);
                        }
                        break;
                    case UseSkipItemPacket useSkipItemPacket:
                        if (sender.player == this.gameData.getCurrentPlayerForTurn()) {
                            Player target = this.gameData.getPlayerByUsername(useSkipItemPacket.target);
                            if (target.isSkipped) {
                                await sender.sendMessage(new ActionFailedPacket { reason = $"{target.username} has already been skipped!" });
                                return;
                            }
                            if (sender.player.items.Remove(Item.SkipPlayerTurn)) {
                                await this.broadcast(new SkipItemUsedPacket { target = useSkipItemPacket.target });
                                await this.sendGameLogMessage($"{target.username} has been skipped by {sender.player.username}");
                                target.isSkipped = true;
                            } else {
                                await sender.sendMessage(new ActionFailedPacket { reason = "You don't have a Skip item!" });
                            }
                        }
                        break;
                    case UseDoubleDamageItemPacket useDoubleDamageItemPacket:
                        if (sender.player == this.gameData.getCurrentPlayerForTurn()) {
                            if (this.gameData.damageForShot != 1) {
                                await sender.sendMessage(new ActionFailedPacket { reason = "You've already used a Double Damage item for this shot!" });
                                return;
                            }
                            if (sender.player.items.Remove(Item.DoubleDamage)) {
                                await this.broadcast(new DoubleDamageItemUsedPacket());
                                await this.sendGameLogMessage($"{sender.player.username} has used a Double Damage item");
                                this.gameData.damageForShot = 2;
                            } else {
                                await sender.sendMessage(new ActionFailedPacket { reason = "You don't have a Double Damage item!" });
                            }
                        }
                        break;
                    case UseCheckBulletItemPacket useCheckBulletItemPacket:
                        if (sender.player == this.gameData.getCurrentPlayerForTurn()) {
                            if (sender.player.items.Remove(Item.CheckBullet)) {
                                AmmoType peekResult = this.gameData.peekAmmoFromChamber();
                                await sender.sendMessage(new CheckBulletItemResultPacket { result = peekResult });
                                await this.broadcast(new CheckBulletItemUsedPacket());
                                await this.sendGameLogMessage($"{sender.player.username} has used a Chamber Check item");
                            } else {
                                await sender.sendMessage(new ActionFailedPacket { reason = "You don't have a Chamber Check item!" });
                            }
                        }
                        break;
                    case UseRebalancerItemPacket useRebalancerItemPacket:
                        if (sender.player == this.gameData.getCurrentPlayerForTurn()) {
                            if (sender.player.items.Remove(Item.Rebalancer)) {
                                int count = this.gameData.addAmmoToChamberAndShuffle(useRebalancerItemPacket.ammoType);
                                await this.broadcast(new RebalancerItemUsedPacket { ammoType = useRebalancerItemPacket.ammoType, count = count });
                                await this.sendGameLogMessage($"{sender.player.username} has used a Rebalancer item and added {count} {useRebalancerItemPacket.ammoType.ToString().ToLower()} rounds");
                            } else {
                                await sender.sendMessage(new ActionFailedPacket { reason = "You don't have a Rebalancer item!" });
                            }
                        }
                        break;
                    case UseAdrenalineItemPacket useAdrenalineItemPacket:
                        if (sender.player == this.gameData.getCurrentPlayerForTurn()) {
                            if (sender.player.items.Remove(Item.Adrenaline)) {
                                // Coin flip (generates 0 or 1, and 0 is turned to -1)
                                int result = new Random().Next(2) * 2 - 1;
                                await this.broadcast(new AdrenalineItemUsedPacket { result  = result });
                                await this.sendGameLogMessage($"{sender.player.username} has used an Adrenaline item and {(result > 0 ? "gained" : "lost")} a life");
                                sender.player.lives += result;
                                await this.checkAndEliminatePlayer(sender.player);
                                // This is only here to handle game end (no shot was taken so a round won't be started, and it won't move to the next turn)
                                await this.postActionTransition(false);
                            } else {
                                await sender.sendMessage(new ActionFailedPacket { reason = "You don't have an Adrenaline item!" });
                            }
                        }
                        break;
                    case UseAddLifeItemPacket useAddLifeItemPacket:
                        if (sender.player == this.gameData.getCurrentPlayerForTurn()) {
                            if (sender.player.items.Remove(Item.AddLife)) {
                                await this.broadcast(new AddLifeItemUsedPacket());
                                await this.sendGameLogMessage($"{sender.player.username} has used a +1 Life item");
                                sender.player.lives++;
                            } else {
                                await sender.sendMessage(new ActionFailedPacket { reason = "You don't have a +1 Life item!" });
                            }
                        }
                        break;
                    case UseQuickshotItemPacket useQuickshotItemPacket:
                        if (sender.player == this.gameData.getCurrentPlayerForTurn()) {
                            if (this.gameData.quickshotEnabled) {
                                await sender.sendMessage(new ActionFailedPacket { reason = "You've already used a Quickshot item for this turn!" });
                                return;
                            }
                            await this.broadcast(new QuickshotItemUsedPacket());
                            await this.sendGameLogMessage($"{sender.player.username} has used a Quickshot item");
                            this.gameData.quickshotEnabled = true;
                        } else {
                            await sender.sendMessage(new ActionFailedPacket { reason = "You don't have a Quickshot item!" });
                        }
                        break;
                    case UseStealItemPacket useStealItemPacket:
                        if (sender.player == this.gameData.getCurrentPlayerForTurn()) {
                            if (sender.player.items.Remove(Item.StealItem)) {
                                await this.broadcast(new StealItemUsedPacket { target = useStealItemPacket.target, item = useStealItemPacket.item });
                                // TODO
                                // await this.sendGameLogMessage($"{sender.player.username} has used a Pickpocket item and stolen {}");
                            } else {
                                await sender.sendMessage(new ActionFailedPacket { reason = "You don't have a Pickpocket item!" });
                            }
                        }
                        break;
                    default:
                        throw new Exception($"Invalid packet type (with player instance) of \"{packet.packetType}\". Did you forget to implement this?");
                }
            }
        }

        private async Task<bool> shootPlayer(Player shooter, Player target) {
            AmmoType shot = this.gameData.pullAmmoFromChamber();
            await this.broadcast(new PlayerShotAtPacket { target = target.username, ammoType = shot, damage = this.gameData.damageForShot });

            if (shooter != target) {
                await this.sendGameLogMessage($"{shooter.username} shot {target.username} with a {shot.ToString().ToLower()} round.");
            } else {
                await this.sendGameLogMessage($"{shooter.username} shot themselves with a {shot.ToString().ToLower()} round.");
            }
            
            bool isTurnEndingMove = true;
            // If it's a live round, regardless of who, the turn is over
            if (shot == AmmoType.Live) {
                target.lives -= this.gameData.damageForShot;
                await this.checkAndEliminatePlayer(target);
            } else if (target == shooter) { // Implied it was a blank round
                isTurnEndingMove = false;
            }

            this.gameData.damageForShot = 1;

            // In case this triggers a round end, we pass along whether or not it was a turn ending shot, so that when a new round starts, it's still that players turn
            return isTurnEndingMove;
        }

        async public Task checkAndEliminatePlayer(Player player) {
            if (player.lives <= 0) {
                this.gameData.eliminatePlayer(player);
                await this.sendGameLogMessage($"{player.username} has been eliminated.");
            }
        }

        public async Task postActionTransition(bool isTurnEndingMove) {
            // Check for game end (if there's one player left standing)
            // Make sure the game is still going in case this triggers twice
            // TODO game over packet
            if (this.gameData.players.Count(player => player.isSpectator == false) <= 1 && this.gameData.gameStarted) {
                await this.endGame();
                return;
            }

            // Check for round end
            if (this.gameData.getAmmoInChamberCount() <= 0) {
                await this.startNewRound();
            }

            if (isTurnEndingMove) {
                await this.nextTurn();
            }
        }

        // Player shoots themselves once and does not get to go again if it was blank
        private async Task forfeitTurn(Player player) {
            await this.shootPlayer(player, player);
            await this.postActionTransition(true);
        }

        public async Task startGame() {
            // Gives items, trigger refresh
            this.gameData.startGame();
            await this.broadcast(new GameStartedPacket());

            this.gameLog.clear();
            await this.broadcast(new GameLogMessagesSyncPacket { messages = this.gameLog.getMessages() });

            await this.startNewRound();
            await this.nextTurn();
        }

        public async Task nextTurn() {
            // Send the turn end packet for the previous player (if there was one) automaticaly
            if (this.gameData.currentTurn != null) {
                await this.broadcast(new TurnEndedPacket { username = this.gameData.currentTurn });
            }

            Player playerForTurn = this.gameData.startNewTurn();
            await this.broadcast(new TurnStartedPacket { username = playerForTurn.username });

            if (playerForTurn.isSkipped) {
                await this.sendGameLogMessage($"{playerForTurn.username} has been skipped.");
                playerForTurn.isSkipped = false;
                await this.nextTurn();
            }

            // If the player left the game, have them shoot themselves and move on
            if (!playerForTurn.inGame) {
                await this.forfeitTurn(playerForTurn);
            }
        }

        public async Task startNewRound() {
            List<int> ammoCounts = this.gameData.newRound();

            await this.sendGameLogMessage($"A new round has started. The chamber has been loaded with {ammoCounts[0]} live round{(ammoCounts[0] != 1 ? "s" : "")} and {ammoCounts[1]} blank{(ammoCounts[1] != 1 ? "s" : "")}.");

            // Auto-apply +1 Life items
            foreach (Player player in this.gameData.players) {
                int extraLives = player.items.RemoveAll(item => item == Item.AddLife);
                player.lives += extraLives;
                if (extraLives > 0) {
                    await this.sendGameLogMessage($"{player.username} has been given {extraLives} {(extraLives > 1 ? "lives" : "life")} from items");
                }
            }

            await this.broadcast(new NewRoundStartedPacket { 
                players = this.gameData.players, 
                liveCount = ammoCounts[0],
                blankCount = ammoCounts[1]
            });
        }

        public async Task endGame() {
            // There's almost certainly at least one player left when this runs (used to just wipe if there was nobody left, but that situation requires 2 people to DC at once which I don't care enough to account for)
            Player? possibleWinner = this.gameData.players.Find(player => player.lives >= 1);
            string winner = possibleWinner != null ? possibleWinner.username : "nobody";

            // Copy any data we may need (like players)
            GameData newGameData = new GameData {
                players = this.gameData.players.Where(player => player.inGame).Select(player => {
                    player.isSpectator = false;
                    player.isSkipped = false;
                    player.items.Clear();
                    player.lives = Player.DEFAULT_LIVES;
                    return player;
                }).ToList(),
                host = this.gameData.host
            };
            this.gameData = newGameData;
            await this.syncGameData();

            await this.sendGameLogMessage($"The game has ended. The winner is {winner}.");
        }

        public Client? getClientForCurrentTurn() {
            Player currentPlayer = this.gameData.getCurrentPlayerForTurn();
            return this.connectedClients.Find(client => client.player == currentPlayer);
        }

        public async Task syncGameData() {
            await this.broadcast(new GameDataSyncPacket { gameData = this.gameData });
        }

        public async Task sendGameLogMessage(string content) {
            GameLogMessage message = new GameLogMessage(content);
            this.gameLog.addMessage(message);
            await this.broadcast(new NewGameLogMessageSentPacket {
                message = message
            });
        }

        public async Task broadcast(ServerPacket packet) {
            await Console.Out.WriteLineAsync($"Broadcasting {JsonConvert.SerializeObject(packet)}");
            foreach (Client client in this.connectedClients) {
                if (client.player?.inGame ?? false) {
                    await client.sendMessage(packet, false);
                }
            }
        }
    }
}
