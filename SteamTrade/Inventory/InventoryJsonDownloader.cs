using System;
using System.Collections.Specialized;
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
        /// Chooses and runs an inventory json downloader method based on fType.
        /// </summary>
        public async Task<string> FetchInventoryAsync(SteamID owner, InventoryType type, ulong start = 0, FetchType fType = FetchType.Inventory)
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
                    throw new ArgumentException("Invalid fetch type passed.");
            }
        }

        /// <summary>
        /// Returns the inventory/json response for the given user/inventoryType.
        /// Works even when the user has a private backback.  However, only works if user sent us a trade offer.
        /// </summary>
        public async Task<string> GetTradeOfferInventoryJsonAsync(SteamID owner, InventoryType inventoryType)
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
        public async Task<string> GetTradeInventoryJsonAsync(SteamID owner, InventoryType inventoryType)
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
        public async Task<string> GetInventoryJsonAsync(SteamID owner, InventoryType type, ulong start = 0)
        {
            string url = String.Format(@"http://{0}/profiles/{1}/inventory/json/{2}/{3}/", SteamWeb.SteamCommunityDomain, owner.ConvertToUInt64(), (ulong)type.Game, type.ContextId);
            return await _steamWeb.FetchAsync(url, "GET", new NameValueCollection{ { "start", start.ToString() } });
        }
    }
}
