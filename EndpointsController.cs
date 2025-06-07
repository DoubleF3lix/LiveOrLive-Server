using liveorlive_server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace liveorlive_server {
    [ApiController]
    public class EndpointsController(Server server) : ControllerBase {
        private readonly Server _server = server;
        private readonly JsonSerializerOptions JSON_OPTIONS = new() { IncludeFields = true, WriteIndented = true };

        [HttpGet("/lobbies")]
        public string GetLobbies() {
            return JsonSerializer.Serialize(_server.Lobbies.Where(lobby => !lobby.Private), JSON_OPTIONS);
        }

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

        [HttpGet("/verify-lobby-connection-info")]
        public IResult VerifyLobbyConnectionInfo([FromQuery] string lobbyId, [FromQuery] string username) {
            var errorMessage = _server.ValidateLobbyConnectionInfo(lobbyId, username);
            if (errorMessage != null) {
                return Results.BadRequest(errorMessage);
            }

            return Results.Ok();
        }

        [HttpGet("/default-settings")]
        public string GetDefaultSettings() {
            return JsonSerializer.Serialize(new Settings());
        }
    }
}
