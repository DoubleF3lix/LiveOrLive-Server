using liveorlive_server.Enums;
using liveorlive_server.Extensions;
using liveorlive_server.Models;
using Microsoft.AspNetCore.SignalR;

namespace liveorlive_server.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IBaseGameRequest {
        public async Task StartGame() {
            var lobby = Context.GetLobby(_server);
            if (lobby.Host != Context.GetPlayer(_server).Username) {
                await Clients.Caller.ActionFailed("You must be the host to do that!");
                return;
            }
            if (lobby.Players.Count(player => !player.IsSpectator && player.InGame) < 2) {
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

            if (lobby.TryGetPlayerByUsername(target, out var targetPlayer)) {
                var result = lobby.ShootPlayer(shooterPlayer, targetPlayer);

                // Be verbose about who shot who (even if it's themselves)
                await Clients.Group(lobby.Id).PlayerShotAt(target, result.BulletFired, result.Damage);

                string message;
                if (result.BulletFired == BulletType.Blank) {
                    message = $"{shooterPlayer.Username} shot {(result.ShotSelf ? "themselves" : target)} with a blank round.";
                } else {
                    message = $"{shooterPlayer.Username} shot {target} with a live round for {result.Damage} damage.";
                }

                await Clients.Group(lobby.Id).GameLogUpdate(new GameLogMessage(message));
                // It's a turn ending action if it was not a blank round fired at themselves
                await OnActionEnd(lobby, !result.ShotSelf || result.BulletFired != BulletType.Blank);

            } else {
                await Clients.Caller.ActionFailed($"{target} isn't a valid player!");
                return;
            }
        }
    }
}
