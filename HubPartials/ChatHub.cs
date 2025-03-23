using Microsoft.AspNetCore.SignalR;

namespace liveorlive_server.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IChatRequest {
        public async Task SendChatMessage(string content) {
            
        }

        public async Task GetChatMessagesRequest() {

        }

        public async Task DeleteChatMessage(Guid messageId) {

        }

        public async Task EditChatMessage(Guid messageId, string content) {

        }
    }
}
