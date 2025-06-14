using liveorlive_server.Models.Results;
using Microsoft.AspNetCore.SignalR;

namespace liveorlive_server.HubPartials {
    // Wraps methods of a lobby to also handle outgoing packets to clients and game log messages.
    // The goal is to keep the hub logic and the game logic isolated, with this class providing methods that interface between the two.
    public partial class LiveOrLiveHub : Hub<IHubServerResponse> {
        /// <summary>
        /// Starts the game on the specified lobby.
        /// </summary>
        /// <param name="lobby">The lobby to start the game of.</param>
        private async Task StartGame(Lobby lobby) {
            lobby.StartGame();
            await Clients.Group(lobby.Id).GameStarted();
            await AddGameLogMessage(lobby, $"The game has started with {lobby.Players.Count(p => !p.IsSpectator)} players.");
        }

        /// <summary>
        /// Ends the game on the specified lobby.
        /// </summary>
        /// <param name="lobby">The lobby to end the game of.</param>
        private async Task EndGame(Lobby lobby) {
            var result = lobby.EndGame();
            await Clients.Group(lobby.Id).GameEnded(result.Winner);
            await AddGameLogMessage(lobby, result.Winner != null ? 
                $"The game has ended. The winner is {result.Winner}!" : 
                "The game has ended. No winner would be determined."
            );
        }

        /// <summary>
        /// Moves to the next turn on the specified lobby.
        /// </summary>
        /// <param name="lobby">The lobby to move to the next turn on.</param>
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

        /// <summary>
        /// Starts a new round on the specified lobby.
        /// </summary>
        /// <param name="lobby">The lobby to start a new round on.</param>
        private async Task NewRound(Lobby lobby) {
            var result = lobby.NewRound();
            await Clients.Group(lobby.Id).NewRoundStarted(result.BlankRounds, result.LiveRounds);
            await AddGameLogMessage(lobby, $"A new round has started with {result.LiveRounds} live rounds and {result.BlankRounds} blanks.");
        }

        /// <summary>
        /// Called at the end of every action (shot taken, item used, etc.) to check for player eliminations or whether or not we need to switch rounds or end the game.
        /// </summary>
        /// <param name="lobby">The lobby to perform the end of action checks on.</param>
        /// <param name="isTurnEndingMove">Whether or not the action should end the players turn.</param>
        private async Task OnActionEnd(Lobby lobby, bool isTurnEndingMove) {
            // If the game is over, we're done
            if (await EndGameConditional(lobby)) {
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

        /// <summary>
        /// Ends the game if the conditions were met (1 or no non-spectator players left with more than 0 lives and in-game)
        /// </summary>
        /// <param name="lobby">The lobby to check the end game condition of.</param>
        /// <returns>Whether or not the conditions were met and the game was ended.</returns>
        private async Task<bool> EndGameConditional(Lobby lobby) {
            if (lobby.Players.Count(player => player.InGame && !player.IsSpectator && player.Lives > 0) <= 1) {
                await EndGame(lobby);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a game log message to the lobby.
        /// </summary>
        /// <param name="lobby">The lobby to add the game log message to.</param>
        /// <param name="message">The message to add to the game log.</param>
        private async Task AddGameLogMessage(Lobby lobby, string message) {
            var gameLogMessage = lobby.AddGameLogMessage(message);
            await Clients.Group(lobby.Id).GameLogUpdate(gameLogMessage);
        }
    }
}
