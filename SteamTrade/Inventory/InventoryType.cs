using System.Collections.Generic;
namespace SteamTrade.Inventory
{
    /// <summary>
    /// A single game may have multiple "types" of inventories.  This immutable class represents a specific inventory type for a specific game.
    /// </summary>
    public class InventoryType
    {
        public static readonly IEnumerable<InventoryType> InventoryTypes = new InventoryType[]{
                                                                BattleBlockTheater,
                                                                CSGO,
                                                                Dota2,
                                                                PathOfExile,
                                                                Portal2,
                                                                SinsOfADarkAge,
                                                                Steam_Gifts,
                                                                Steam_Coupons,
                                                                Steam_Community,
                                                                Steam_ItemRewards,
                                                                SuperMondayNightCombat_Endorsements,
                                                                SuperMondayNightCombat_Flair,
                                                                SuperMondayNightCombat_Misc,
                                                                SuperMondayNightCombat_Products,
                                                                SuperMondayNightCombat_Pros,
                                                                SuperMondayNightCombat_Taunts,
                                                                SuperMondayNightCombat_TreasureBalls,
                                                                SuperMondayNightCombat_Uniforms,
                                                                SuperMondayNightCombat_Weapons,
                                                                TeamFortress2,
                                                                Warframe
                                                            };
        public static readonly InventoryType BattleBlockTheater = new InventoryType(Game.BattleBlockTheater, 2);
        public static readonly InventoryType CSGO = new InventoryType(Game.CSGO, 2);
        public static readonly InventoryType Dota2 = new InventoryType(Game.Dota2, 2);
        public static readonly InventoryType PathOfExile = new InventoryType(Game.PathOfExile, 1);
        public static readonly InventoryType Portal2 = new InventoryType(Game.Portal2, 2);
        public static readonly InventoryType SinsOfADarkAge = new InventoryType(Game.SinsOfADarkAge, 1);
        /// <summary>
        /// Spiral Knights inventory.
        /// Because the "types" of inventories are custom to each user, the ContextId for this InventoryType.SpiralKnights is invalid.
        /// Additionally, it means loading a SpiralKnights potentially requires many more web requests than for other games.
        /// </summary>
        public static readonly InventoryType SpiralKnights = new InventoryType(Game.SpiralKnights, 0);
        /// <summary>
        /// Contains all tradable Steam games
        /// </summary>
        public static readonly InventoryType Steam_Gifts = new InventoryType(Game.Steam, 1);
        /// <summary>
        /// Contains all tradable Steam games
        /// </summary>
        public static readonly InventoryType Steam_Coupons = new InventoryType(Game.Steam, 3);
        /// <summary>
        /// Contains all cards, backgrounds, and emotes
        /// </summary>
        public static readonly InventoryType Steam_Community = new InventoryType(Game.Steam, 6);
        /// <summary>
        /// Contains "item rewards" for random non-inventory games, usually given out during Steam holiday events
        /// </summary>
        public static readonly InventoryType Steam_ItemRewards = new InventoryType(Game.Steam, 7);
        public static readonly InventoryType SuperMondayNightCombat_Products = new InventoryType(Game.SuperMondayNightCombat, 1);
        public static readonly InventoryType SuperMondayNightCombat_Uniforms = new InventoryType(Game.SuperMondayNightCombat, 2);
        public static readonly InventoryType SuperMondayNightCombat_Endorsements = new InventoryType(Game.SuperMondayNightCombat, 3);
        public static readonly InventoryType SuperMondayNightCombat_Pros = new InventoryType(Game.SuperMondayNightCombat, 5);
        public static readonly InventoryType SuperMondayNightCombat_Weapons = new InventoryType(Game.SuperMondayNightCombat, 7);
        public static readonly InventoryType SuperMondayNightCombat_Taunts = new InventoryType(Game.SuperMondayNightCombat, 10);
        public static readonly InventoryType SuperMondayNightCombat_Misc = new InventoryType(Game.SuperMondayNightCombat, 12);
        public static readonly InventoryType SuperMondayNightCombat_Flair = new InventoryType(Game.SuperMondayNightCombat, 13);
        public static readonly InventoryType SuperMondayNightCombat_TreasureBalls = new InventoryType(Game.SuperMondayNightCombat, 14);
        public static readonly InventoryType TeamFortress2 = new InventoryType(Game.TeamFortress2, 2);
        public static readonly InventoryType Warframe = new InventoryType(Game.Warframe, 2);

        /// <summary>
        /// A number representing the "subtype" of the inventory.  For instance, Steam coupons, Steam backgrounds/emotes/cards,
        /// and Steam gifts all have different context id's.
        /// </summary>
        public readonly long ContextId;

        /// <summary>
        /// The game which this type of inventory belongs to
        /// </summary>
        public readonly Game Game;

        public InventoryType(Game game, long contextId)
        {
            Game = game;
            ContextId = contextId;
        }
    }
}