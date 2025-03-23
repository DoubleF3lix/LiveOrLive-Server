using Microsoft.AspNetCore.SignalR;

namespace liveorlive_server.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IConnectionRequest {
        public async Task JoinGameRequest(string username) {
            // TODO rethink this (have username and lobbyID come in via query args, validate on connection)
            // Have endpoint to check if lobby ID is valid
            // Then if it is, client should try connecting via query args and signalR client
            // That way, client is guarunteed to be in game with a Player object
        }

        public async Task KickPlayer(string username) {
            await Console.Out.WriteLineAsync($"KickPlayer: {username}");
        }

        public async Task SetHost(string username) {
            
        }
    }
}
