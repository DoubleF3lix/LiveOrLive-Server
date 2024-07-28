namespace liveorlive_server {
    public interface Packet {
        public string packetType { get; }
    }

    // Just to help classify stuff (Server comes from the server, client comes from client)
    public interface ServerPacket : Packet { }
    public interface ClientPacket : Packet { }

    // Server packets
    public record GameDataSyncPacket : ServerPacket {
        public string packetType => "gameDataSync";
        public required GameData gameData;
    }

    public record PlayerJoinedPacket : ServerPacket {
        public string packetType => "playerJoined";
        public required Player player;
    }

    public record PlayerJoinRejectedPacket : ServerPacket {
        public string packetType => "playerJoinRejected";
        public required string reason;
    }

    public record HostSetPacket : ServerPacket {
        public string packetType => "hostSet";
        public required string username;
    }

    public record GameStartedPacket : ServerPacket {
        public string packetType => "gameStarted";
    }

    public record NewRoundStartedPacket : ServerPacket {
        public string packetType => "newRoundStarted";
        public required List<Player> players;
        public required int liveCount;
        public required int blankCount;
    }

    public record TurnStartedPacket : ServerPacket {
        public string packetType => "turnStarted";
        public required string username;
    }

    public record TurnEndedPacket : ServerPacket {
        public string packetType => "turnEnded";
        public required string username;
    }

    public record ActionFailedPacket : ServerPacket {
        public string packetType => "actionFailed";
        public required string reason;
    }

    public record UseChamberCheckItemResultPacket : ServerPacket {
        public string packetType => "useChamberCheckItemResult";
        public required AmmoType ammoType;
    }

    public record NewChatMessageSentPacket : ServerPacket {
        public string packetType => "newChatMessageSent";
        public required ChatMessage message;
    }

    public record ChatMessagesSyncPacket : ServerPacket {
        public string packetType => "chatMessagesSync";
        public required List<ChatMessage> messages;
    }

    public record ShowAlertPacket : ServerPacket {
        public string packetType => "showAlert";
        public required string content;
    }

    // Client packets
    public record JoinGamePacket : ClientPacket {
        public string packetType => "joinGame";
        public required string username;
    }

    public record SetHostPacket : ClientPacket {
        public string packetType => "setHost";
        public required string username;
    }

    public record GameDataRequestPacket : ClientPacket {
        public string packetType => "gameDataRequest";
    }

    public record StartGamePacket : ClientPacket {
        public string packetType => "startGame";
    }

    public record ShootPlayerPacket : ClientPacket {
        public string packetType => "shootPlayer";
        public required string target;
    }

    public record UseSkipItemPacket : ClientPacket {
        public string packetType => "useSkipItem";
        public required string target;
    }

    public record UseDoubleDamageItemPacket : ClientPacket {
        public string packetType => "useDoubleDamageItem";
    }

    public record UseChamberCheckItemPacket : ClientPacket {
        public string packetType => "useChamberCheckItem";
    }

    public record UseRebalancerItemPacket : ClientPacket {
        public string packetType => "useRebalancerItem";
        public required AmmoType ammoType;
    }

    public record UseQuickshotItemPacket : ClientPacket {
        public string packetType => "useQuickshotItem";
    }

    public record UseStealItemPacket : ClientPacket {
        public string packetType => "useStealItem";
        public required string target;
        public required Item item;
    }

    public record SendNewChatMessagePacket : ClientPacket {
        public string packetType => "sendNewChatMessage";
        public required string content;
    }

    public record ChatMessagesRequestPacket : ClientPacket {
        public string packetType => "chatMessagesRequest";
    }
}
