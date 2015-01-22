using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamTrade.Inventory
{
    /// <summary>
    /// Represents the "AppId", or game to which the inventory belongs.
    /// Note that a game may have more than one type of Inventory, eg. Steam has Gifts, Coupons, and Community
    /// </summary>
    public enum Game : ulong
    {
        BattleBlockTheater = 238460,
        CSGO = 730,
        Dota2 = 570,
        PathOfExile = 238960,
        Portal2 = 620,
        SinsOfADarkAge = 251970,
        SpiralKnights = 99900,
        Steam = 753,
        SuperMondayNightCombat = 104700,
        TeamFortress2 = 440,
        Warframe = 230410,
    }
}
