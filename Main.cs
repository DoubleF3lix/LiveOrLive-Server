namespace liveorlive_server {
    public class Program {
        public static async Task Main(string[] args) {
            Server server = new Server();
            await server.start("0.0.0.0", 8080);
        }
    }
}
