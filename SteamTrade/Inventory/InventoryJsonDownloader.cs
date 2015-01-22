using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using SteamKit2;

namespace SteamTrade.Inventory
{
    internal class InventoryJsonDownloader
    {
        private readonly SteamWeb _steamWeb;

        public InventoryJsonDownloader(SteamWeb steamWeb)
        {
            _steamWeb = steamWeb;
        }

        /// <summary>
        /// Returns the inventory/json response for the given user/inventoryType.
        /// Works even when the user has a private backback.  However, only works if user sent us a trade offer.
        /// </summary>
        public string GetTradeOfferInventoryJson(SteamID owner, InventoryType inventoryType)
        {
            string url = @"http://steamcommunity.com/tradeoffer/new/partnerinventory/";

            var data = new NameValueCollection
            {
                { "sessionid", _steamWeb.SessionId },
                { "partner", owner.ConvertToUInt64().ToString() },
                { "appid", inventoryType.Game.ToString() },
                { "contextid", inventoryType.ContextId.ToString() }
            };

            return _steamWeb.Fetch(url, "GET", data);
        }

        /// <summary>
        /// Returns the inventory/json response for the given user/inventoryType.
        /// Works even when the user has a private backback.  However, only works if we are currently trading with that user.
        /// </summary>
        public string GetTradeInventoryJson(SteamID owner, InventoryType inventoryType)
        {
            string url = String.Format(@"http://steamcommunity.com/trade/{0}/foreigninventory/", owner.ConvertToUInt64());

            var data = new NameValueCollection
            {
                { "sessionid", _steamWeb.SessionId },
                { "steamid", owner.ConvertToUInt64().ToString() },
                { "appid", inventoryType.Game.ToString() },
                { "contextid", inventoryType.ContextId.ToString() }
            };

            return _steamWeb.Fetch(url, "GET", data);
        }

        /// <summary>
        /// Returns the inventory/json response for the given user/inventoryType.
        /// Only works when the user's backpack is non-private.
        /// </summary>
        public string GetInventoryJson(SteamID owner, InventoryType inventoryType, ulong start)
        {
            string url = String.Format(@"http://steamcommunity.com/profiles/{0}/inventory/json/{1}/{2}/?start={3}",
                owner.ConvertToUInt64(), inventoryType.Game, inventoryType.ContextId, start);

            return _steamWeb.Fetch(url, "GET");
        }
    }
}
