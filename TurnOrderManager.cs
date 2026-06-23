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
        /// Gets the turn order by usernames. Used for display and only changes as players are eliminated.
        /// </summary>
        public List<string> TurnOrder => [.. _players.Where(p => !p.Eliminated).Select(p => p.Username)];

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

        /// <summary>
        /// Reverses the turn order.
        /// </summary>
        public void ReverseTurnOrder() {
            _players.Reverse();
            _currentTurnIndex = _players.Count - _currentTurnIndex - 1;
        }

        /// <summary>
        /// Removes a player from the turn order.
        /// </summary>
        /// <param name="player">The player to remove from the turn order.</param>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="player"/> isn't found in the turn order.</exception>
        public void RemovePlayer(Player player) {
            var index = _players.IndexOf(player);
            if (index == -1) {
                throw new InvalidOperationException($"Tried to remove player from TurnOrderManager that didn't exist, username: {player.Username}");
            }
            if (index <= _currentTurnIndex) {
                _currentTurnIndex--;
            }
            _players.RemoveAt(index);
        }

        /// <summary>
        /// Fetches the player after another player in the turn order, wrapping around as needed. Used for ricochet checking.
        /// </summary>
        /// <param name="player">The player that we want to fetch the next player relative to.</param>
        /// <returns>The player after <paramref name="player"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="player"/> isn't found in the turn order.</exception>
        public Player GetPlayerAfter(Player player) {
            var index = _players.IndexOf(player);
            if (index == -1) {
                throw new InvalidOperationException($"Tried to fetch player after a player that didn't exist, username: {player.Username}");
            }
            return _players[(index + 1) % _players.Count];
        }

        /// Unused for now. May be used if a ricochet hitting a skipped player makes it pong ping to a random player.
        private Player GetRandomPlayerExcept(Player player) {
            if (_players.Count <= 1) {
                throw new InvalidOperationException("Cannot select a random player when there are no other players.");
            }

            var excludedPlayerIndex = _players.IndexOf(player);
            if (excludedPlayerIndex == -1) {
                throw new InvalidOperationException($"Tried to fetch a random player excluding a player that didn't exist, username: {player.Username}");
            }

            // Random index as Count - 1...
            var randomPlayerIndex = Random.Shared.Next(_players.Count - 1);
            // ...then create a gap around the skipped player
            if (randomPlayerIndex >= excludedPlayerIndex) {
                randomPlayerIndex++;
            }
            return _players[randomPlayerIndex];
        }
    }
}
