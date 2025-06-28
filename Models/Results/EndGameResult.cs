namespace liveorlive_server.Models.Results {
    public class EndGameResult {
        /// <summary>
        /// <c>null</c> if there was no winner
        /// </summary>
        public string? Winner { get; set; }
        public required List<string> PurgedPlayers { get; set; }
    }
}
