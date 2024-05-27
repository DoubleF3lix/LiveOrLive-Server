namespace backend_server {
    public interface Packet {
        public string packetType { get; }
    }

    // Just to help classify stuff (Server comes from the server, client comes from client)
    public interface ServerPacket : Packet { }
    public interface ClientPacket : Packet { }

    // Server packets
    public record UpdatePlayerDataPacket : ServerPacket {
        public string packetType => "updatePlayerData";
        public required Player player;
        public List<Item>? items;
        public int? lives;
    }

    public record ShowAlertPacket : ServerPacket {
        public string packetType => "showAlert";
        public required string content;
    }

    public record NewChatMessageSentPacket : ServerPacket {
        public string packetType => "newChatMessageSent";
        public required ChatMessage message;
    }

    public record PlayerJoinRejectedPacket : ServerPacket {
        public string packetType => "playerJoinRejected";
        public required string reason;
    }

    public record PlayerJoinedPacket : ServerPacket {
        public string packetType => "playerJoined";
        public required Player player;
    }

    public record SetHostPacket : ServerPacket {
        public string packetType => "setHost";
    }

    public record GetGameInfoResponsePacket : ServerPacket {
        public string packetType => "getGameInfoResponse";
        public required Player currentHost;
        public required List<Player> players;
        public required List<ChatMessage> chatMessages;
        public required int turnCount;
    }

    public record GunFiredPacket : ServerPacket {
        public string packetType => "gunFired";
        public required Player target;
    }

    public record ItemUsedPacket : ServerPacket {
        public string packetType => "itemUsed";
        public required Item itemID;
        public Player? target;
    }

    public record GameStartedPacket : ServerPacket {
        public string packetType => "gameStarted";
    }

    // Client packets
    public record SendNewChatMessagePacket : ClientPacket {
        public string packetType => "sendNewChatMessage";
        public required string content;
    }

    public record JoinGamePacket : ClientPacket {
        public string packetType => "joinGame";
        public required string username;
    }

    public record GetGameInfoPacket : ClientPacket {
        public string packetType => "getGameInfo";
    }

    public record FireGunPacket : ClientPacket {
        public string packetType => "fireGun";
        public required Player target;
    }

    public record UseItemPacket : ClientPacket {
        public string packetType => "useItem";
        public required Item item;
        public Player? target;
    }

    public record StartGamePacket : ClientPacket {
        public string packetType => "startGame";
    }
}
