namespace liveorlive_server {
    public class TurnOrderManager {
        public string? currentTurn {
            get {
                if (this.currentTurnIndex < 0) {
                    return null;
                }
                return this.turnOrder[this.currentTurnIndex];
            }
        }

        private List<string> turnOrder = []; // Usernames
        private int currentTurnIndex = -1;

        public void populate(List<Player> players) {
            this.turnOrder = players.Where(player => player.inGame == true).Select(player => player.username).ToList();
        }

        // TODO this is throwing divide by zero errors
        public void advance() {
            this.currentTurnIndex = (this.currentTurnIndex + 1) % this.turnOrder.Count;
        }

        public void eliminatePlayer(string username) {
            int index = this.turnOrder.IndexOf(username);
            if (index != -1) {
                this.turnOrder.RemoveAt(index);
                if (index < this.currentTurnIndex) {
                    this.currentTurnIndex--;
                } else if (index == this.currentTurnIndex) {
                    // If the current player is eliminated, adjust to the next player's turn
                    this.currentTurnIndex %= this.turnOrder.Count;
                }
            }
        }
    }
}
