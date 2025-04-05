namespace liveorlive_server.Models {
    public class CreateLobbyRequest {
        public required string Username {  get; set; }
        public string? LobbyName { get; set; }
        public Settings Config { get; set; } = new Settings();
    }

    public class CreateLobbyResponse {
        public required string LobbyId { get; set; }
    }
}
