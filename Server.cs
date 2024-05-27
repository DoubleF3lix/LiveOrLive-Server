using System.Net;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;


namespace backend_server {
    public class Server {
        WebApplication app;

        List<Client> connectedClients = new List<Client>();
        GameData gameData = new GameData();

        public Server() {
            this.app = WebApplication.CreateBuilder().Build();
            this.app.UseWebSockets();
            
            // app.MapGet("/", () => new Microsoft.AspNetCore.Mvc.JsonResult("works"));

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
                client.onDisconnect();
            }
        }

        public async Task packetReceived(Client sender, ClientPacket packet) {
            await Console.Out.WriteLineAsync($"Received packet from {sender.ToString()}: {packet}");
            if (sender.player == null) {
                switch (packet) {
                    case JoinGamePacket joinGamePacket:
                        // Check if the username was available
                        // TODO this broke (can log in as same username twice)
                        bool usernameTaken = this.gameData.players.Any(player => player.username == joinGamePacket.username);
                        await Console.Out.WriteLineAsync($"Username taken for \"{joinGamePacket.username}\": {usernameTaken}");
                        if (!usernameTaken) {
                            sender.player = new Player(joinGamePacket.username, true);
                            this.gameData.players.Add(sender.player);
                        // If it's not, check if the username is still logged in. If so, error, if not, assume it's the player logging back in
                        } else {
                            Player takenPlayer = this.gameData.players.First(player => player.username == joinGamePacket.username);
                            if (takenPlayer.inGame) {
                                await sender.sendMessage(new PlayerJoinRejectedPacket { reason = "That username is already taken. Please choose another." });
                                return;
                            } else {
                                sender.player = takenPlayer;
                                // TODO player leave packet
                            }
                        }

                        // Failure triggers a return which skips over this code
                        await broadcast(new PlayerJoinedPacket { player = sender.player });
                        // If they're the first player, mark them as the host
                        int joinedPlayersCount = this.gameData.players.Count(player => player.inGame);
                        if (joinedPlayersCount == 0) {
                            await sender.sendMessage(new SetHostPacket());
                            this.gameData.gameHost = sender.player;
                        }

                        break;
                    default:
                        throw new Exception($"Invalid packet type (without player instance) of \"{packet.packetType}\". Did you forget to implement this?");
                   }
            } else {
                switch (packet) {
                    case SendNewChatMessagePacket sendNewChatMessagePacket:
                        ChatMessage message = this.gameData.chat.addMessage(sender.player, sendNewChatMessagePacket.content);
                        await broadcast(new NewChatMessageSentPacket { message = message });
                        break;
                    // TODO sometimes doesn't contain chat messages???
                    case GetGameInfoPacket getGameInfoPacket:
                        await sender.sendMessage(new GetGameInfoResponsePacket { currentHost = this.gameData.gameHost, players = this.gameData.players, chatMessages = this.gameData.chat.getMessages(), turnCount = this.gameData.turnCount });
                        break;
                        /*
                    case FireGunPacket fireGunPacket:
                        // Check the player can make a move
                        if (fromPlayer != this.players[this.turnCount % this.players.Count() - 1]) {

                        }
                        break;
                    case UseItemPacket useItemPacket:
                        if (fromPlayer != this.players[this.turnCount % this.players.Count() - 1]) {

                        }
                        break;
                    case StartGamePacket startGamePacket:
                        // Host only packet
                        if (fromPlayer == this.gameHost) {
                            // TODO
                        }
                        break;
                        */
                    default:
                        throw new Exception("Invalid packet type (with player instance) of \"{packet.packetType}\". Did you forget to implement this?");
                }
            }
        }

        public async Task broadcast(ServerPacket packet) {
            foreach (Client client in this.connectedClients) {
                await client.sendMessage(packet);
            }
        }
    }
}
