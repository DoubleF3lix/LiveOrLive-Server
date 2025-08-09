using LiveOrLiveServer.Enums;
using LiveOrLiveServer.Extensions;
using LiveOrLiveServer.Models;
using Microsoft.AspNetCore.SignalR;

namespace LiveOrLiveServer.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IBaseGameRequest {
        public async Task StartGame() {
            var lobby = Context.GetLobby(_server);
            if (lobby.Host != Context.GetPlayer(_server).Username) {
                await Clients.Caller.ActionFailed("You must be the host to do that!");
                return;
            }
            if (lobby.Players.Count(player => player.InGame) < 2) {
                await Clients.Caller.ActionFailed("There must be at least 2 non-spectators to start a game");
                return;
            }

            await StartGame(lobby);
            await NewRound(lobby);
            await NewTurn(lobby);
        }

        public async Task GetLobbyDataRequest() {
            var lobby = Context.GetLobby(_server);
            await Clients.Caller.GetLobbyDataResponse(lobby);
        }

        public async Task ShootPlayer(string target) {
            var lobby = Context.GetLobby(_server);
            var shooterPlayer = Context.GetPlayer(_server);

            if (lobby.CurrentTurn != shooterPlayer.Username) {
                await Clients.Caller.ActionFailed("It must be your turn to shoot another player!");
                return;
            }

            if (!lobby.TryGetPlayerByUsername(target, out var targetPlayer)) {
                await Clients.Caller.ActionFailed($"{target} isn't a valid player!");
                return;
            }

            if (targetPlayer.Lives == 0) {
                await Clients.Caller.ActionFailed("You can't shoot a dead player!");
                return;
            }

            await ShootPlayerActual(lobby, shooterPlayer, targetPlayer);
        }
    }
}
