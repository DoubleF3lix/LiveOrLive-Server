using Microsoft.AspNetCore.SignalR;

namespace LiveOrLiveServer.HubPartials {
    public partial class LiveOrLiveHub : Hub<IHubServerResponse>, IGenericRequest {
    }
}
