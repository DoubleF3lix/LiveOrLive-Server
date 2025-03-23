namespace liveorlive_server.Models {
    public class CreateLobbyRequest {
        public required string Username {  get; set; }
        public string? LobbyName { get; set; }
        public Config Config { get; set; } = new Config();
    }

    public class CreateLobbyResponse {
        public required string LobbyId { get; set; }
    }
}
