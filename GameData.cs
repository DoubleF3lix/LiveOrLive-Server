using Newtonsoft.Json;
using System;

namespace liveorlive_server {
    public class GameData {
        public List<Player> players = [];
        public string? host = null;

        [JsonIgnore]
        public List<string> turnOrder = []; // Usernames
        public int turnCount = -1;
        [JsonIgnore]
        public bool gameStarted = false;
        [JsonIgnore]
        public ItemDeck itemDeck;

        private List<AmmoType> ammoDeck = [];

        [JsonIgnore]
        public Chat chat = new Chat();
        public readonly string gameID = Guid.NewGuid().ToString();

        public GameData() {
            this.itemDeck = new ItemDeck(this.players.Count);
        }

        public void startGame() {
            
        }

        public void newRound() {

        }
    }
}
