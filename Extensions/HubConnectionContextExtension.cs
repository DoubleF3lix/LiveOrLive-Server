using Microsoft.AspNetCore.SignalR;
using System.Diagnostics.CodeAnalysis;

namespace liveorlive_server.Extensions {
    public static class HubCallerContextExtension {
        public static void SetLobbyId(this HubCallerContext context, string gameId) {
            context.Items["lobbyId"] = gameId;
        }

        public static bool TryGetLobbyId(this HubCallerContext context, [NotNullWhen(true)] out string? lobbyId) {
            lobbyId = context.Items["lobbyId"]?.ToString();
            return lobbyId != null;
        }

        public static bool TryGetLobby(this HubCallerContext context, Server server, [NotNullWhen(true)] out Lobby? lobby) {
            lobby = null;
            return context.TryGetLobbyId(out var lobbyId) && server.TryGetLobbyById(lobbyId, out lobby);
        }

        public static bool TryGetPlayer(this HubCallerContext context, Server server, [NotNullWhen(true)] out Player? player) {
            player = null;
            if (context.TryGetLobby(server, out var lobby)) {
                player = lobby.Players.FirstOrDefault(player => player.connectionId == context.ConnectionId);
            }
            return player != null;
        }

        public static string GetLobbyId(this HubCallerContext context) {
            if (context.TryGetLobbyId(out var result)) {
                return result;
            }
            throw new InvalidOperationException();
        }

        public static Lobby GetLobby(this HubCallerContext context, Server server) {
            if (context.TryGetLobby(server, out var result)) {
                return result;
            }
            throw new InvalidOperationException();
        }

        public static Player GetPlayer(this HubCallerContext context, Server server) {
            if (context.TryGetPlayer(server, out var result)) {
                return result;
            }
            throw new InvalidOperationException();
        }
    }
}
