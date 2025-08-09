using Tapper;

namespace LiveOrLiveServer.Models {
    [TranspilationSource]
    public class Spectator(string username, string? connectionId) : ConnectedClient(username, connectionId) {}
}
