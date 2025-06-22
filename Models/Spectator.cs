using Tapper;

namespace liveorlive_server.Models {
    [TranspilationSource]
    public class Spectator(string username, string? connectionId) : ConnectedClient(username, connectionId) {}
}
