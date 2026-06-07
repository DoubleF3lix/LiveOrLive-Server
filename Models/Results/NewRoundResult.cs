using LiveOrLiveServer.Enums;
using Tapper;

namespace LiveOrLiveServer.Models.Results {
    [TranspilationSource]
    public record NewRoundResult {
        public int BlankRounds { get; set; }
        public int LiveRounds { get; set; }
        public required Dictionary<string, List<Item>> DealtItems { get; set; }
    }
}
