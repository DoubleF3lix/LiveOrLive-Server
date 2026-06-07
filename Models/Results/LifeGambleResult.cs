namespace LiveOrLiveServer.Models.Results {
    public record LifeGambleResult {
        public int LifeChange { get; set; }
        public bool Dead { get; set; }
        public bool Eliminated { get; set; }
    }
}
