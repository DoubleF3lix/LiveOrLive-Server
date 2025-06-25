using Microsoft.AspNetCore.SignalR;
using liveorlive_server.Extensions;
using System.Diagnostics;
using System.Collections.Concurrent;
using liveorlive_server.Models;

namespace liveorlive_server.HubPartials {
    public partial class LiveOrLiveHub(Server server) : Hub<IHubServerResponse>, IConnectionRequest {
        private readonly static ConcurrentDictionary<string, HubCallerContext> _connectionContexts = [];
        private readonly Server _server = server;

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

            var errorMessage = _server.ValidateLobbyConnectionInfo(lobbyId, username);
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

            var lobby = _server.GetLobbyById(lobbyId);

            // Store the lobbyId on the connection, and add this connection to the group
            Context.SetLobbyId(lobbyId);
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);

            // ValidateLobbyConnectionInfo ensures that there is no in-game player in the lobby with this username
            // So either they exist and we're good to take them, or they don't and we make a new one
            // This handles assigning an existing player or creating a new one
            var player = lobby.AddClient(Context.ConnectionId, username);

            // INFORM THE MEN. A NEW client HAS ARRIVED
            _connectionContexts[Context.ConnectionId] = Context;
            await Clients.OthersInGroup(lobbyId).ClientJoined(player);
            await Clients.Caller.ConnectionSuccess();

            if (lobby.Host == null) {
                await ChangeHost(lobby, username, "Host joined");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception) {
            // Removal from group is handled automatically
            // https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs?view=aspnetcore-9.0#handle-events-for-a-connection
            await Console.Out.WriteLineAsync($"Disconnected: {Context.ConnectionId}");
            // Client may not exist if we got here on connection failure
            if (Context.TryGetClient(_server, out var client)) {
                // Lobby is guarunteed to exist here
                var lobby = Context.GetLobby(_server);

                // Always remove spectators since we don't track InGame
                if (client is Spectator spectator) {
                    lobby.Spectators.Remove(spectator);
                } else if (client is Player player) {
                    // Remove entirely if game hasn't started
                    if (!lobby.GameStarted) {
                        lobby.Players.Remove(player);
                        // Otherwise mark as inactive
                    } else {
                        player.InGame = false;
                        player.ConnectionId = null;
                    }
                }

                // Host transfer (find first player who isn't the one who just left, otherwise null)
                if (lobby.Host == client.Username) {
                    var newHost = lobby.Clients.FirstOrDefault(p => (p is Player player && player.InGame) || p is Spectator)?.Username;
                    await ChangeHost(lobby, newHost, "Host disconnected");
                }

                await Clients.Group(lobby.Id).ClientLeft(client.Username);
                if (client is Player forfeitingPlayer) {
                    if (forfeitingPlayer.Username == lobby.CurrentTurn) {
                        await ForfeitTurn(lobby, forfeitingPlayer);
                    }
                    // Check if the game is over when someone leaves (only for a player though, we don't care about spectators)
                    if (lobby.GameStarted) {
                        await EndGameConditional(lobby);
                    }
                }
            }
            _connectionContexts.Remove(Context.ConnectionId, out _);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task KickPlayer(string username) {
            var lobby = Context.GetLobby(_server);
            if (lobby.Host != Context.GetClient(_server).Username) {
                await Clients.Caller.ActionFailed("You must be the host to do that!");
                return;
            }
            // Make sure they're connected. In case we want to support bots or something in the future, edit this.
            if (!lobby.TryGetClientByUsername(username, out var player) || player.ConnectionId == null) {
                await Clients.Caller.ActionFailed($"Failed to kick player: couldn't find {username} (are they still in the game?)");
                return;
            }
            if (lobby.Host == username) {
                await Clients.Caller.ActionFailed("You can't kick yourself!");
                return;
            }
            await Clients.Group(lobby.Id).ClientKicked(username);
            _connectionContexts[player.ConnectionId].Abort();
            // Deleting is handled as this triggers OnDisconnectedAsync
        }

        public async Task SetHost(string username) {
            var lobby = Context.GetLobby(_server);
            if (lobby.Host != Context.GetClient(_server).Username) {
                await Clients.Caller.ActionFailed("You must be the host to do that!");
                return;
            }
            if (!lobby.TryGetClientByUsername(username, out var newHost)) {
                await Clients.Caller.ActionFailed($"Failed to transfer host: couldn't find {username}");
                return;
            }
            if (newHost is Player newHostPlayer && !newHostPlayer.InGame) {
                await Clients.Caller.ActionFailed($"Failed to transfer host: {username} is not in-game");
                return;
            }
            await ChangeHost(lobby, username, "Host was manually transferred");
        }

        public async Task SwitchPlayerType() {
            var lobby = Context.GetLobby(_server);
            if (!Context.TryGetClient(_server, out var client)) {
                return;
            }

            switch (client) {
                // Move to players
                case Spectator spectator:
                    if (lobby.GameStarted) {
                        await Clients.Caller.ActionFailed("You can only switch to a player when a game is not in progress!");
                        return;
                    }
                    var newPlayerResult = lobby.ChangeSpectatorToPlayer(spectator);
                    await Clients.Group(lobby.Id).PlayerTypeChanged(newPlayerResult.NewPlayer);
                    break;
                // Move to spectators
                case Player player:
                    var newSpectatorResult = lobby.ChangePlayerToSpectator(player);
                    await Clients.Group(lobby.Id).PlayerTypeChanged(newSpectatorResult.NewSpectator);
                    break;
            }
        }
    }
}
