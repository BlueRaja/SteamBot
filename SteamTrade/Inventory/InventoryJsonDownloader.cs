using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        /// Fetches a person's inventory json based on type needed.
        /// </summary>
        /// <param name="owner">SteamId of user to fetch inv for</param>
        /// <param name="type">The inventory type to fetch(game + contextid)</param>
        /// <param name="start"><remarks>Only used by GetInventoryJson()</remarks>Where to start fetching from.</param>
        /// <param name="fType">Type of fetch(Normal, Trade, TradeOffer)</param>
        /// <returns>null if invalid fType passed, json string if successful</returns>
        public string GetInventoryJsonDynamic(SteamID owner, InventoryType type, ulong start = 0, FetchType fType = FetchType.Inventory)
        {
            switch (fType)
            {
                case FetchType.Inventory:
                    return GetInventoryJson(owner, type, start);
                case FetchType.TradeInventory:
                    return GetTradeInventoryJson(owner, type);
                case FetchType.TradeOfferInventory:
                    return GetTradeOfferInventoryJson(owner, type);
                default:
                    return null;
            }
        }

        public async Task<string> GetInventoryJsonDynamicAsync(SteamID owner, InventoryType type, ulong start = 0, FetchType fType = FetchType.Inventory)
        {
            switch (fType)
            {
                case FetchType.Inventory:
                    return await GetInventoryJsonAsync(owner, type, start);
                case FetchType.TradeInventory:
                    return await GetTradeInventoryJsonAsync(owner, type);
                case FetchType.TradeOfferInventory:
                    return await GetTradeOfferInventoryJsonAsync(owner, type);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns the inventory/json response for the given user/inventoryType.
        /// Works even when the user has a private backback.  However, only works if user sent us a trade offer.
        /// </summary>
        private string GetTradeOfferInventoryJson(SteamID owner, InventoryType inventoryType)
        {
            string url = @"http://steamcommunity.com/tradeoffer/new/partnerinventory/";

            var data = new NameValueCollection
            {
                { "sessionid", _steamWeb.SessionId },
                { "partner", owner.ConvertToUInt64().ToString() },
                { "appid", ((ulong)inventoryType.Game).ToString() },
                { "contextid", inventoryType.ContextId.ToString() }
            };

            return _steamWeb.Fetch(url, "GET", data);
        }

        /// <summary>
        /// Returns the inventory/json response for the given user/inventoryType.
        /// Works even when the user has a private backback.  However, only works if user sent us a trade offer.
        /// </summary>
        private async Task<string> GetTradeOfferInventoryJsonAsync(SteamID owner, InventoryType inventoryType)
        {
            string url = @"http://steamcommunity.com/tradeoffer/new/partnerinventory/";

            var data = new NameValueCollection
            {
                { "sessionid", _steamWeb.SessionId },
                { "partner", owner.ConvertToUInt64().ToString() },
                { "appid", ((ulong)inventoryType.Game).ToString() },
                { "contextid", inventoryType.ContextId.ToString() }
            };

            return await _steamWeb.FetchAsync(url, "GET", data);
        }

        /// <summary>
        /// Returns the inventory/json response for the given user/inventoryType.
        /// Works even when the user has a private backback.  However, only works if we are currently trading with that user.
        /// </summary>
        private string GetTradeInventoryJson(SteamID owner, InventoryType inventoryType)
        {
            string url = String.Format(@"http://steamcommunity.com/trade/{0}/foreigninventory/", owner.ConvertToUInt64());

            var data = new NameValueCollection
            {
                { "sessionid", _steamWeb.SessionId },
                { "steamid", owner.ConvertToUInt64().ToString() },
                { "appid", ((ulong)inventoryType.Game).ToString() },
                { "contextid", inventoryType.ContextId.ToString() }
            };

            return _steamWeb.Fetch(url, "GET", data);
        }

        /// <summary>
        /// Returns the inventory/json response for the given user/inventoryType.
        /// Works even when the user has a private backback.  However, only works if we are currently trading with that user.
        /// </summary>
        private async Task<string> GetTradeInventoryJsonAsync(SteamID owner, InventoryType inventoryType)
        {
            string url = String.Format(@"http://steamcommunity.com/trade/{0}/foreigninventory/", owner.ConvertToUInt64());

            var data = new NameValueCollection
            {
                { "sessionid", _steamWeb.SessionId },
                { "steamid", owner.ConvertToUInt64().ToString() },
                { "appid", ((ulong)inventoryType.Game).ToString() },
                { "contextid", inventoryType.ContextId.ToString() }
            };

            return await _steamWeb.FetchAsync(url, "GET", data);
        }

        /// <summary>
        /// Returns the inventory/json response for the given user/inventoryType.
        /// Only works when the user's backpack is non-private.
        /// </summary>
        private string GetInventoryJson(SteamID owner, InventoryType inventoryType, ulong start)
        {
            string url = String.Format(@"http://steamcommunity.com/profiles/{0}/inventory/json/{1}/{2}/?start={3}",
                owner.ConvertToUInt64(), (ulong)inventoryType.Game, inventoryType.ContextId, start);
            return _steamWeb.Fetch(url, "GET");
        }

        /// <summary>
        /// Returns the inventory/json response for the given user/inventoryType.
        /// Only works when the user's backpack is non-private.
        /// </summary>
        private async Task<string> GetInventoryJsonAsync(SteamID owner, InventoryType inventoryType, ulong start)
        {
            string url = String.Format(@"http://steamcommunity.com/profiles/{0}/inventory/json/{1}/{2}/?start={3}",
                owner.ConvertToUInt64(), (ulong)inventoryType.Game, inventoryType.ContextId, start);
            return await _steamWeb.FetchAsync(url, "GET");
        }
    }
}
