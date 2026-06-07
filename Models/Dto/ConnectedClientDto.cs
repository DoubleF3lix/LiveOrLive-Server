using LiveOrLiveServer.Enums;
using Tapper;

namespace LiveOrLiveServer.Models.Dto {
    [TranspilationSource]
    public record ConnectedClientDto {
        public required string Username { get; set; }
        public required long JoinTime { get; set; }
        public required ClientType ClientType { get; set; }
    }
}
