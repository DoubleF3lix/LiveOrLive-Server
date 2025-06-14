namespace liveorlive_server {
    public class Program {
        public static async Task Main() {
            var app = new App();
            await app.Start("0.0.0.0", 8080);
        }
    }
}
