using liveorlive_server.Models.Results;
using Microsoft.AspNetCore.SignalR;

namespace liveorlive_server.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse> {
        private async Task StartGame(Lobby lobby) {
            lobby.StartGame();
            await Clients.Group(lobby.Id).GameStarted();
            await AddGameLogMessage(lobby, $"The game has started with {lobby.Players.Count(p => !p.IsSpectator)} players.");
        }

        private async Task EndGame(Lobby lobby) {
            var result = lobby.EndGame();
            await Clients.Group(lobby.Id).GameEnded(result.Winner);
            await AddGameLogMessage(lobby, $"The game has ended. The winner is {result.Winner}!");
        }

        private async Task NewTurn(Lobby lobby) {
            foreach (var resultLine in lobby.NewTurn()) {
                switch (resultLine) {
                    case StartTurnResult startTurnResult:
                        await Clients.Group(lobby.Id).TurnStarted(startTurnResult.PlayerUsername);
                        await AddGameLogMessage(lobby, $"It's {startTurnResult.PlayerUsername}'s turn.");
                        break;
                    case EndTurnResult endTurnResult:
                        await Clients.Group(lobby.Id).TurnEnded(endTurnResult.PlayerUsername);
                        if (endTurnResult.EndDueToSkip) {
                            await AddGameLogMessage(lobby, $"{endTurnResult.PlayerUsername} was skipped.");
                        }
                        await AddGameLogMessage(lobby, $"{endTurnResult.PlayerUsername}'s turn has ended.");
                        break;
                }
            }
        }

        private async Task NewRound(Lobby lobby) {
            var result = lobby.NewRound();
            await Clients.Group(lobby.Id).NewRoundStarted(result.BlankRounds, result.LiveRounds);
            await AddGameLogMessage(lobby, $"A new round has started with {result.LiveRounds} live rounds and {result.BlankRounds} blanks.");
        }

        private async Task OnActionEnd(Lobby lobby, bool isTurnEndingMove) {
            // Check for game end (if there's one player left standing)
            // Make sure the game is still going in case this triggers twice
            if (lobby.Players.Count(player => !player.IsSpectator && player.Lives > 0) <= 1) {
                await EndGame(lobby);
                return;
            }

            if (isTurnEndingMove) {
                await NewTurn(lobby);
            }

            // Check for round end
            if (lobby.AmmoLeftInChamber <= 0) {
                await NewRound(lobby);
            }
        }

        private async Task AddGameLogMessage(Lobby lobby, string message) {
            var gameLogMessage = lobby.AddGameLogMessage(message);
            await Clients.Group(lobby.Id).GameLogUpdate(gameLogMessage);
        }
    }
}
