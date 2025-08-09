using LiveOrLiveServer.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace LiveOrLiveServer.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IGameLogRequest {
        public async Task GetGameLogRequest() {
            var lobby = Context.GetLobby(_server);
            await Clients.Caller.GetGameLogResponse(lobby.GameLogMessages);
        }
    }
}
