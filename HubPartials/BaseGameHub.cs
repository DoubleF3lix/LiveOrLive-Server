using liveorlive_server.Enums;
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
            await Clients.Group(lobby.Id).GameLogUpdate(
                new GameLogMessage($"The game has started with {lobby.Players.Count(p => !p.IsSpectator)} players.")
            );

            // No skipped players on game start (hopefully, it's a safe assumption anyway)
            NewTurn(lobby);
            // await Clients.Group(lobby.Id).TurnStarted(lobby.PlayerForCurrentTurn.Username);
        }

        public async Task GetLobbyDataRequest() {
            var lobby = Context.GetLobby(this._server);
            await Clients.Caller.GetLobbyDataResponse(lobby);
        }

        public async Task ShootPlayer(string target) {
            var lobby = Context.GetLobby(this._server);
            var shooter = Context.GetPlayer(this._server);

            if (lobby.CurrentTurn != shooter.Username) {
                await Clients.Caller.ActionFailed("It must be your turn to shoot another player!");
                return;
            }

            if (lobby.TryGetPlayerByUsername(target, out var targetPlayer)) {
                var bulletFired = lobby.FireGun();
                var damage = (int)bulletFired;
                targetPlayer.Lives -= damage;
                await Clients.Group(lobby.Id).PlayerShotAt(target, bulletFired, damage);
                if (bulletFired == BulletType.Blank) {
                    await Clients.Group(lobby.Id).GameLogUpdate(
                        new GameLogMessage($"{lobby.CurrentTurn} shot {target} with a blank round.")
                    );
                } else {
                    await Clients.Group(lobby.Id).GameLogUpdate(
                        new GameLogMessage($"{lobby.CurrentTurn} shot {target} with a live round for {damage} damage.")
                    );
                }

                this.NewTurn(lobby);
            } else {
                await Clients.Caller.ActionFailed($"{target} isn't a valid player!");
                return;
            }
        }

        private void NewTurn(Lobby lobby) {
            lobby.NewTurn(turnStartPlayer => {
                Clients.Group(lobby.Id).TurnStarted(turnStartPlayer);
                AddGameLogMessage(lobby, $"It's {turnStartPlayer}'s turn.");
            }, (turnEndPlayer, endOnSkip) => {
                Clients.Group(lobby.Id).TurnEnded(turnEndPlayer);
                if (endOnSkip) {
                    AddGameLogMessage(lobby, $"{turnEndPlayer} was skipped.");
                }
                AddGameLogMessage(lobby, $"{turnEndPlayer}'s turn has ended.");
            });
        }

        private void AddGameLogMessage(Lobby lobby, string message) {
            var gameLogMessage = lobby.AddGameLogMessage(message);
            Clients.Group(lobby.Id).GameLogUpdate(gameLogMessage);
        }
    }
}
