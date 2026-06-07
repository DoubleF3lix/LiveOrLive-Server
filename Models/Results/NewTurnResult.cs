namespace LiveOrLiveServer.Models.Results {
    public abstract record NewTurnResult {
        public required string PlayerUsername { get; set; }
    }

    public record StartTurnResult : NewTurnResult { }

    public record EndTurnResult : NewTurnResult {
        public bool EndDueToSkip { get; set; }
    }
}
