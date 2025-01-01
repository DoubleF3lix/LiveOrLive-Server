namespace liveorlive_server {
    public class TurnOrderManager {
        public string currentTurn {
            get {
                if (this.currentTurnIndex < 0) {
                    return "";
                }
                return this.turnOrder[this.currentTurnIndex];
            }
        }

        private List<string> turnOrder = []; // Usernames
        private int currentTurnIndex = -1;

        public void Populate(List<Player> players) {
            this.turnOrder = players.Where(player => player.inGame == true).Select(player => player.username).ToList();
        }

        public void Advance() {
            this.currentTurnIndex = (this.currentTurnIndex + 1) % this.turnOrder.Count;
        }

        public void EliminatePlayer(string username) {
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
