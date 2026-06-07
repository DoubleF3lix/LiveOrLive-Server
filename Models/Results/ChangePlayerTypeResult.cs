namespace LiveOrLiveServer.Models.Results {
    public record ChangePlayerToSpectatorResult {
        public required Spectator NewSpectator { get; set; }
        public bool ForfeitTurn { get; set; } = false;
    }

    public record ChangeSpectatorToPlayerResult {
        public required Player NewPlayer { get; set; }
    }
}
