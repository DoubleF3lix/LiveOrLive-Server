﻿using Microsoft.AspNetCore.SignalR;
using liveorlive_server.Extensions;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace liveorlive_server.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IConnectionRequest {
        private readonly ConcurrentDictionary<string, HubCallerContext> _connectionContexts = new();
        private readonly Server _server;

        public LiveOrLiveHub(Server server) {
            _server = server;
        }

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

            var errorMessage = this._server.ValidateLobbyConnectionInfo(lobbyId, username);
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

            var lobby = this._server.GetLobbyById(lobbyId);

            // Store the lobbyId on the connection, and add this connection to the group
            Context.SetLobbyId(lobbyId);
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);

            // ValidateLobbyConnectionInfo ensures that there is no in-game player in the lobby with this username
            // So either they exist and we're good to take them, or they don't and we make a new one
            // This handles assigning an existing player or creating a new one
            var player = lobby.AddPlayer(Context.ConnectionId, username);

            // INFORM THE DEVELOPMENT TEAM. A NEW player HAS ARRIVED
            this._connectionContexts[Context.ConnectionId] = Context;
            await Clients.OthersInGroup(lobbyId).PlayerJoined(player);
            await Clients.Caller.ConnectionSuccess();

            if (lobby.Host == null) {
                lobby.Host = username;
                await Clients.Group(Context.GetLobbyId()).HostChanged(null, username, "Host joined");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception) {
            // Removal from group is handled automatically
            // https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs?view=aspnetcore-9.0#handle-events-for-a-connection
            await Console.Out.WriteLineAsync($"Disconnected: {Context.ConnectionId}");
            // Player may not exist if we got here on connection failure
            if (Context.TryGetPlayer(this._server, out var player)) {
                // Lobby is guarunteed to exist here
                var lobby = Context.GetLobby(this._server);
                // Remove entirely if game hasn't started
                if (!lobby.GameStarted) {
                    lobby.Players.Remove(player);
                // Otherwise mark as inactive
                } else {
                    player.InGame = false;
                    player.connectionId = null;
                }

                // Host transfer (find first player who isn't the one who just left, otherwise null)
                if (lobby.Host == player.Username) {
                    lobby.Host = lobby.Players.FirstOrDefault(p => p.InGame)?.Username;
                    // player.username is previous host
                    await Clients.Group(lobby.Id).HostChanged(player.Username, lobby.Host, "Host disconnected");
                }

                await Clients.Group(lobby.Id).PlayerLeft(player.Username);
                // Check if the game is over when someone leaves
                if (lobby.GameStarted) {
                    await EndGameConditional(lobby);
                }
            }
            this._connectionContexts.Remove(Context.ConnectionId, out _);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task KickPlayer(string username) {
            var lobby = Context.GetLobby(this._server);
            if (lobby.Host != Context.GetPlayer(this._server).Username) {
                await Clients.Caller.ActionFailed("You must be the host to do that!");
                return;
            }
            // Make sure they're connected. In case we want to support bots or something in the future, edit this
            if (!lobby.TryGetPlayerByUsername(username, out var player) || player.connectionId == null) {
                await Clients.Caller.ActionFailed($"Failed to kick player: couldn't find {username} (are they still in the game?)");
                return;
            }
            if (lobby.Host == username) {
                await Clients.Caller.ActionFailed("You can't kick yourself!");
                return;
            }
            await Clients.Group(Context.GetLobbyId()).PlayerKicked(username);
            this._connectionContexts[player.connectionId].Abort();
            // Deleting is handled as this triggers OnDisconnectedAsync
        }

        public async Task SetHost(string username) {
            var lobby = Context.GetLobby(this._server);
            if (lobby.Host != Context.GetPlayer(this._server).Username) {
                await Clients.Caller.ActionFailed("You must be the host to do that!");
                return;
            }
            if (!lobby.TryGetPlayerByUsername(username, out var newHost)) {
                await Clients.Caller.ActionFailed($"Failed to transfer host: couldn't find {username}");
                return;
            }
            if (!newHost.InGame) {
                await Clients.Caller.ActionFailed($"Failed to transfer host: {username} is not in-game");
                return;
            }
            lobby.Host = username;
            await Clients.Group(Context.GetLobbyId()).HostChanged(lobby.Host, username, "Host was transferred");
        }
    }
}
