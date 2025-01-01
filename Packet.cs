namespace liveorlive_server {
    public interface IPacket {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Lowercase for consistency with JSON")]
        public string packetType { get; }
    }

    // Just to help classify stuff (Server comes from the server, client comes from client)
    public interface IServerPacket : IPacket { }
    public interface IClientPacket : IPacket { }

    // Server packets
    public record GameDataSyncPacket : IServerPacket {
        public string packetType => "gameDataSync";
        public required GameData gameData;
    }

    public record PlayerJoinedPacket : IServerPacket {
        public string packetType => "playerJoined";
        public required Player player;
    }

    public record PlayerJoinRejectedPacket : IServerPacket {
        public string packetType => "playerJoinRejected";
        public required string reason;
    }

    public record PlayerLeftPacket : IServerPacket {
        public string packetType => "playerLeft";
        public required string username;
    }

    public record HostSetPacket : IServerPacket {
        public string packetType => "hostSet";
        public required string username;
    }

    public record GameStartedPacket : IServerPacket {
        public string packetType => "gameStarted";
    }

    public record NewRoundStartedPacket : IServerPacket {
        public string packetType => "newRoundStarted";
        public required List<Player> players;
        public required int liveCount;
        public required int blankCount;
    }

    public record TurnStartedPacket : IServerPacket {
        public string packetType => "turnStarted";
        public required string username;
    }

    public record TurnEndedPacket : IServerPacket {
        public string packetType => "turnEnded";
        public required string username;
    }

    public record ActionFailedPacket : IServerPacket {
        public string packetType => "actionFailed";
        public required string reason;
    }

    public record PlayerShotAtPacket : IServerPacket {
        public string packetType => "playerShotAt";
        public required string target;
        public required AmmoType ammoType;
        public required int damage; // Maybe derive ammoType from the damage (0 = blank?)
    }

    public record SkipItemUsedPacket : IServerPacket {
        public string packetType => "skipItemUsed";
        public required string target;
    }

    public record DoubleDamageItemUsedPacket : IServerPacket {
        public string packetType => "doubleDamageItemUsed";
    }

    public record CheckBulletItemUsedPacket : IServerPacket {
        public string packetType => "checkBulletItemUsed"; 
    }

    public record CheckBulletItemResultPacket : IServerPacket {
        public string packetType => "checkBulletItemResult";
        public required AmmoType result;
    }

    public record RebalancerItemUsedPacket : IServerPacket {
        public string packetType => "rebalancerItemUsed";
        public required AmmoType ammoType;
        public required int count;
    }

    public record AdrenalineItemUsedPacket : IServerPacket {
        public string packetType => "adrenalineItemUsed";
        public required int result;
    }

    public record AddLifeItemUsedPacket : IServerPacket {
        public string packetType => "addLifeItemUsed";
    }

    public record QuickshotItemUsedPacket : IServerPacket {
        public string packetType => "quickshotItemUsed";
    }

    public record StealItemUsedPacket : IServerPacket {
        public string packetType => "stealItemUsed";
        public required string target;
        public required Item item;
    }

    public record NewChatMessageSentPacket : IServerPacket {
        public string packetType => "newChatMessageSent";
        public required ChatMessage message;
    }

    public record ChatMessagesSyncPacket : IServerPacket {
        public string packetType => "chatMessagesSync";
        public required List<ChatMessage> messages;
    }

    public record GameLogMessagesSyncPacket : IServerPacket {
        public string packetType => "gameLogMessagesSync";
        public required List<GameLogMessage> messages;
    }

    public record ShowAlertPacket : IServerPacket {
        public string packetType => "showAlert";
        public required string content;
    }

    public record NewGameLogMessageSentPacket : IServerPacket {
        public string packetType => "newGameLogMessageSent";
        public required GameLogMessage message;
    }

    public record PlayerKickedPacket : IServerPacket {
        public string packetType => "playerKicked";
        public required string username;
        public required string currentTurn;
    }

    // Client packets
    public record JoinGamePacket : IClientPacket {
        public string packetType => "joinGame";
        public required string username;
    }

    public record SetHostPacket : IClientPacket {
        public string packetType => "setHost";
        public required string username;
    }

    public record GameDataRequestPacket : IClientPacket {
        public string packetType => "gameDataRequest";
    }

    public record StartGamePacket : IClientPacket {
        public string packetType => "startGame";
    }

    public record ShootPlayerPacket : IClientPacket {
        public string packetType => "shootPlayer";
        public required string target;
    }

    public record UseSkipItemPacket : IClientPacket {
        public string packetType => "useSkipItem";
        public required string target;
    }

    public record UseDoubleDamageItemPacket : IClientPacket {
        public string packetType => "useDoubleDamageItem";
    }

    public record UseCheckBulletItemPacket : IClientPacket {
        public string packetType => "useCheckBulletItem";
    }

    public record UseRebalancerItemPacket : IClientPacket {
        public string packetType => "useRebalancerItem";
        public required AmmoType ammoType;
    }

    public record UseAdrenalineItemPacket : IClientPacket {
        public string packetType => "useAdrenalineItem";
    }

    public record UseAddLifeItemPacket : IClientPacket {
        public string packetType => "useAddLifeItem";
    }

    public record UseQuickshotItemPacket : IClientPacket {
        public string packetType => "useQuickshotItem";
    }

    public record UseStealItemPacket : IClientPacket {
        public string packetType => "useStealItem";
        public required string target;
        public required Item item;
        public required AmmoType? ammoType;
        public required string? skipTarget;
    }

    public record SendNewChatMessagePacket : IClientPacket {
        public string packetType => "sendNewChatMessage";
        public required string content;
    }

    public record ChatMessagesRequestPacket : IClientPacket {
        public string packetType => "chatMessagesRequest";
    }

    public record GameLogMessagesRequestPacket : IClientPacket {
        public string packetType => "gameLogMessagesRequest";
    }

    public record KickPlayerPacket : IClientPacket {
        public string packetType => "kickPlayer";
        public required string username;
    }
}
