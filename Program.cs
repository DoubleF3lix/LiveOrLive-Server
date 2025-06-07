namespace liveorlive_server {
    public class Program {
        public static async Task Main() {
            var app = new App();
            await app.Start("localhost", 8080);
        }
    }
}
