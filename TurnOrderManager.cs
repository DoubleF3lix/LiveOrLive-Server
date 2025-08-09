using LiveOrLiveServer.Models;
using System.Diagnostics.CodeAnalysis;

namespace LiveOrLiveServer {
    /// <summary>
    /// Manager for the turn order. Must call <c>Advance</c> to initialize the turn order before calling <c>GetPlayerForCurrentTurn</c>.
    /// </summary>
    /// <param name="players">The list of players for the lobby. Spectators are automatically filtered out and can be safely included.</param>
    public class TurnOrderManager(List<Player> players) {
        private readonly List<Player> _players = players;
        private int _currentTurnIndex = -1;

        /// <summary>
        /// Gets the turn order by usernames. Used for display and doesn't change after initialization.
        /// </summary>
        public List<string> TurnOrder => _players.Select(p => p.Username).ToList();

        /// <summary>
        /// Advances the turn by one, skipping spectators and dead players, looping around as necessary. Does not skip skipped players, since that should be handled by the lobby itself.
        /// </summary>
        /// <exception cref="Exception">Thrown if there are no eligible players left in the turn order (all dead).</exception>
        public void Advance() {
            if (_players.All(p => p.Lives <= 0)) {
                throw new Exception("Can't advance empty turn order");
            }

            Player? currentPlayerForTurn;
            do {
                _currentTurnIndex = (_currentTurnIndex + 1) % _players.Count;
                currentPlayerForTurn = _players[_currentTurnIndex];
            } while (currentPlayerForTurn.Lives <= 0);
        }

        /// <summary>
        /// Gets the player for the currrent turn.
        /// </summary>
        /// <returns>The player instance for the current turn.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the turn order was not initialized with <c>Advance</c> or the fetch failed for some other reason.</exception>
        public Player GetPlayerForCurrentTurn() {
            if (TryGetPlayerForCurrentTurn(out var result)) {
                return result;
            }
            throw new InvalidOperationException();
        }

        /// <summary>
        /// A safe variant of <c>GetPlayerForCurrentTurn</c> to fetch the player instance for the current turn.
        /// </summary>
        /// <param name="player">An <c>out</c> variable for the player instance for the current turn.</param>
        /// <returns><c>true</c> if there is a player for the current turn, <c>false</c> if not (likely because none was initialized).</returns>
        public bool TryGetPlayerForCurrentTurn([NotNullWhen(true)] out Player? player) {
            player = _currentTurnIndex >= 0 ? _players[_currentTurnIndex] : null;
            return player != null;
        }

        public void ReverseTurnOrder() {
            _players.Reverse();
            _currentTurnIndex = _players.Count - _currentTurnIndex - 1;
        }

        public void RemovePlayer(Player player) {
            var index = _players.IndexOf(player);
            if (index == -1) {
                throw new InvalidOperationException($"Tried to remove player from TurnOrderManager that didn't exist, username{player.Username}");
            }
            if (index <= _currentTurnIndex) {
                _currentTurnIndex--;
            }
            _players.RemoveAt(index);
        }
    }
}
