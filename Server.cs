using System.Net;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;


namespace liveorlive_server {
    public class Server {
        WebApplication app;

        List<Client> connectedClients = new List<Client>();
        GameData gameData = new GameData();
        public GameLog gameLog = new GameLog();
        public Chat chat = new Chat();
        // TODO store GameLog in its own class as well (can still be stored in redux I guess)

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
            foreach (Client c in this.connectedClients) {
                if (c.webSocket.State == WebSocketState.Closed) {
                    this.connectedClients.Remove(c);
                }
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
                        ClientPacket packet = JsonConvert.DeserializeObject<ClientPacket>(message, new PacketJSONConverter());
                        await this.packetReceived(client, packet);
                    } else if (result.MessageType == WebSocketMessageType.Close || webSocket.State == WebSocketState.Aborted) {
                        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                        break;
                    }
                }
            } catch (WebSocketException exception) {
                // Abormal disconnection, finally block has us covered
            } finally {
                this.connectedClients.Remove(client);

                // If they're the host, try to pass that status on to someone else
                // If they don't have a player assigned, don't bother
                if (client.player?.username == this.gameData.host) {
                    // TODO null error here (client is made null while iterating over it?)
                    if (this.connectedClients.Count(client => client.player != null) > 0) {
                        Player newHost = this.connectedClients[0].player;
                        this.gameData.host = newHost.username;
                        await broadcast(new HostSetPacket { username = this.gameData.host }); // TODO set host function
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
                client.onDisconnect();
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

                        await broadcast(new PlayerJoinedPacket { player = sender.player });
                        // If they're the first player, mark them as the host
                        if (this.gameData.getActivePlayers().Count == 1) {
                            // TODO make SetHostPacket send a chat message
                            await broadcast(new HostSetPacket { username = sender.player.username });
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
                        await broadcast(new NewChatMessageSentPacket { message = message });
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
                    case ShootPlayerPacket shootPlayerPacket:
                        // Make sure it's the players turn
                        if (sender.player == this.gameData.getCurrentPlayerForTurn()) {
                            // TODO turn this into a function so that nextTurn(inGame == false) can copy the logic
                            bool isTurnEndingMove = await this.shootPlayer(sender.player, this.gameData.getPlayerByUsername(shootPlayerPacket.target));

                            await this.postActionTransition(isTurnEndingMove);
                        }
                        break;
                    default:
                        throw new Exception("Invalid packet type (with player instance) of \"{packet.packetType}\". Did you forget to implement this?");
                }
            }
        }

        // autoShiftTurn is set to false on turn forfeit since it always advances
        // TODO shooting breaks if someone leaves??
        // TODO act when player leaves during their turn
        private async Task<bool> shootPlayer(Player shooter, Player target) {
            AmmoType shot = this.gameData.pullAmmoFromChamber();
            await broadcast(new PlayerShotAtPacket { target = target.username, ammoType = shot, damage = this.gameData.damageForShot });
            this.gameData.damageForShot = 1;

            if (shooter != target) {
                await this.sendGameLogMessage($"{shooter.username} shot {target.username} with a {shot.ToString().ToLower()} round.");
            } else {
                await this.sendGameLogMessage($"{shooter.username} shot themselves with a {shot.ToString().ToLower()} round.");
            }
            
            bool isTurnEndingMove = true;
            // If it's a live round, regardless of who, the turn is over
            if (shot == AmmoType.Live) {
                target.lives -= this.gameData.damageForShot;
                if (target.lives <= 0) {
                    this.gameData.eliminatePlayer(target);
                    await this.sendGameLogMessage($"{target.username} has been eliminated.");
                }
            } else if (target == shooter) { // Implied it was a blank round
                isTurnEndingMove = false;
            }
    
            // In case this triggers a round end, we pass along whether or not it was a turn ending shot, so that when a new round starts, it's still that players turn
            return isTurnEndingMove;
        }

        public async Task postActionTransition(bool isTurnEndingMove) {
            // Check for game end (if there's one player left standing)
            // Make sure the game is still going in case this triggers twice
            // TODO maybe rethink new turn trigger new round and having double execution stuff
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
            await broadcast(new GameStartedPacket());

            await this.startNewRound();
            await this.nextTurn();
        }

        public async Task nextTurn() {
            // Send the turn end packet for the previous player (if there was one) automaticaly
            if (this.gameData.currentTurn != null) {
                await broadcast(new TurnEndedPacket { username = this.gameData.currentTurn });
            }

            Player playerForTurn = this.gameData.startNewTurn();
            await broadcast(new TurnStartedPacket { username = playerForTurn.username });

            // If the player left the game, have them shoot themselves and move on
            if (!playerForTurn.inGame) {
                await this.forfeitTurn(playerForTurn);
            }
        }

        public async Task startNewRound() {
            List<int> ammoCounts = this.gameData.newRound();
            await broadcast(new NewRoundStartedPacket { 
                players = this.gameData.players, 
                liveCount = ammoCounts[0],
                blankCount = ammoCounts[1]
            });
            await this.sendGameLogMessage($"A new round has started. The chamber has been loaded with {ammoCounts[0]} live round{(ammoCounts[0] != 1 ? "s" : "")} and {ammoCounts[1]} blank{(ammoCounts[1] != 1 ? "s" : "")}.");
        }

        public async Task endGame() {
            // There's almost certainly at least one player left when this runs (used to just wipe if there was nobody left, but that situation requires 2 people to DC at once which I don't care enough to account for)
            string winner = this.gameData.players.Find(player => player.lives >= 1).username;

            // Copy any data we may need (like players)
            GameData newGameData = new GameData {
                players = this.gameData.players.Where(player => player.inGame).Select(player => {
                    player.isSpectator = false;
                    player.items.Clear();
                    player.lives = Player.DEFAULT_LIVES;
                    return player;
                }).ToList(),
                host = this.gameData.host
            };
            this.gameData = newGameData;
            await this.syncGameData();

            await Console.Out.WriteLineAsync(JsonConvert.SerializeObject(this.gameData.players));

            this.gameLog.clear();
            await this.broadcast(new GameLogMessagesSyncPacket { messages = this.gameLog.getMessages() });
            await this.sendGameLogMessage($"The game has ended. The winner is {winner}.");
        }

        public Client? getClientForCurrentTurn() {
            Player currentPlayer = this.gameData.getCurrentPlayerForTurn();
            Client currentClient = this.connectedClients.Find(client => client.player == currentPlayer);
            // TODO make player shoot themselves if client is null (they disconnected)
            // make sure to check player is not spectator
            return currentClient;
        }

        public async Task syncGameData() {
            await broadcast(new GameDataSyncPacket { gameData = this.gameData });
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
