using LiveOrLiveServer.Enums;
using Tapper;

namespace LiveOrLiveServer.Models.Dto {
    [TranspilationSource]
    public record PlayerDto : ConnectedClientDto {
        public required bool InGame { get; set; }
        public required int Lives { get; set; }
        public required List<Item> Items { get; set; }
        public required bool IsSkipped { get; set; }
        public required bool IsRicochet { get; set; }
        public required bool ImmuneToSkip { get; set; }
        public required int ReviveCount { get; set; }
        public required bool Eliminated { get; set; }
    }
}
