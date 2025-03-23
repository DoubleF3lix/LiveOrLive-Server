using Microsoft.AspNetCore.SignalR;

namespace liveorlive_server.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IGameLogRequest {
        public async Task GetGameLogRequest() {

        }
    }
}
