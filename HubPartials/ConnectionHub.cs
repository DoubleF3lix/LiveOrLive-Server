using Microsoft.AspNetCore.SignalR;
using liveorlive_server.Extensions;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace liveorlive_server.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IConnectionRequest {
        private static readonly ConcurrentDictionary<string, HubCallerContext> _connectionContexts = new();

        public override async Task OnConnectedAsync() {
            await Console.Out.WriteLineAsync($"Connected: {Context.ConnectionId}");

            // Make sure we have gameId, otherwise reject the connection
            var context = Context.GetHttpContext();
            if (context == null) {
                await Clients.Caller.ActionFailed("FATAL: Missing HttpContext");
                Context.Abort();
                return;
            }

            var lobbyId = context.GetStringQueryParam("lobbyId");
            var username = context.GetStringQueryParam("username");

            var errorMessage = Server.ValidateLobbyConnectionInfo(lobbyId, username);
            if (errorMessage != null) {
                await Clients.Caller.ConnectionFailed(errorMessage);
                // Give some time to the client to handle disconnecting before we forcibly kick them out
                // Client sometimes throws an error that we disconnected before hub handshake was done
                await Task.Delay(50);
                Context.Abort();
                return;
            }
            // Validation confirms they're not null
            Debug.Assert(lobbyId != null);
            Debug.Assert(username != null);

            var lobby = Server.GetLobbyById(lobbyId);

            // Store the lobbyId on the connection, and add this connection to the group
            Context.SetLobbyId(lobbyId);
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);

            // Check if we need to reassign player object
            // ValidateLobbyConnectionInfo ensures that there is no in-game player in the lobby with this username
            // So either they exist and we're good to take them, or they don't and we make a new one
            // Could use ?? here, but using lobby.players.Contains seems iffy
            if (lobby.TryGetPlayerByUsername(username, out Player? player)) {
                player.connectionId = Context.ConnectionId;
                player.inGame = true;
            } else {
                player = new Player(lobby.Config, username, Context.ConnectionId, lobby.gameStarted);
                lobby.players.Add(player);
            }

            if (lobby.players.Count == 1) {
                lobby.host = username;
                // Don't bother sending if we're the only player we'd be sending it to
                await Clients.Group(Context.GetLobbyId()).HostChanged(null, username, "Host joined");
            }

            // INFORM THE DEVELOPMENT TEAM. A NEW player HAS ARRIVED
            _connectionContexts[Context.ConnectionId] = Context;
            await Clients.OthersInGroup(lobbyId).PlayerJoined(player);
            await Clients.Caller.ConnectionSuccess();
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception) {
            // Removal from group is handled automatically
            // https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs?view=aspnetcore-9.0#handle-events-for-a-connection
            await Console.Out.WriteLineAsync($"Disconnected: {Context.ConnectionId}");
            // Player may not exist if we got here on connection failure
            if (Context.TryGetPlayer(out var player)) {
                // Lobby is guarunteed to exist here
                var lobby = Context.GetLobby();
                // Remove entirely if game hasn't started
                if (!lobby.gameStarted) {
                    lobby.players.Remove(player);
                // Otherwise mark as inactive
                } else {
                    player.inGame = false;
                }

                // Host transfer (find first player who isn't the one who just left, otherwise null)
                if (lobby.host == player.username) {
                    lobby.host = lobby.players.FirstOrDefault(p => p.inGame)?.username;
                    // player.username is previous host
                    await Clients.Group(Context.GetLobbyId()).HostChanged(player.username, lobby.host, "Host disconnected");
                }

            }
            _connectionContexts.Remove(Context.ConnectionId, out _);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task KickPlayer(string username) {
            var lobby = Context.GetLobby();
            if (lobby.host != Context.GetPlayer().username) {
                await Clients.Caller.ActionFailed("You must be the host to kick a player");
                return;
            }
            if (!lobby.TryGetPlayerByUsername(username, out var player)) {
                await Clients.Caller.ActionFailed($"Couldn't find {username}");
                return;
            }
            await Clients.Group(Context.GetLobbyId()).PlayerKicked(username);
            _connectionContexts[player.connectionId].Abort();
        }

        public async Task SetHost(string username) {
            var lobby = Context.GetLobby();
            if (lobby.host != Context.GetPlayer().username) {
                await Clients.Caller.ActionFailed("You must be the host to transfer it to another player");
                return;
            }
            if (!lobby.TryGetPlayerByUsername(username, out var _)) {
                await Clients.Caller.ActionFailed($"Couldn't find {username}");
                return;
            }
            await Clients.Group(Context.GetLobbyId()).HostChanged(lobby.host, username, "Host transferred");
            lobby.host = username;
        }
    }
}
