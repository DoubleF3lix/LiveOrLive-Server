using liveorlive_server.Enums;
using liveorlive_server.Models.Results;
using Microsoft.AspNetCore.SignalR;

namespace liveorlive_server.HubPartials {
    // Wraps methods of a lobby to also handle outgoing packets to clients and game log messages.
    // The goal is to keep the hub logic and the game logic isolated, with this class providing methods that interface between the two.
    public partial class LiveOrLiveHub : Hub<IHubServerResponse> {
        /// <summary>
        /// Starts the game on the specified lobby.
        /// </summary>
        /// <param name="lobby">The lobby to start the game of.</param>
        private async Task StartGame(Lobby lobby) {
            var turnOrder = lobby.StartGame();
            await Clients.Group(lobby.Id).GameStarted(turnOrder);
            await AddGameLogMessage(lobby, $"The game has started with {lobby.Players.Count(p => !p.IsSpectator)} players.");
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
            // If the game is over, we're done
            if (await EndGameConditional(lobby)) {
                return;
            }

            if (isTurnEndingMove) {
                await NewTurn(lobby);
            }

            // Check for round end
            if (lobby.AmmoLeftInChamber <= 0) {
                await NewRound(lobby);
            }
        }

        /// <summary>
        /// Ends the game if the conditions were met (1 or no non-spectator players left with more than 0 lives and in-game)
        /// </summary>
        /// <param name="lobby">The lobby to check the end game condition of.</param>
        /// <returns>Whether or not the conditions were met and the game was ended.</returns>
        private async Task<bool> EndGameConditional(Lobby lobby) {
            if (lobby.Players.Count(player => player.InGame && !player.IsSpectator && player.Lives > 0) <= 1) {
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
        private async Task UseReverseTurnOrderItemActual(Lobby lobby, Player user, Player? itemSource = null) {
            if (!lobby.Settings.EnableReverseTurnOrderItem) {
                await Clients.Caller.ActionFailed("Reverse Turn Order is not enabled!");
                return;
            }

            itemSource ??= user;

            if (!itemSource.Items.Remove(Item.ReverseTurnOrder)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have a Reverse Turn Order item!" : $"{itemSource.Username} doesn't have a Reverse Turn Order item!");
                return;
            }

            lobby.ReverseTurnOrder();
            await Clients.Group(lobby.Id).ReverseTurnOrderItemUsed();
            await AddGameLogMessage(lobby, $"{user.Username} reversed the turn order.");
        }

        /// <summary>
        /// Uses a rack chamber item for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        private async Task UseRackChamberItemActual(Lobby lobby, Player user, Player? itemSource = null) {
            if (!lobby.Settings.EnableRackChamberItem) {
                await Clients.Caller.ActionFailed("Rack Chamber is not enabled!");
                return;
            }

            itemSource ??= user;

            if (!itemSource.Items.Remove(Item.RackChamber)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have a Rack Chamber item!" : $"{itemSource.Username} doesn't have a Rack Chamber item!");
                return;
            }

            var result = lobby.RackChamber();
            await Clients.Group(lobby.Id).RackChamberItemUsed(result.BulletType);
            await AddGameLogMessage(lobby, $"{user.Username} racked the chamber and ejected a {(result.BulletType == BulletType.Live ? "live" : "blank")} round.");
        }

        /// <summary>
        /// Uses an extra life item for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="target">The username of the player getting the extra life.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        private async Task UseExtraLifeItemActual(Lobby lobby, Player user, string target, Player? itemSource = null) {
            if (!lobby.Settings.EnableExtraLifeItem) {
                await Clients.Caller.ActionFailed("Extra Life is not enabled!");
                return;
            }

            if (!lobby.TryGetPlayerByUsername(target, out var targetPlayer)) {
                await Clients.Caller.ActionFailed($"{target} isn't a valid player!");
                return;
            }

            itemSource ??= user;

            if (!itemSource.Items.Remove(Item.ExtraLife)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have an Extra Life item!" : $"{itemSource.Username} doesn't have an Extra Life item!");
                return;
            }

            lobby.GiveExtraLife(targetPlayer);
            await Clients.Group(lobby.Id).ExtraLifeItemUsed(target);
            await AddGameLogMessage(lobby, $"{user.Username} used an extra life item and gave {(user == targetPlayer ? "themselves" : targetPlayer.Username)} an extra life.");
        }

        /// <summary>
        /// Uses a pickpocket item for the specified player. You can't steal a pickpocket item, so there is no <c>itemSource</c> like other items.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="stealTarget">The username of the player we're stealing an item from.</param>
        /// <param name="itemToSteal">The item we're stealing from <c>target</c>.</param>
        /// <param name="stolenItemTarget">The username of the player we're using the stolen item on, if applicable.</param>
        private async Task UsePickpocketItemActual(Lobby lobby, Player user, string stealTarget, Item itemToSteal, string? stolenItemTarget) {
            if (!lobby.Settings.EnablePickpocketItem) {
                await Clients.Caller.ActionFailed("Pickpocket is not enabled!");
                return;
            }

            if (!lobby.TryGetPlayerByUsername(stealTarget, out var stealTargetPlayer)) {
                await Clients.Caller.ActionFailed($"{stealTarget} isn't a valid player!");
                return;
            }

            if (!user.Items.Remove(Item.Pickpocket)) {
                await Clients.Caller.ActionFailed("You don't have a Pickpocket item!");
                return;
            }

            if (itemToSteal == Item.Pickpocket) {
                await Clients.Caller.ActionFailed("You can't steal a Pickpocket item!");
                return;
            }

            if (!stealTargetPlayer.Items.Remove(itemToSteal)) {
                await Clients.Caller.ActionFailed($"{stealTargetPlayer.Username} doesn't have a {itemToSteal} item!");
                return;
            }

            if (itemToSteal == Item.ExtraLife || itemToSteal == Item.Skip || itemToSteal == Item.Ricochet) {
                if (stolenItemTarget == null) {
                    await Clients.Caller.ActionFailed($"{itemToSteal} requires an item target!");
                    return;
                }

                if (!lobby.TryGetPlayerByUsername(stolenItemTarget, out var _)) {
                    await Clients.Caller.ActionFailed($"{stolenItemTarget} isn't a valid player!");
                    return;
                }
            }

            // Tell everyone first because it should follow chronology (X stole an item -> X used item)
            await Clients.Group(lobby.Id).PickpocketItemUsed(stealTarget, itemToSteal, stolenItemTarget);
            // Grammar is important kids (7:54 PM)
            // Grammar is important, kids* (8:14 PM)
            await AddGameLogMessage(lobby, $"{user.Username} stole a{(itemToSteal == Item.ExtraLife || itemToSteal == Item.Invert ? "n" : "")} {itemToSteal.ToString().ToLower()} item from {stealTargetPlayer.Username}.");

            // TODO need to check that item usage worked correctly... perhaps make each one return true if success
            // Also need to not take pickpocket item if the sub-item use failed
            switch (itemToSteal) {
                case Item.ReverseTurnOrder:
                    await UseReverseTurnOrderItemActual(lobby, user, stealTargetPlayer);
                    break;
                case Item.RackChamber:
                    await UseRackChamberItemActual(lobby, user, stealTargetPlayer);
                    break;
                case Item.ExtraLife when stolenItemTarget != null:
                    await UseExtraLifeItemActual(lobby, user, stolenItemTarget, stealTargetPlayer);
                    break;
                case Item.LifeGamble:
                    await UseLifeGambleItemActual(lobby, user, stealTargetPlayer);
                    break;
                case Item.Invert:
                    await UseInvertItemActual(lobby, user, stealTargetPlayer);
                    break;
                case Item.ChamberCheck:
                    await UseChamberCheckItemActual(lobby, user, stealTargetPlayer);
                    break;
                case Item.DoubleDamage:
                    await UseDoubleDamageItemActual(lobby, user, stealTargetPlayer);
                    break;
                case Item.Skip when stolenItemTarget != null:
                    await UseSkipItemActual(lobby, user, stolenItemTarget, stealTargetPlayer);
                    break;
                case Item.Ricochet when stolenItemTarget != null:
                    await UseRicochetItemActual(lobby, user, stolenItemTarget, stealTargetPlayer);
                    break;
            }
        }

        /// <summary>
        /// Uses a life gamble item for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        private async Task UseLifeGambleItemActual(Lobby lobby, Player user, Player? itemSource = null) {
            if (!lobby.Settings.EnableLifeGambleItem) {
                await Clients.Caller.ActionFailed("Life Gamble is not enabled!");
                return;
            }

            itemSource ??= user;

            if (!itemSource.Items.Remove(Item.LifeGamble)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have a Life Gamble item!" : $"{itemSource.Username} doesn't have a Life Gamble item!");
                return;
            }

            var result = lobby.LifeGamble(user);
            await Clients.Group(lobby.Id).LifeGambleItemUsed(result.LifeChange);
            // Grammar is *still* important, kids
            await AddGameLogMessage(lobby, $"{user.Username} used a life gamble item and {(result.LifeChange < 0 ? "lost" : "gained")} {Math.Abs(result.LifeChange)} {(result.LifeChange == 1 ? "life" : "lives")}.");
        }

        /// <summary>
        /// Uses an invert item for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        private async Task UseInvertItemActual(Lobby lobby, Player user, Player? itemSource = null) {
            if (!lobby.Settings.EnableInvertItem) {
                await Clients.Caller.ActionFailed("Invert is not enabled!");
                return;
            }

            itemSource ??= user;

            if (!itemSource.Items.Remove(Item.Invert)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have an Invert item!" : $"{itemSource.Username} doesn't have an Invert item!");
                return;
            }

            lobby.InvertChamber();
            await Clients.Group(lobby.Id).InvertItemUsed();
            await AddGameLogMessage(lobby, $"{user.Username} inverted the chamber round.");
        }

        /// <summary>
        /// Uses a chamber check item for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        private async Task UseChamberCheckItemActual(Lobby lobby, Player user, Player? itemSource = null) {
            if (!lobby.Settings.EnableChamberCheckItem) {
                await Clients.Caller.ActionFailed("Chamber Check is not enabled!");
                return;
            }

            itemSource ??= user;

            if (!itemSource.Items.Remove(Item.ChamberCheck)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have a Chamber Check item!" : $"{itemSource.Username} doesn't have a Chamber Check item!");
                return;
            }

            var result = lobby.PeekChamber();
            await Clients.Group(lobby.Id).ChamberCheckItemUsed(result.ChamberRoundType);
            await AddGameLogMessage(lobby, $"{user.Username} peeked the chamber. It's a {result.ChamberRoundType.ToString().ToLower()} round!");
        }

        /// <summary>
        /// Uses a double damage item for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        private async Task UseDoubleDamageItemActual(Lobby lobby, Player user, Player? itemSource = null) {
            if (!lobby.Settings.EnableDoubleDamageItem) {
                await Clients.Caller.ActionFailed("Double Damage is not enabled!");
                return;
            }

            itemSource ??= user;

            if (!itemSource.Items.Remove(Item.ChamberCheck)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have a Double Damage item!" : $"{itemSource.Username} doesn't have a Double Damage item!");
                return;
            }

            if (lobby.DoubleDamageEnabled) {
                await Clients.Caller.ActionFailed("Double damage is already activated!");
                return;
            }

            lobby.EnableDoubleDamage();
            await Clients.Group(lobby.Id).DoubleDamageItemUsed();
            await AddGameLogMessage(lobby, $"{user.Username} activated double damage for the next shot.");
        }

        /// <summary>
        /// Uses a skip item for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="target">The username of the player getting the extra life.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        private async Task UseSkipItemActual(Lobby lobby, Player user, string target, Player? itemSource = null) {
            if (!lobby.Settings.EnableSkipItem) {
                await Clients.Caller.ActionFailed("Skip is not enabled!");
                return;
            }

            if (!lobby.TryGetPlayerByUsername(target, out var targetPlayer)) {
                await Clients.Caller.ActionFailed($"{target} isn't a valid player!");
                return;
            }

            itemSource ??= user;

            if (!itemSource.Items.Remove(Item.Skip)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have a Skip item!" : $"{itemSource.Username} doesn't have a Skip item!");
                return;
            }

            if (targetPlayer.IsSkipped) {
                await Clients.Caller.ActionFailed($"{targetPlayer.Username} is already skipped!");
                return;
            }

            lobby.SkipPlayer(targetPlayer);
            await Clients.Group(lobby.Id).SkipItemUsed(target);
            await AddGameLogMessage(lobby, $"{user.Username} skipped {(user == targetPlayer ? "themselves" : targetPlayer.Username)}.");
        }

        /// <summary>
        /// Uses a ricochet for the specified player.
        /// </summary>
        /// <param name="lobby">The lobby the player belongs to.</param>
        /// <param name="user">The player using the item.</param>
        /// <param name="target">The username of the player getting the extra life.</param>
        /// <param name="itemSource">The player we should take the item from. Set to <c>player</c> if <c>null</c>.</param>
        private async Task UseRicochetItemActual(Lobby lobby, Player user, string target, Player? itemSource = null) {
            if (!lobby.Settings.EnableRicochetItem) {
                await Clients.Caller.ActionFailed("Ricochet is not enabled!");
                return;
            }

            if (!lobby.TryGetPlayerByUsername(target, out var targetPlayer)) {
                await Clients.Caller.ActionFailed($"{target} isn't a valid player!");
                return;
            }

            itemSource ??= user;

            if (!itemSource.Items.Remove(Item.Ricochet)) {
                await Clients.Caller.ActionFailed(user == itemSource ? "You don't have a Ricochet item!" : $"{itemSource.Username} doesn't have a Ricochet item!");
                return;
            }

            // TODO don't stop them from using it if ricochets are anonymous
            if (targetPlayer.IsRicochet) {
                await Clients.Caller.ActionFailed($"{targetPlayer.Username} is already protected with ricochet!");
                return;
            }

            lobby.SkipPlayer(targetPlayer);
            await Clients.Group(lobby.Id).RicochetItemUsed(target);
            await AddGameLogMessage(lobby, $"{user.Username} protected {(user == targetPlayer ? "themselves" : targetPlayer.Username)} with ricochet.");
        }
    }
}
