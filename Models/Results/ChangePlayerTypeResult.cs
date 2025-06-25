namespace liveorlive_server.Models.Results {
    public class ChangePlayerToSpectatorResult {
        public required Spectator NewSpectator { get; set; }
        public bool ForfeitTurn { get; set; } = false;
    }

    public class ChangeSpectatorToPlayerResult {
        public required Player NewPlayer { get; set; }
    }
}
