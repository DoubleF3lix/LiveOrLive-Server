using liveorlive_server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace liveorlive_server {
    [ApiController]
    public class EndpointsController(Server server) : ControllerBase {
        private readonly Server _server = server;
        private readonly JsonSerializerOptions JSON_OPTIONS = new() { IncludeFields = true, WriteIndented = true };

        /// <summary>
        /// GET request to fetch all public lobbies as JSON.
        /// </summary>
        /// <returns>JSON string of lobbies list with their info, such as settings.</returns>
        [HttpGet("/lobbies")]
        public string GetLobbies() {
            return JsonSerializer.Serialize(_server.Lobbies.Where(lobby => !lobby.Private), JSON_OPTIONS);
        }

        /// <summary>
        /// POST request to create a lobby.
        /// </summary>
        /// <param name="request">The request to create the lobby. Includes username of the creating player, lobby name, and settings.</param>
        /// <returns>200 if successful, 400 on failure.</returns>
        [HttpPost("/lobbies")]
        public IResult CreateLobby(CreateLobbyRequest request) {
            if (request.Username == null) {
                return Results.BadRequest("Username not supplied");
            }
            if (request.Username.Length > 20 || request.Username.Length < 2) {
                return Results.BadRequest("Username must be between 2 and 20 characters");
            }

            // Config is default initialized by auto-binding magic if it isn't set
            // And if any properties are missing, they get their default values too
            var newLobby = _server.CreateLobby(request.Settings, request.LobbyName);

            return Results.Ok(new CreateLobbyResponse { LobbyId = newLobby.Id });
        }

        /// <summary>
        /// GET request to verify lobby connection info.
        /// </summary>
        /// <param name="lobbyId">Lobby ID to be connected to, passed in query string.</param>
        /// <param name="username">Username to connect with, passed in query string.</param>
        /// <returns>200 if connection info is valid, 400 if lobby couldn't be found or username was taken.</returns>
        [HttpGet("/verify-lobby-connection-info")]
        public IResult VerifyLobbyConnectionInfo([FromQuery] string lobbyId, [FromQuery] string username) {
            var errorMessage = _server.ValidateLobbyConnectionInfo(lobbyId, username);
            if (errorMessage != null) {
                return Results.BadRequest(errorMessage);
            }

            return Results.Ok();
        }

        /// <summary>
        /// GET request to fetch the default settings
        /// </summary>
        /// <returns>JSON string with the default settings.</returns>
        [HttpGet("/default-settings")]
        public string GetDefaultSettings() {
            return JsonSerializer.Serialize(new Settings());
        }
    }
}
