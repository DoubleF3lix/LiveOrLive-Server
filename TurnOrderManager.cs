namespace liveorlive_server {
    public class TurnOrderManager(List<Player>? players) {
        private readonly List<Player> players = players ?? [];
        private int currentTurnIndex = -1;

        public List<string> TurnOrder => this.players.Where(p => !p.IsSpectator).Select(p => p.Username).ToList();
        public string? CurrentTurn {
            get {
                if (this.currentTurnIndex < 0) {
                    return null;
                }

                return this.TurnOrder[this.currentTurnIndex];
            }
        }

        public void Advance() {
            // Just in case so we don't lock the server down
            if (players.All(p => p.Lives <= 0)) {
                currentTurnIndex = -1;
                return;
            }

            Player? currentPlayerForTurn;
            do {
                this.currentTurnIndex = (this.currentTurnIndex + 1) % this.players.Count;
                currentPlayerForTurn = this.players[currentTurnIndex];
            } while (currentPlayerForTurn.Lives <= 0);
        }
    }
}
