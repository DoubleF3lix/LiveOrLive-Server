namespace liveorlive_server {
    public interface IPacket {
        public string PacketType { get; }
    }

    // Just to help classify stuff (Server comes from the server, client comes from client)
    public interface IServerPacket : IPacket { }
    public interface IClientPacket : IPacket { }

    // Server packets
    public record GameDataSyncPacket : IServerPacket {
        public string PacketType => "gameDataSync";
        public required GameData gameData;
    }

    public record PlayerJoinedPacket : IServerPacket {
        public string PacketType => "playerJoined";
        public required Player player;
    }

    public record PlayerJoinRejectedPacket : IServerPacket {
        public string PacketType => "playerJoinRejected";
        public required string reason;
    }

    public record PlayerLeftPacket : IServerPacket {
        public string PacketType => "playerLeft";
        public required string username;
    }

    public record HostSetPacket : IServerPacket {
        public string PacketType => "hostSet";
        public required string username;
    }

    public record GameStartedPacket : IServerPacket {
        public string PacketType => "gameStarted";
    }

    public record NewRoundStartedPacket : IServerPacket {
        public string PacketType => "newRoundStarted";
        public required List<Player> players;
        public required int liveCount;
        public required int blankCount;
    }

    public record TurnStartedPacket : IServerPacket {
        public string PacketType => "turnStarted";
        public required string username;
    }

    public record TurnEndedPacket : IServerPacket {
        public string PacketType => "turnEnded";
        public required string username;
    }

    public record ActionFailedPacket : IServerPacket {
        public string PacketType => "actionFailed";
        public required string reason;
    }

    public record PlayerShotAtPacket : IServerPacket {
        public string PacketType => "playerShotAt";
        public required string target;
        public required AmmoType ammoType;
        public required int damage; // Maybe derive ammoType from the damage (0 = blank?)
    }

    public record SkipItemUsedPacket : IServerPacket {
        public string PacketType => "skipItemUsed";
        public required string target;
    }

    public record DoubleDamageItemUsedPacket : IServerPacket {
        public string PacketType => "doubleDamageItemUsed";
    }

    public record CheckBulletItemUsedPacket : IServerPacket {
        public string PacketType => "checkBulletItemUsed"; 
    }

    public record CheckBulletItemResultPacket : IServerPacket {
        public string PacketType => "checkBulletItemResult";
        public required AmmoType result;
    }

    public record RebalancerItemUsedPacket : IServerPacket {
        public string PacketType => "rebalancerItemUsed";
        public required AmmoType ammoType;
        public required int count;
    }

    public record AdrenalineItemUsedPacket : IServerPacket {
        public string PacketType => "adrenalineItemUsed";
        public required int result;
    }

    public record AddLifeItemUsedPacket : IServerPacket {
        public string PacketType => "addLifeItemUsed";
    }

    public record QuickshotItemUsedPacket : IServerPacket {
        public string PacketType => "quickshotItemUsed";
    }

    public record StealItemUsedPacket : IServerPacket {
        public string PacketType => "stealItemUsed";
        public required string target;
        public required Item item;
    }

    public record NewChatMessageSentPacket : IServerPacket {
        public string PacketType => "newChatMessageSent";
        public required ChatMessage message;
    }

    public record ChatMessagesSyncPacket : IServerPacket {
        public string PacketType => "chatMessagesSync";
        public required List<ChatMessage> messages;
    }

    public record GameLogMessagesSyncPacket : IServerPacket {
        public string PacketType => "gameLogMessagesSync";
        public required List<GameLogMessage> messages;
    }

    public record ShowAlertPacket : IServerPacket {
        public string PacketType => "showAlert";
        public required string content;
    }

    public record NewGameLogMessageSentPacket : IServerPacket {
        public string PacketType => "newGameLogMessageSent";
        public required GameLogMessage message;
    }

    public record PlayerKickedPacket : IServerPacket {
        public string PacketType => "playerKicked";
        public required string username;
        public required string currentTurn;
    }

    // Client packets
    public record JoinGamePacket : IClientPacket {
        public string PacketType => "joinGame";
        public required string username;
    }

    public record SetHostPacket : IClientPacket {
        public string PacketType => "setHost";
        public required string username;
    }

    public record GameDataRequestPacket : IClientPacket {
        public string PacketType => "gameDataRequest";
    }

    public record StartGamePacket : IClientPacket {
        public string PacketType => "startGame";
    }

    public record ShootPlayerPacket : IClientPacket {
        public string PacketType => "shootPlayer";
        public required string target;
    }

    public record UseSkipItemPacket : IClientPacket {
        public string PacketType => "useSkipItem";
        public required string target;
    }

    public record UseDoubleDamageItemPacket : IClientPacket {
        public string PacketType => "useDoubleDamageItem";
    }

    public record UseCheckBulletItemPacket : IClientPacket {
        public string PacketType => "useCheckBulletItem";
    }

    public record UseRebalancerItemPacket : IClientPacket {
        public string PacketType => "useRebalancerItem";
        public required AmmoType ammoType;
    }

    public record UseAdrenalineItemPacket : IClientPacket {
        public string PacketType => "useAdrenalineItem";
    }

    public record UseAddLifeItemPacket : IClientPacket {
        public string PacketType => "useAddLifeItem";
    }

    public record UseQuickshotItemPacket : IClientPacket {
        public string PacketType => "useQuickshotItem";
    }

    public record UseStealItemPacket : IClientPacket {
        public string PacketType => "useStealItem";
        public required string target;
        public required Item item;
        public required AmmoType? ammoType;
        public required string? skipTarget;
    }

    public record SendNewChatMessagePacket : IClientPacket {
        public string PacketType => "sendNewChatMessage";
        public required string content;
    }

    public record ChatMessagesRequestPacket : IClientPacket {
        public string PacketType => "chatMessagesRequest";
    }

    public record GameLogMessagesRequestPacket : IClientPacket {
        public string PacketType => "gameLogMessagesRequest";
    }

    public record KickPlayerPacket : IClientPacket {
        public string PacketType => "kickPlayer";
        public required string username;
    }
}
