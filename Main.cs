namespace liveorlive_server {
    public class Program {
        public static async Task Main() {
            Server server = new();
            await server.Start("0.0.0.0", 8080);
        }
    }
}
