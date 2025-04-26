using liveorlive_server.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace liveorlive_server.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IBaseGameRequest {
        public async Task StartGame() {
            var lobby = Context.GetLobby();
            if (lobby.Host != Context.GetPlayer().Username) {
                await Clients.Caller.ActionFailed("You must be the host to do that!");
                return;
            }
            if (lobby.Players.Count(player => !player.IsSpectator && player.InGame) < 2) {
                await Clients.Caller.ActionFailed("There must be at least 2 non-spectators to start a game");
                return;
            }
            lobby.StartGame();
            await Clients.Group(Context.GetLobbyId()).GameStarted();
        }

        public async Task GetLobbyDataRequest() {
            var lobby = Context.GetLobby();
            await Clients.Caller.GetLobbyDataResponse(lobby);
        }

        public async Task ShootPlayer(string target) {
            
        }
    }
}
