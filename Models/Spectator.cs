using LiveOrLiveServer.Models.Dto;

namespace LiveOrLiveServer.Models {
    public class Spectator(string username, string? connectionId) : ConnectedClient(username, connectionId) {
        public SpectatorDto ToDto() {
            return new SpectatorDto { 
                Username = Username, 
                JoinTime = JoinTime,
                ClientType = ClientType 
            };
        }
    }
}
