using LiveOrLiveServer.Enums;
using Tapper;

namespace LiveOrLiveServer.Models.Results {
    [TranspilationSource]
    public class NewRoundResult {
        public int BlankRounds { get; set; }
        public int LiveRounds { get; set; }
        public required Dictionary<string, List<Item>> DealtItems { get; set; }
    }
}
