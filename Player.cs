﻿using liveorlive_server.Enums;
using System.Text.Json.Serialization;
using Tapper;

namespace liveorlive_server {
    [TranspilationSource]
    public class Player(Settings config, string username, string? connectionId, bool isSpectator = false) {
        public string Username { get; set; } = username;
        [JsonIgnore]
        public string? connectionId = connectionId;

        // Needed to keep track of players who have left without kicking them for disconnects
        public bool InGame { get; set; } = true;
        public bool IsSpectator { get; set; } = isSpectator;

        public int Lives { get; set; } = config.DefaultLives;
        public List<Item> Items { get; set; } = new(config.MaxItems);
        public bool IsSkipped { get; set; } = false;
        public bool IsRicochet { get; set; } = false;

        public readonly long joinTime = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;

        public override bool Equals(object? obj) {
            return obj is Player other && this.Equals(other);
        }

        public bool Equals(Player player) {
            return this.Username == player.Username;
        }
    }
}
