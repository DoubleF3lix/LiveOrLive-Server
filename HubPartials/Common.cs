using liveorlive_server.Enums;
using liveorlive_server.Models;
using liveorlive_server.Models.Results;
using Microsoft.AspNetCore.SignalR;

namespace liveorlive_server.HubPartials {
    // Wraps methods of a lobby to also handle outgoing packets to clients and game log messages.
    // The goal is to keep the hub logic and the game logic isolated, with this class providing methods that interface between the two.
    public partial class LiveOrLiveHub : Hub<IHubServerResponse> {
        /// <summary>
        /// Transfers the lobby host to another player. Handles informing clients of the old host and the new one.
        /// </summary>
        /// <param name="lobby">The lobby to change the host of.</param>
        /// <param name="newHost">The username of the new host.</param>
        /// <param name="reason">The reason for the change.</param>
        public async Task ChangeHost(Lobby lobby, string? newHost, string reason) {
            var oldHost = lobby.Host;
            lobby.Host = newHost;
            await Clients.Group(lobby.Id).HostChanged(oldHost, newHost, reason);
            await AddGameLogMessage(lobby, $"{newHost} is now the host. Reason: {reason}.");
        }

        /// <summary>
        /// Starts the game on the specified lobby.
        /// </summary>
        /// <param name="lobby">The lobby to start the game of.</param>
        private async Task StartGame(Lobby lobby) {
            var turnOrder = lobby.StartGame();
            await Clients.Group(lobby.Id).GameStarted(turnOrder);
            await AddGameLogMessage(lobby, $"The game has started with {lobby.Players.Count} players.");
        }

        /// <summary>
        /// Ends the game on the specified lobby.
        /// </summary>
        /// <param name="lobby">The lobby to end the game of.</param>
        private async Task EndGame(Lobby lobby) {
            var result = lobby.EndGame();
            await Clients.Group(lobby.Id).GameEnded(result.Winner);
            await AddGameLogMessage(lobby, result.Winner != null ? 
                $"The game has ended. The winner is {result.Winner}!" : 
                "The game has ended. No winner would be determined."
            );
        }

        /// <summary>
        /// Moves to the next turn on the specified lobby.
        /// </summary>
        /// <param name="lobby">The lobby to move to the next turn on.</param>
        private async Task NewTurn(Lobby lobby) {
            foreach (var resultLine in lobby.NewTurn()) {
                switch (resultLine) {
                    case StartTurnResult startTurnResult:
                        await Clients.Group(lobby.Id).TurnStarted(startTurnResult.PlayerUsername);
                        await AddGameLogMessage(lobby, $"It's {startTurnResult.PlayerUsername}'s turn.");
                        break;
                    case EndTurnResult endTurnResult:
                        await Clients.Group(lobby.Id).TurnEnded(endTurnResult.PlayerUsername);
                        if (endTurnResult.EndDueToSkip) {
                            await AddGameLogMessage(lobby, $"{endTurnResult.PlayerUsername} was skipped.");
                        }
                        await AddGameLogMessage(lobby, $"{endTurnResult.PlayerUsername}'s turn has ended.");
                        break;
                }
            }

            if (lobby.CurrentTurn != null && lobby.TryGetPlayerByUsername(lobby.CurrentTurn, out var currentTurnPlayer) && !currentTurnPlayer.InGame) {
                await ForfeitTurn(lobby, currentTurnPlayer);
            }
        }

        /// <summary>
        /// Starts a new round on the specified lobby.
        /// </summary>
        /// <param name="lobby">The lobby to start a new round on.</param>
        private async Task NewRound(Lobby lobby) {
            var result = lobby.NewRound();
            await Clients.Group(lobby.Id).NewRoundStarted(result);
            await AddGameLogMessage(lobby, $"A new round has started with {result.LiveRounds} live rounds and {result.BlankRounds} blanks.");
        }

        /// <summary>
        /// Called at the end of every action (shot taken, item used, etc.) to check for player eliminations or whether or not we need to switch rounds or end the game.
        /// </summary>
        /// <param name="lobby">The lobby to perform the end of action checks on.</param>
        /// <param name="isTurnEndingMove">Whether or not the action should end the players turn.</param>
        private async Task OnActionEnd(Lobby lobby, bool isTurnEndingMove) {
            // TODO elimination checking (take in acted on player)
            // Also check for sudden death enabling

            // If the game is over, we're done
            if (await EndGameConditional(lobby)) {
                return;
            }

            // Check for round end
            if (lobby.AmmoLeftInChamber <= 0) {
                await NewRound(lobby);
            }

            if (isTurnEndingMove) {
                await NewTurn(lobby);
            }

        }

        /// <summary>
        /// Ends the game if the conditions were met (1 or no non-spectator players left with more than 0 lives and in-game)
        /// </summary>
        /// <param name="lobby">The lobby to check the end game condition of.</param>
        /// <returns>Whether or not the conditions were met and the game was ended.</returns>
        private async Task<bool> EndGameConditional(Lobby lobby) {
            if (lobby.Players.Count(player => player.Lives > 0) <= 1) {
                await EndGame(lobby);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a game log message to the lobby.
        /// </summary>
        /// <param name="lobby">The lobby to add the game log message to.</param>
        /// <param name="message">The message to add to the game log.</param>
        private async Task AddGameLogMessage(Lobby lobby, string message) {
            var gameLogMessage = lobby.AddGameLogMessage(message);
            await Clients.Group(lobby.Id).GameLogUpdate(gameLogMessage);
        }

        /// <summary>
        /// Uses a reverse turn order item for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        /// <returns><c>true</c> if the item usage was a success, <c>false</c> if some precondition failed.</returns>
        private async Task<bool> UseReverseTurnOrderItemActual(Lobby lobby, Player user, Player? itemSource = null) {
            if (!lobby.Settings.EnableReverseTurnOrderItem) {
                await Clients.Caller.ActionFailed("Reverse Turn Order is not enabled!");
                return false;
            }

            itemSource ??= user;

            if (!lobby.RemoveItemFromPlayer(itemSource, Item.ReverseTurnOrder)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have a Reverse Turn Order item!" : $"{itemSource.Username} doesn't have a Reverse Turn Order item!");
                return false;
            }

            lobby.ReverseTurnOrder();
            await Clients.Group(lobby.Id).ReverseTurnOrderItemUsed(itemSource.Username);
            await AddGameLogMessage(lobby, $"{user.Username}{(user != itemSource ? $" stole an item from {itemSource.Username} and" : "")} reversed the turn order.");

            return true;
        }

        /// <summary>
        /// Uses a rack chamber item for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        /// <returns><c>true</c> if the item usage was a success, <c>false</c> if some precondition failed.</returns>
        private async Task<bool> UseRackChamberItemActual(Lobby lobby, Player user, Player? itemSource = null) {
            if (!lobby.Settings.EnableRackChamberItem) {
                await Clients.Caller.ActionFailed("Rack Chamber is not enabled!");
                return false;
            }

            itemSource ??= user;

            if (!lobby.RemoveItemFromPlayer(itemSource, Item.RackChamber)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have a Rack Chamber item!" : $"{itemSource.Username} doesn't have a Rack Chamber item!");
                return false;
            }

            var result = lobby.RackChamber();
            await Clients.Group(lobby.Id).RackChamberItemUsed(result.BulletType, itemSource.Username);
            await AddGameLogMessage(lobby, $"{user.Username}{(user != itemSource ? $" stole an item from {itemSource.Username} and" : "")} racked the chamber, ejecting a {(result.BulletType == BulletType.Live ? "live" : "blank")} round.");

            return true;
        }

        /// <summary>
        /// Uses an extra life item for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="target">The username of the player getting the extra life.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        /// <returns><c>true</c> if the item usage was a success, <c>false</c> if some precondition failed.</returns>
        private async Task<bool> UseExtraLifeItemActual(Lobby lobby, Player user, string target, Player? itemSource = null) {
            if (!lobby.Settings.EnableExtraLifeItem) {
                await Clients.Caller.ActionFailed("Extra Life is not enabled!");
                return false;
            }

            if (!lobby.TryGetPlayerByUsername(target, out var targetPlayer)) {
                await Clients.Caller.ActionFailed($"{target} isn't a valid player!");
                return false;
            }

            itemSource ??= user;

            if (!lobby.RemoveItemFromPlayer(itemSource, Item.ExtraLife)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have an Extra Life item!" : $"{itemSource.Username} doesn't have an Extra Life item!");
                return false;
            }

            lobby.GiveExtraLife(targetPlayer);
            await Clients.Group(lobby.Id).ExtraLifeItemUsed(target, itemSource.Username);
            await AddGameLogMessage(lobby, $"{user.Username}{(user != itemSource ? " stole an item and" : "")} gave {(user == targetPlayer ? "themselves" : targetPlayer.Username)} an extra life.");

            return true;
        }

        /// <summary>
        /// Uses a pickpocket item for the specified player. You can't steal a pickpocket item, so there is no <c>itemSource</c> like other items.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="stealTarget">The username of the player we're stealing an item from.</param>
        /// <param name="itemToSteal">The item we're stealing from <c>target</c>.</param>
        /// <param name="stolenItemTarget">The username of the player we're using the stolen item on, if applicable.</param>
        /// <returns><c>true</c> if the item usage was a success, <c>false</c> if some precondition failed.</returns>
        private async Task<bool> UsePickpocketItemActual(Lobby lobby, Player user, string stealTarget, Item itemToSteal, string? stolenItemTarget) {
            if (!lobby.Settings.EnablePickpocketItem) {
                await Clients.Caller.ActionFailed("Pickpocket is not enabled!");
                return false;
            }

            if (!lobby.TryGetPlayerByUsername(stealTarget, out var stealTargetPlayer)) {
                await Clients.Caller.ActionFailed($"{stealTarget} isn't a valid player!");
                return false;
            }

            // Don't remove yet, in case the child item was a failure
            if (!user.Items.Contains(Item.Pickpocket)) {
                await Clients.Caller.ActionFailed("You don't have a Pickpocket item!");
                return false;
            }

            if (itemToSteal == Item.Pickpocket) {
                await Clients.Caller.ActionFailed("You can't steal a Pickpocket item!");
                return false;
            }

            // Don't remove, in case usage fails
            if (!stealTargetPlayer.Items.Contains(itemToSteal)) {
                await Clients.Caller.ActionFailed($"{stealTargetPlayer.Username} doesn't have a {itemToSteal.ToFriendlyString()} item!");
                return false;
            }

            if (itemToSteal == Item.ExtraLife || itemToSteal == Item.Skip || itemToSteal == Item.Ricochet) {
                if (stolenItemTarget == null) {
                    await Clients.Caller.ActionFailed($"{itemToSteal.ToFriendlyString()} requires an item target!");
                    return false;
                }

                if (!lobby.TryGetPlayerByUsername(stolenItemTarget, out _)) {
                    await Clients.Caller.ActionFailed($"{stolenItemTarget} isn't a valid player!");
                    return false;
                }
            }

            bool stolenItemUseSuccess = itemToSteal switch {
                Item.ReverseTurnOrder => await UseReverseTurnOrderItemActual(lobby, user, stealTargetPlayer),
                Item.RackChamber => await UseRackChamberItemActual(lobby, user, stealTargetPlayer),
                Item.ExtraLife when stolenItemTarget != null => await UseExtraLifeItemActual(lobby, user, stolenItemTarget, stealTargetPlayer),
                Item.Pickpocket => throw new NotImplementedException(),
                Item.LifeGamble => await UseLifeGambleItemActual(lobby, user, stealTargetPlayer),
                Item.Invert => await UseInvertItemActual(lobby, user, stealTargetPlayer),
                Item.ChamberCheck => await UseChamberCheckItemActual(lobby, user, stealTargetPlayer),
                Item.DoubleDamage => await UseDoubleDamageItemActual(lobby, user, stealTargetPlayer),
                Item.Skip when stolenItemTarget != null => await UseSkipItemActual(lobby, user, stolenItemTarget, stealTargetPlayer),
                Item.Ricochet when stolenItemTarget != null => await UseRicochetItemActual(lobby, user, stolenItemTarget, stealTargetPlayer),
                _ => throw new NotImplementedException()
            };

            // Remove items only on success
            // The above calls should handle printing any success/error messages, so we're done
            if (stolenItemUseSuccess) {
                lobby.RemoveItemFromPlayer(user, Item.Pickpocket);
                lobby.RemoveItemFromPlayer(stealTargetPlayer, itemToSteal);
                await Clients.Group(lobby.Id).PickpocketItemUsed(stealTarget, itemToSteal, stolenItemTarget, user.Username);
            }

            return stolenItemUseSuccess;
        }

        /// <summary>
        /// Uses a life gamble item for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        /// <returns><c>true</c> if the item usage was a success, <c>false</c> if some precondition failed.</returns>
        private async Task<bool> UseLifeGambleItemActual(Lobby lobby, Player user, Player? itemSource = null) {
            if (!lobby.Settings.EnableLifeGambleItem) {
                await Clients.Caller.ActionFailed("Life Gamble is not enabled!");
                return false;
            }

            itemSource ??= user;

            if (!lobby.RemoveItemFromPlayer(itemSource, Item.LifeGamble)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have a Life Gamble item!" : $"{itemSource.Username} doesn't have a Life Gamble item!");
                return false;
            }

            var result = lobby.LifeGamble(user);
            await Clients.Group(lobby.Id).LifeGambleItemUsed(result.LifeChange, itemSource.Username);
            // Grammar is *still* important, kids
            await AddGameLogMessage(lobby, $"{user.Username} {(user != itemSource ? "stole" : "used")} a life gamble item{(user != itemSource ? $" from {itemSource.Username}" : "")} and {(result.LifeChange < 0 ? "lost" : "gained")} {Math.Abs(result.LifeChange)} {(Math.Abs(result.LifeChange) == 1 ? "life" : "lives")}.");

            // Need to check if we eliminated ourselves (whoops!)
            await OnActionEnd(lobby, false);

            return true;
        }

        /// <summary>
        /// Uses an invert item for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        /// <returns><c>true</c> if the item usage was a success, <c>false</c> if some precondition failed.</returns>
        private async Task<bool> UseInvertItemActual(Lobby lobby, Player user, Player? itemSource = null) {
            if (!lobby.Settings.EnableInvertItem) {
                await Clients.Caller.ActionFailed("Invert is not enabled!");
                return false;
            }

            itemSource ??= user;

            if (!lobby.RemoveItemFromPlayer(itemSource, Item.Invert)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have an Invert item!" : $"{itemSource.Username} doesn't have an Invert item!");
                return false;
            }

            lobby.InvertChamber();
            await Clients.Group(lobby.Id).InvertItemUsed(itemSource.Username);
            await AddGameLogMessage(lobby, $"{user.Username}{(user != itemSource ? $" stole an item from {itemSource.Username} and" : "")} inverted the chamber round.");

            return true;
        }

        /// <summary>
        /// Uses a chamber check item for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        /// <returns><c>true</c> if the item usage was a success, <c>false</c> if some precondition failed.</returns>
        private async Task<bool> UseChamberCheckItemActual(Lobby lobby, Player user, Player? itemSource = null) {
            if (!lobby.Settings.EnableChamberCheckItem) {
                await Clients.Caller.ActionFailed("Chamber Check is not enabled!");
                return false;
            }

            itemSource ??= user;

            if (!lobby.RemoveItemFromPlayer(itemSource, Item.ChamberCheck)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have a Chamber Check item!" : $"{itemSource.Username} doesn't have a Chamber Check item!");
                return false;
            }

            var result = lobby.PeekChamber();
            await Clients.Group(lobby.Id).ChamberCheckItemUsed(result.ChamberRoundType, itemSource.Username);
            await AddGameLogMessage(lobby, $"{user.Username}{(user != itemSource ? $" stole an item from {itemSource.Username} and" : "")} peeked the chamber. It's a {result.ChamberRoundType.ToString().ToLower()} round!");

            return true;
        }

        /// <summary>
        /// Uses a double damage item for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        /// <returns><c>true</c> if the item usage was a success, <c>false</c> if some precondition failed.</returns>
        private async Task<bool> UseDoubleDamageItemActual(Lobby lobby, Player user, Player? itemSource = null) {
            if (!lobby.Settings.EnableDoubleDamageItem) {
                await Clients.Caller.ActionFailed("Double Damage is not enabled!");
                return false;
            }

            itemSource ??= user;

            if (!itemSource.Items.Contains(Item.DoubleDamage)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have a Double Damage item!" : $"{itemSource.Username} doesn't have a Double Damage item!");
                return false;
            }

            if (lobby.DoubleDamageEnabled) {
                await Clients.Caller.ActionFailed("Double damage is already activated!");
                return false;
            }

            lobby.RemoveItemFromPlayer(itemSource, Item.DoubleDamage);
            lobby.EnableDoubleDamage();
            await Clients.Group(lobby.Id).DoubleDamageItemUsed(itemSource.Username);
            await AddGameLogMessage(lobby, $"{user.Username}{(user != itemSource ? $" stole an item from {itemSource.Username} and" : "")} activated double damage for the next shot.");

            return true;
        }

        /// <summary>
        /// Uses a skip item for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="target">The username of the player getting the extra life.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        /// <returns><c>true</c> if the item usage was a success, <c>false</c> if some precondition failed.</returns>
        private async Task<bool> UseSkipItemActual(Lobby lobby, Player user, string target, Player? itemSource = null) {
            if (!lobby.Settings.EnableSkipItem) {
                await Clients.Caller.ActionFailed("Skip is not enabled!");
                return false;
            }

            if (!lobby.TryGetPlayerByUsername(target, out var targetPlayer)) {
                await Clients.Caller.ActionFailed($"{target} isn't a valid player!");
                return false;
            }

            itemSource ??= user;

            if (!itemSource.Items.Contains(Item.Skip)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have a Skip item!" : $"{itemSource.Username} doesn't have a Skip item!");
                return false;
            }

            if (targetPlayer.IsSkipped) {
                await Clients.Caller.ActionFailed($"{targetPlayer.Username} is already skipped!");
                return false;
            }

            lobby.RemoveItemFromPlayer(itemSource, Item.Skip);
            lobby.SkipPlayer(targetPlayer); 
            await Clients.Group(lobby.Id).SkipItemUsed(target, itemSource.Username);
            await AddGameLogMessage(lobby, $"{user.Username}{(user != itemSource ? $" stole an item from {itemSource.Username} and" : "")} skipped {(user == targetPlayer ? "themselves" : targetPlayer.Username)}.");

            // Self-skipping
            if (user == targetPlayer) {
                await NewTurn(lobby);
            }

            return true;
        }

        /// <summary>
        /// Uses a ricochet for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="target">The username of the player getting the extra life.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        /// <returns><c>true</c> if the item usage was a success, <c>false</c> if some precondition failed.</returns>
        private async Task<bool> UseRicochetItemActual(Lobby lobby, Player user, string target, Player? itemSource = null) {
            if (!lobby.Settings.EnableRicochetItem) {
                await Clients.Caller.ActionFailed("Ricochet is not enabled!");
                return false;
            }

            if (!lobby.TryGetPlayerByUsername(target, out var targetPlayer)) {
                await Clients.Caller.ActionFailed($"{target} isn't a valid player!");
                return false;
            }

            itemSource ??= user;

            if (!lobby.RemoveItemFromPlayer(itemSource, Item.Ricochet)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have a Ricochet item!" : $"{itemSource.Username} doesn't have a Ricochet item!");
                return false;
            }

            // TODO don't stop them from using it if ricochets are anonymous
            if (targetPlayer.IsRicochet) {
                await Clients.Caller.ActionFailed($"{targetPlayer.Username} is already protected with ricochet!");
                return false;
            }

            lobby.RicochetPlayer(targetPlayer);
            await Clients.Group(lobby.Id).RicochetItemUsed(target, itemSource.Username);
            await AddGameLogMessage(lobby, $"{user.Username}{(user != itemSource ? $" stole an item from {itemSource.Username} and" : "")} protected {(user == targetPlayer ? "themselves" : targetPlayer.Username)} with ricochet.");

            return true;
        }

        private async Task ShootPlayerActual(Lobby lobby, Player shooter, Player target) {
            var result = lobby.ShootPlayer(shooter, target);

            // Be verbose about who shot who (even if it's themselves)
            await Clients.Group(lobby.Id).PlayerShotAt(target.Username, result.BulletFired, result.Damage);

            await AddGameLogMessage(lobby, $"{shooter.Username} shot {(result.ShotSelf ? "themselves" : target.Username)} with a {result.BulletFired.ToFriendlyString()} round{(result.BulletFired == BulletType.Live ? $" for {result.Damage} damage" : "")}.");
            // It's a turn ending action if it was not a blank round fired at themselves
            await OnActionEnd(lobby, !result.ShotSelf || result.BulletFired != BulletType.Blank);
        }

        private async Task ForfeitTurn(Lobby lobby, Player player) {
            // Make the player shoot themselves
            await ShootPlayerActual(lobby, player, player);
            await NewTurn(lobby);
        }
    }
}
