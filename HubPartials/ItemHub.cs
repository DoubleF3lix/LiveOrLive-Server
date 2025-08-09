using LiveOrLiveServer.Enums;
using LiveOrLiveServer.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace LiveOrLiveServer.HubPartials
{
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IItemRequest {
        public async Task UseReverseTurnOrderItem() {
            var lobby = Context.GetLobby(_server);
            var player = Context.GetPlayer(_server);

            if (lobby.CurrentTurn != player.Username) {
                await Clients.Caller.ActionFailed("It must be your turn to use an item!");
                return;
            }

            await UseReverseTurnOrderItemActual(lobby, player);
        }

        public async Task UseRackChamberItem() {
            var lobby = Context.GetLobby(_server);
            var player = Context.GetPlayer(_server);

            if (lobby.CurrentTurn != player.Username) {
                await Clients.Caller.ActionFailed("It must be your turn to use an item!");
                return;
            }

            await UseRackChamberItemActual(lobby, player);
        }

        public async Task UseExtraLifeItem(string target) {
            var lobby = Context.GetLobby(_server);
            var player = Context.GetPlayer(_server);

            if (lobby.CurrentTurn != player.Username) {
                await Clients.Caller.ActionFailed("It must be your turn to use an item!");
                return;
            }

            await UseExtraLifeItemActual(lobby, player, target);
        }

        public async Task UsePickpocketItem(string target, Item item, string? itemTarget) {
            var lobby = Context.GetLobby(_server);
            var player = Context.GetPlayer(_server);

            if (lobby.CurrentTurn != player.Username) {
                await Clients.Caller.ActionFailed("It must be your turn to use an item!");
                return;
            }

            await UsePickpocketItemActual(lobby, player, target, item, itemTarget);
        }

        public async Task UseLifeGambleItem() {
            var lobby = Context.GetLobby(_server);
            var player = Context.GetPlayer(_server);

            if (lobby.CurrentTurn != player.Username) {
                await Clients.Caller.ActionFailed("It must be your turn to use an item!");
                return;
            }

            await UseLifeGambleItemActual(lobby, player);
        }

        public async Task UseInvertItem() {
            var lobby = Context.GetLobby(_server);
            var player = Context.GetPlayer(_server);

            if (lobby.CurrentTurn != player.Username) {
                await Clients.Caller.ActionFailed("It must be your turn to use an item!");
                return;
            }

            await UseInvertItemActual(lobby, player);
        }

        public async Task UseChamberCheckItem() {
            var lobby = Context.GetLobby(_server);
            var player = Context.GetPlayer(_server);

            if (lobby.CurrentTurn != player.Username) {
                await Clients.Caller.ActionFailed("It must be your turn to use an item!");
                return;
            }

            await UseChamberCheckItemActual(lobby, player);
        }

        public async Task UseDoubleDamageItem() {
            var lobby = Context.GetLobby(_server);
            var player = Context.GetPlayer(_server);

            if (lobby.CurrentTurn != player.Username) {
                await Clients.Caller.ActionFailed("It must be your turn to use an item!");
                return;
            }

            await UseDoubleDamageItemActual(lobby, player);
        }

        public async Task UseSkipItem(string target) {
            var lobby = Context.GetLobby(_server);
            var player = Context.GetPlayer(_server);

            if (lobby.CurrentTurn != player.Username) {
                await Clients.Caller.ActionFailed("It must be your turn to use an item!");
                return;
            }

            await UseSkipItemActual(lobby, player, target);
        }

        public async Task UseRicochetItem(string target) {
            var lobby = Context.GetLobby(_server);
            var player = Context.GetPlayer(_server);

            if (lobby.CurrentTurn != player.Username) {
                await Clients.Caller.ActionFailed("It must be your turn to use an item!");
                return;
            }

            await UseRicochetItemActual(lobby, player, target);
        }
    }
}