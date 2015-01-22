using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamTrade.Inventory
{
    public class ItemInventory
    {
        //Todo: Make this also asyncable to hopefully prevent trade from being blocked till inventory is loaded.

        public bool inventoryLoaded { get; private set; }

        public IEnumerable<InventoryItem> items { get; private set; }

        private InventoryType invType { get; set; }

        private SteamWeb SteamWeb { get; set; }

        public ItemInventory(SteamWeb web, InventoryType type)
        {
            SteamWeb = web;
            invType = type;
        }

        public void FetchInventory(SteamKit2.SteamID owner, FetchType type)
        {
        }

        public static ItemInventory FetchInventory(SteamWeb web, SteamKit2.SteamID owner, InventoryType invType, FetchType type)
        {
            ItemInventory ret = new ItemInventory(web, invType);
            ret.FetchInventory(owner, type);
            return ret;
        }
    }
}
