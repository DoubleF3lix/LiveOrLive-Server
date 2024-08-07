using System.Net;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;


namespace liveorlive_server {
    public class Server {
        WebApplication app;

        List<Client> connectedClients = new List<Client>();
        GameData gameData = new GameData();
        public Chat chat = new Chat();

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
                        await Console.Out.WriteLineAsync($"Username taken for \"{joinGamePacket.username}\": {usernameTaken}");
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
                    case GameDataRequestPacket gameInfoRequestPacket:
                        await this.syncGameData();
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
                            Player target = this.gameData.getPlayerByUsername(shootPlayerPacket.target);
                            AmmoType shot = this.gameData.pullAmmoFromChamber();
                            await broadcast(new PlayerShotAtPacket { target = shootPlayerPacket.target, ammoType = shot });
                            if (shot == AmmoType.Live) {
                                // TODO check for double damage
                                target.lives--;
                                if (target.lives <= 0) {
                                    this.gameData.eliminatePlayer(target);
                                    // TODO send elimination packet maybe?
                                }
                                await this.nextTurn();
                            } else if (target != sender.player) {
                                await this.nextTurn();
                            }
                            // Only case not covered is if it was blank and the target was the player, in which case, they get to go again
                        }
                        break;
                    default:
                        throw new Exception("Invalid packet type (with player instance) of \"{packet.packetType}\". Did you forget to implement this?");
                }
            }
        }

        public async Task startGame() {
            // Gives items, trigger refresh
            this.gameData.startGame();
            await broadcast(new GameStartedPacket());

            await this.startNewRound();
        }

        public async Task startNewRound() {
            if (await this.checkForGameEnd()) {
                return;
            }

            List<int> ammoCounts = this.gameData.newRound();
            await broadcast(new NewRoundStartedPacket { 
                players = this.gameData.players, 
                liveCount = ammoCounts[0],
                blankCount = ammoCounts[1] 
            });

            await this.nextTurn();
        }

        public async Task nextTurn() {
            // TODO game over packet, add items
            // TODO make gameLog populate on server side, not client
            if (await this.checkForGameEnd()) {
                return;
            }

            // Send the turn end packet for the previous player (if there was one) automaticaly
            if (this.gameData.currentTurn != null) { 
                await broadcast(new TurnEndedPacket { username = this.gameData.currentTurn }); 
            }

            // TODO start on the right player when triggering this (preserve turn order)
            if (this.gameData.getAmmoInChamberCount() <= 0) {
                await this.startNewRound();
            }

            Player playerForTurn = this.gameData.startNewTurn();
            await broadcast(new TurnStartedPacket { username = playerForTurn.username });

            if (playerForTurn.inGame == false) {
                await broadcast(new PlayerShotAtPacket { target = playerForTurn.username, ammoType = this.gameData.pullAmmoFromChamber() });
                await this.nextTurn();
            }
        }

        public async Task<bool> checkForGameEnd() {
            // Check if there's one player left standing or if all but one player has left
            await Console.Out.WriteLineAsync(this.gameData.players.Count(player => player.isSpectator == false).ToString());
            if (this.gameData.players.Count(player => player.isSpectator == false) <= 1 || this.connectedClients.Count <= 1) {
                await this.endGame();
                return true;
            }
            return false;
        }

        public async Task endGame() {
            // Broadcast message only if there is anyone to broadcast to (AKA it didn't end by all players DC)
            if (this.connectedClients.Count >= 1) {
                await this.sendGameLogMessage($"The game has ended. The winner is {this.gameData.players.Find(player => player.lives >= 1).username}");

                // Copy any data we may need (like players)
                GameData newGameData = new GameData {
                    players = this.gameData.players.Where(player => player.inGame).Select(player => {
                        player.isSpectator = false;
                        player.items.Clear();
                        player.lives = Player.DEFAULT_LIVES;
                        return player;
                    }).ToList(),
                    host = this.gameData.host,
                    gameLog = new(this.gameData.gameLog)
                };
                this.gameData = newGameData;
                await this.syncGameData();
            } else {
                // Otherwise, we can just nuke existing data and start fresh
                this.gameData = new GameData();
            }
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
            this.gameData.gameLog.Add(message);
            await this.broadcast(new NewGameLogMessageSentPacket {
                message = message
            });
        }

        public async Task broadcast(ServerPacket packet) {
            foreach (Client client in this.connectedClients) {
                if (client.player?.inGame ?? false) {
                    await client.sendMessage(packet);
                }
            }
        }
    }
}
