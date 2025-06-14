namespace liveorlive_server.Models.Results {
    public abstract class TurnResult {
        public required string PlayerUsername { get; set; }
    }

    public class StartTurnResult : TurnResult { }

    public class EndTurnResult : TurnResult {
        public bool EndDueToSkip { get; set; }
    }
}
