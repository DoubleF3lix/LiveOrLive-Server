namespace LiveOrLiveServer.Models.Results {
    public abstract class NewTurnResult {
        public required string PlayerUsername { get; set; }
    }

    public class StartTurnResult : NewTurnResult { }

    public class EndTurnResult : NewTurnResult {
        public bool EndDueToSkip { get; set; }
    }
}
