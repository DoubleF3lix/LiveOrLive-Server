using Tapper;

namespace LiveOrLiveServer.Models.Dto {
    [TranspilationSource]
    public record LobbyDto {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required long CreationTime { get; set; }
        public required Settings Settings { get; set; }
        public required List<PlayerDto> Players { get; set; } = [];
        public required List<SpectatorDto> Spectators { get; set; } = [];

        public required string? Host { get; set; }
        public required bool GameStarted { get; set; }

        public required List<string> TurnOrder { get; set; } = [];
        public required string? CurrentTurn { get; set; }
        public required bool SuddenDeathActivated { get; set; }
        public required int RicochetCounter { get; set; }
    }
}
