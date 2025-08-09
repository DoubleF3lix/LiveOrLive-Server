using LiveOrLiveServer.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace LiveOrLiveServer.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IChatRequest {
        public async Task SendChatMessage(string content) {
            var lobby = Context.GetLobby(_server);
            var player = Context.GetPlayer(_server);
            // Take only the first 500 characters. No spam 4 u
            var chatMessage = lobby.AddChatMessage(player.Username, content[..Math.Min(content.Length, 500)]);
            await Clients.Group(lobby.Id).ChatMessageSent(chatMessage);
        }

        public async Task GetChatMessagesRequest() {
            var lobby = Context.GetLobby(_server);
            await Clients.Caller.GetChatMessagesResponse(lobby.ChatMessages);
        }

        public async Task DeleteChatMessage(Guid messageId) {

        }

        public async Task EditChatMessage(Guid messageId, string content) {

        }
    }
}
