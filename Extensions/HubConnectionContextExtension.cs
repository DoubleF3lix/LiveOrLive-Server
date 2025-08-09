using LiveOrLiveServer.Models;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics.CodeAnalysis;

namespace LiveOrLiveServer.Extensions {
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

        public static bool TryGetClient(this HubCallerContext context, Server server, [NotNullWhen(true)] out ConnectedClient? client) {
            client = null;
            if (context.TryGetLobby(server, out var lobby)) {
                client = lobby.Clients.FirstOrDefault(client => client.ConnectionId == context.ConnectionId);
            }
            return client != null;
        }

        public static bool TryGetPlayer(this HubCallerContext context, Server server, [NotNullWhen(true)] out Player? player) {
            player = null;
            if (context.TryGetLobby(server, out var lobby)) {
                player = lobby.Players.FirstOrDefault(player => player.ConnectionId == context.ConnectionId);
            }
            return player != null;
        }

        public static Lobby GetLobby(this HubCallerContext context, Server server) {
            if (context.TryGetLobby(server, out var result)) {
                return result;
            }
            throw new InvalidOperationException();
        }

        public static ConnectedClient GetClient(this HubCallerContext context, Server server) {
            if (context.TryGetClient(server, out var result)) {
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
