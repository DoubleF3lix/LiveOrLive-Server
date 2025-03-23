using Microsoft.AspNetCore.SignalR;

namespace liveorlive_server.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IBaseGameRequest {
        public async Task GameDataRequest() {
            
        }

        public async Task ShootPlayer(string target) {
            
        }
    }
}
