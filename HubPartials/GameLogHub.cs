using liveorlive_server.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace liveorlive_server.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IGameLogRequest {
        public async Task GetGameLogRequest() {
            var lobby = Context.GetLobby();
            await Clients.Caller.GetGameLogResponse(lobby.GameLogMessages);
        }
    }
}
