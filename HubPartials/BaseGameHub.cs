using liveorlive_server.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace liveorlive_server.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IBaseGameRequest {
        public async Task StartGame() {
            var lobby = Context.GetLobby(this._server);
            if (lobby.Host != Context.GetPlayer(this._server).Username) {
                await Clients.Caller.ActionFailed("You must be the host to do that!");
                return;
            }
            if (lobby.Players.Count(player => !player.IsSpectator && player.InGame) < 2) {
                await Clients.Caller.ActionFailed("There must be at least 2 non-spectators to start a game");
                return;
            }

            lobby.StartGame();
            await Clients.Group(lobby.Id).GameStarted();

            var (blankCounts, liveCounts) = lobby.NewRound();
            await Clients.Group(lobby.Id).NewRoundStarted(blankCounts, liveCounts);

            // No skipped players on game start (hopefully, it's a safe assumption anyway)
            lobby.NewTurn();
            await Clients.Group(lobby.Id).TurnStarted(lobby.PlayerForCurrentTurn.Username);
        }

        public async Task GetLobbyDataRequest() {
            var lobby = Context.GetLobby(this._server);
            await Clients.Caller.GetLobbyDataResponse(lobby);
        }

        public async Task ShootPlayer(string target) {
            
        }
    }
}
