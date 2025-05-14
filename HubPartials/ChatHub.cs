using liveorlive_server.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace liveorlive_server.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IChatRequest {
        public async Task SendChatMessage(string content) {
            var lobby = Context.GetLobby(this._server);
            // Take only the first 500 characters. No spam 4 u
            var chatMessage = lobby.AddChatMessage(Context.GetPlayer(this._server).Username, content[..Math.Min(content.Length, 500)]);
            await Clients.Group(lobby.Id).ChatMessageSent(chatMessage);
        }

        public async Task GetChatMessagesRequest() {
            var lobby = Context.GetLobby(this._server);
            await Clients.Caller.GetChatMessagesResponse(lobby.ChatMessages);
        }

        public async Task DeleteChatMessage(Guid messageId) {

        }

        public async Task EditChatMessage(Guid messageId, string content) {

        }
    }
}
