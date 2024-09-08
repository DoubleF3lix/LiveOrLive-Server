using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

namespace liveorlive_server {
    public class Client {
        public readonly WebSocket webSocket;
        public readonly Server server;

        public readonly string ID;
        public Player? player;

        public Client(WebSocket webSocket, Server server, string ID) {
            this.webSocket = webSocket;
            this.server = server;
            this.ID = ID;
            this.onConnect();
        }

        // Sends a message to the corresponding client that this instance represents
        public async Task sendMessage(ServerPacket packet, bool printLog = true) {
            if (printLog) {
                await Console.Out.WriteLineAsync($"Sending packet to {this.ToString()}: {JsonConvert.SerializeObject(packet)}");
            }
            string dataStr = JsonConvert.SerializeObject(packet, new PacketJSONConverter());
            var bytes = Encoding.UTF8.GetBytes(dataStr);
            await this.webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public void onConnect() {
            Console.WriteLine($"Connection opened with {this.ID}");
        }

        public void onDisconnect() {
            Console.WriteLine($"Connection closed with {this.ToString()}");
        }

        public override string ToString() {
            return $"Client {{ ID = \"{this.ID}\" }}";
        }
    }
}
