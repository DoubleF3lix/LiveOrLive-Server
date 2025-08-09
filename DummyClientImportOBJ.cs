using LiveOrLiveServer.Models;
using Tapper;

namespace LiveOrLiveServer {
    [TranspilationSource]
    public class DummyClientImportOBJ {
        // The auto-generated client code imports these things in the .Models folder even if nothing uses them
        // This throws a TS error and makes builds fail
        // So, make a dummy object just so something imports them
        public ConnectedClient? This { get; set; }
        public ChatMessage? Is { get; set; }
        public GameLogMessage? Stupid { get; set; }
    }
}
