using Microsoft.AspNetCore.SignalR;
using liveorlive_server.Extensions;
using System.Diagnostics;

namespace liveorlive_server.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse> {
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

            // Check if we need to reassign player object
            // ValidateLobbyConnectionInfo ensures that there is no in-game player in the lobby with this username
            // So either they exist and we're good to take them, or they don't and we make a new one
            var existingPlayerWithUsername = lobby.TryGetPlayerByUsername(username);
            if (existingPlayerWithUsername != null) {
                existingPlayerWithUsername.connectionId = Context.ConnectionId;
                existingPlayerWithUsername.inGame = true;
            } else {
                // If not, make a new one
                lobby.players.Add(new Player(lobby.Config, username, Context.ConnectionId, lobby.gameStarted));
            }

            if (lobby.players.Count == 1) {
                lobby.host = username;
                // Don't bother sending if we're the only player we'd be sending it to
                // await Clients.Group(Context.GetLobbyId()).HostChanged("N/A", username, "Host joined");
            }

            // Store the lobbyId on the connection, and add this connection to the group
            Context.SetLobbyId(lobbyId);
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
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
                    var previousHost = lobby.host;
                    lobby.host = lobby.players.FirstOrDefault(p => p.inGame && p.username != player.username)?.username;
                    await Clients.Group(Context.GetLobbyId()).HostChanged(previousHost, lobby.host ?? "N/A", "Host disconnected");
                }

            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
