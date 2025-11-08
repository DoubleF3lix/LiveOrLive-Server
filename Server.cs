using LiveOrLiveServer.Models;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace LiveOrLiveServer {
    public class Server {
        public readonly ConcurrentBag<Lobby> Lobbies = [];

        public Server() {
            // TODO remove
            CreateLobby(name: "Gambling Addiction");
        }

        /// <summary>
        /// Validates information needed to connect to a lobby before actually connecting. Can be used client side to "pre-auth" details, but used by the server itself to reject invalid information.
        /// </summary>
        /// <param name="lobbyId">The ID of the lobby to check for validity.</param>
        /// <param name="username">The username to check for validity, in the selected lobby. Not checked if the lobby doesn't exist.</param>
        /// <returns>An error message as a string, <c>null</c> if valid.</returns>
        public string? ValidateLobbyConnectionInfo(string? lobbyId, string? username) {
            if (lobbyId == null || username == null) {
                return "Missing lobbyId or username";
            }

            var lobby = Lobbies.FirstOrDefault(lobby => lobby.Id == lobbyId);
            if (lobby == null) {
                return "Couldn't locate lobby";
            }

            if (username.Length > 20 || username.Length < 2) {
                return "Username must be between 2 and 20 characters";
            }

            var existingPlayerWithUsername = lobby.Players.FirstOrDefault(player => player.Username == username);
            if (existingPlayerWithUsername != null && existingPlayerWithUsername.InGame) {
                return "Username is already taken";
            }

            // No error
            return null;
        }

        /// <summary>
        /// Creates a lobby with a random ID.
        /// </summary>
        /// <param name="settings">Optional <c>Settings</c> object. Uses default settings if <c>null</c>.</param>
        /// <param name="name">Optional name for the lobby. Set to the lobby ID if <c>null</c>.</param>
        /// <returns>The newly created <c>Lobby</c> instance.</returns>
        public Lobby CreateLobby(Settings? settings = null, string? name = null) {
            var newLobby = new Lobby(GenerateId(), settings, name);
            Lobbies.Add(newLobby);
            return newLobby;
        }

        /// <summary>
        /// Fetches a lobby from the server by ID. Errors if it doesn't exist.
        /// </summary>
        /// <param name="lobbyId">The lobby ID to fetch by.</param>
        /// <returns>The <c>Lobby</c> instance matching the ID.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the lobby couldn't be found.</exception>
        public Lobby GetLobbyById(string lobbyId) {
            if (TryGetLobbyById(lobbyId, out var result)) {
                return result;
            }
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Attempts to fetch a lobby from this server by ID.
        /// </summary>
        /// <param name="lobbyId">The lobby ID to fetch by.</param>
        /// <param name="lobby">The <c>Lobby</c> instance for the matched lobby, or <c>null</c> if not found.</param>
        /// <returns>A boolean matching if the lobby could be found.</returns>
        public bool TryGetLobbyById(string? lobbyId, [NotNullWhen(true)] out Lobby? lobby) {
            lobby = Lobbies.FirstOrDefault(lobby => lobby.Id == lobbyId);
            return lobby != null;
        }

        /// <summary>
        /// Generates a random 4-character ID, guarunteed to be unused by any other lobby. Used on lobby creation.
        /// </summary>
        /// <returns>The randomly generated ID.</returns>
        public string GenerateId() {
            string? id;
            do {
                id = Guid.NewGuid()
                    .ToString("N")
                    .ToUpper()
                    .Replace("0", "")
                    .Replace("O", "")
                    .Replace("I", "")
                    .Replace("1", "")
                    .Replace("5", "")
                    .Replace("S", "")
                    [..4]; 
            } while (id != "" && TryGetLobbyById(id, out _));
            return id;
        }
    }
}
