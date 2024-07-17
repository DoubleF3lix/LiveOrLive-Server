using System;

namespace liveorlive_server {
    public class GameData {
        public List<Player> players = [];
        public Player? host = null;
        public Chat chat = new Chat();

        // -1 means game hasn't started
        public int turnCount = -1;

        public readonly string gameID = Guid.NewGuid().ToString();
    }
}
