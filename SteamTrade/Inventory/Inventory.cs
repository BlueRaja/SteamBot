using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SteamKit2;
using SteamTrade.Exceptions;

namespace SteamTrade.Inventory
{
    public class Inventory
    {
        public delegate void OnInventoryLoaded(Inventory inventory);

        public SteamID InventoryOwner { get; private set; }
        public InventoryType InventoryType { get; private set; }
        public IEnumerable<InventoryItem> Items { get; private set; }
        public bool InventoryLoaded { get; private set; }

        private SteamWeb web;
        private FetchType fType;
        private ulong start;
        private bool callbackFired;

        public Inventory(SteamWeb web, SteamID owner, InventoryType type, FetchType fType = FetchType.Inventory, ulong iStart = 0)
        {
            this.web = web;
            InventoryOwner = owner;
            InventoryType = type;
            this.fType = fType;
            start = iStart;
        }

        public static Inventory FetchInventory(SteamWeb web, SteamID owner, InventoryType type, FetchType fType = FetchType.Inventory)
        {
            Inventory inv = new Inventory(web, owner, type, fType);
            inv.FetchInventory();
            return inv;
        }

        public static void FetchInventories(IEnumerable<Inventory> inventories)
        {
            foreach (Inventory inv in inventories)
                inv.FetchInventory();
        }

        public static IEnumerable<Inventory> FetchInventories(SteamWeb web, SteamID owner, IEnumerable<InventoryType> invTypes, FetchType fType = FetchType.Inventory)
        {
            List<Inventory> invs = new List<Inventory>();
            foreach (InventoryType type in invTypes)
                invs.Add(Inventory.FetchInventory(web, owner, type, fType));
            return invs;
        }

#pragma warning disable 4014
        public static void QuickFetchInventory(Inventory inventory, OnInventoryLoaded callback)
        {
            inventory.FetchInventoryAsync(callback);
        }

        public static void QuickFetchInventory(SteamWeb web, SteamID owner, OnInventoryLoaded callback, InventoryType type, FetchType fType = FetchType.Inventory)
        {
            new Inventory(web, owner, type, fType).FetchInventoryAsync(callback);
        }

        public static void QuickFetchInventories(IEnumerable<Inventory> inventories, OnInventoryLoaded callback)
        {
            foreach (Inventory inv in inventories)
                inv.FetchInventoryAsync(callback);
        }

        public static void QuickFetchInventories(SteamWeb web, SteamID owner, OnInventoryLoaded callback, IEnumerable<InventoryType> invTypes, FetchType fType = FetchType.Inventory)
        {
            foreach (InventoryType type in invTypes)
                new Inventory(web, owner, type, fType).FetchInventoryAsync(callback);
        }
#pragma warning restore 4014

        public void FetchInventory()
        {
            Parse();
        }

        public async Task FetchInventoryAsync(OnInventoryLoaded callback)
        {
            await ParseAsync(callback);
        }

        private void Parse()
        {
            Console.WriteLine(fType);
            InventoryJsonDownloader downloader = new InventoryJsonDownloader(web);
            string json = null;
            switch (fType)
            {
                case FetchType.Inventory:
                    json = downloader.GetInventoryJson(InventoryOwner, InventoryType, start);
                    break;
                case FetchType.TradeInventory:
                    json = downloader.GetTradeInventoryJson(InventoryOwner, InventoryType);
                    break;
                case FetchType.TradeOfferInventory:
                    json = downloader.GetTradeOfferInventoryJson(InventoryOwner, InventoryType);
                    break;
            }
            List<InventoryItem> items = new List<InventoryItem>();
            JObject inventoryJO = null;
            try
            {
                inventoryJO = JObject.Parse(json);
            }
            catch (Exception)
            {
                return;
            }
            if (!(bool)inventoryJO["success"])
                throw new InventoryFetchException(this.InventoryOwner);
            foreach (JProperty itemProperty in inventoryJO["rgInventory"])
            {
                JObject itemJO = (JObject)itemProperty.Value;
                string descriptionName = itemJO["classid"] + "_" + itemJO["instanceid"];
                JObject descriptionJO = (JObject)inventoryJO["rgDescriptions"][descriptionName];
                InventoryItem item = GenerateItemFromJson(itemJO, descriptionJO);
                items.Add(item);
            }
            if ((bool)inventoryJO["more"])
            {
                Inventory moreInv = new Inventory(web, InventoryOwner, InventoryType, fType, (ulong)inventoryJO["more_start"]);
                moreInv.FetchInventory();
                while (!callbackFired)
                    Thread.Yield();
                if (moreInv.InventoryLoaded)
                    items.AddRange(moreInv.Items);
            }
            InventoryLoaded = true;
            this.Items = items;
        }

        private async Task ParseAsync(OnInventoryLoaded callback)
        {
            InventoryJsonDownloader downloader = new InventoryJsonDownloader(web);
            string json = null;
            switch (fType)
            {
                case FetchType.Inventory:
                    json = await downloader.GetInventoryJsonAsync(InventoryOwner, InventoryType, start);
                    break;
                case FetchType.TradeInventory:
                    json = await downloader.GetTradeInventoryJsonAsync(InventoryOwner, InventoryType);
                    break;
                case FetchType.TradeOfferInventory:
                    json = await downloader.GetTradeOfferInventoryJsonAsync(InventoryOwner, InventoryType);
                    break;
            }
            List<InventoryItem> items = new List<InventoryItem>();
            JObject inventoryJO = null;
            try
            {
                inventoryJO = JObject.Parse(json);
            }
            catch (Exception)
            {
                return;
            }
            if (!(bool)inventoryJO["success"])
                throw new InventoryFetchException(this.InventoryOwner);
            foreach (JProperty itemProperty in inventoryJO["rgInventory"])
            {
                JObject itemJO = (JObject) itemProperty.Value;
                string descriptionName = itemJO["classid"] + "_" + itemJO["instanceid"];
                JObject descriptionJO = (JObject) inventoryJO["rgDescriptions"][descriptionName];
                InventoryItem item = GenerateItemFromJson(itemJO, descriptionJO);
                items.Add(item);
            }
            if ((bool)inventoryJO["more"])
            {
                Inventory moreInv = new Inventory(web, InventoryOwner, InventoryType, fType, (ulong)inventoryJO["more_start"]);
#pragma warning disable 4014
                moreInv.FetchInventoryAsync(this.MoreInvLoaded);
#pragma warning restore
                while (!callbackFired)
                    Thread.Yield();
                if (moreInv.InventoryLoaded)
                    items.AddRange(moreInv.Items);
            }
            InventoryLoaded = true;
            this.Items = items;
            callback(this);
        }

        private void MoreInvLoaded(Inventory inventory)
        {
            Console.WriteLine("Loaded " + inventory.Items.Count() + " more items.");
            this.callbackFired = true;
        }

        private InventoryItem GenerateItemFromJson(JObject itemJo, JObject descriptionJo)
        {
            InventoryItem item = new InventoryItem();
            item.InventoryType = InventoryType;
            item.Id = (ulong) itemJo["id"];
            item.InventoryPosition = (int) itemJo["pos"];

            item.BackgroundColor = (string) descriptionJo["background_color"];
            item.IconUrl = @"http://steamcommunity-a.akamaihd.net/economy/image/" + (string) descriptionJo["icon_url"];
            item.IconUrlLarge = @"http://steamcommunity-a.akamaihd.net/economy/image/" + (string)descriptionJo["icon_url_large"];
            item.OriginalName = (string) descriptionJo["market_name"];
            item.IsMarketable = (bool) descriptionJo["marketable"];
            item.DisplayName = (string) descriptionJo["name"];
            item.NameColor = (string) descriptionJo["name_color"];
            item.IsTradable = (bool) descriptionJo["tradable"];
            item.Type = (string) descriptionJo["type"];
            item.Amount = itemJo["amount"] != null ? (ulong)itemJo["amount"] : 1;
            string desc = "";
            if (descriptionJo["descriptions"].Type == JTokenType.Array)
            {
                foreach (JObject descJO in descriptionJo["descriptions"])
                {
                    if (desc == null)
                        desc = (string)descJO["value"];
                    else
                        desc += "\n" + (string)descJO["value"];
                }
            }
            item.Description = desc;
            List<InventoryTag> tags = new List<InventoryTag>();
            foreach (JObject jTag in descriptionJo["tags"])
            {
                InventoryTag nTag = new InventoryTag();
                nTag.InternalName = (string)jTag["internal_name"];
                nTag.Name = (string)jTag["name"];
                nTag.Category = (string)jTag["category"];
                nTag.CategoryName = (string)jTag["category_name"];
                nTag.Color = (string)jTag["color"];
                tags.Add(nTag);
            }
            item.Tags = tags.ToArray();
            return item;
        }

        public InventoryItem GetItem(ulong assetId)
        {
            foreach (var item in Items)
            {
                if (item.Id == assetId)
                    return item;
            }
            return null;
        }
    }
}