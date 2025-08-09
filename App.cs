using LiveOrLiveServer.HubPartials;

namespace LiveOrLiveServer {
    public class App {
        private readonly WebApplication app;

        public App() {
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

            builder.Services.AddControllers();
            builder.Services.AddSingleton<Server>();

            app = builder.Build();
            app.UseRouting();
            app.UseCors("_allowClientOrigins");
            app.MapControllers();

            app.MapHub<LiveOrLiveHub>("");
        }

        public async Task Start(string url, int port) {
            await app.RunAsync($"http://{url}:{port}");
        }
    }
}
