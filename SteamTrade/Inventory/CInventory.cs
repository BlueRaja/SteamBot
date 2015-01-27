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
    public class CInventory
    {
        public delegate void OnInventoryLoaded(CInventory inventory);

        public SteamID InventoryOwner { get; private set; }
        public InventoryType InventoryType { get; private set; }
        public IEnumerable<InventoryItem> Items { get; private set; }
        private bool invLoaded;

        private SteamWeb web;
        private FetchType fType;
        private ulong start;
        private bool callbackFired;

        public bool InventoryLoaded
        {
            get
            {
                return invLoaded && Items != null && Items.Count() > 0;
            }
        }

        public CInventory(SteamWeb web, SteamID owner, InventoryType type, FetchType fType = FetchType.Inventory, ulong iStart = 0)
        {
            this.web = web;
            InventoryOwner = owner;
            InventoryType = type;
            this.fType = fType;
            start = iStart;
        }

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
            string json = new InventoryJsonDownloader(web).GetInventoryJsonDynamic(InventoryOwner, InventoryType, start, fType);
            if (json == null)
                throw new InventoryFetchException(this.InventoryOwner);
            List<InventoryItem> items = new List<InventoryItem>();
            JObject inventoryJO = JObject.Parse(json);
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
                CInventory moreInv = new CInventory(web, InventoryOwner, InventoryType, fType, (ulong)inventoryJO["more_start"]);
                moreInv.FetchInventory();
                if (moreInv.InventoryLoaded)
                    items.AddRange(moreInv.Items);
            }
            invLoaded = true;
            this.Items = items;
        }

        private async Task ParseAsync(OnInventoryLoaded callback)
        {
            string json = await new InventoryJsonDownloader(web).GetInventoryJsonDynamicAsync(InventoryOwner, InventoryType, start, fType);
            if (json == null)
                goto Callback;
            List<InventoryItem> items = new List<InventoryItem>();
            JObject inventoryJO = null;
            inventoryJO = JObject.Parse(json);
            if (!(bool)inventoryJO["success"])
                goto Callback;
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
                CInventory moreInv = new CInventory(web, InventoryOwner, InventoryType, fType, (ulong)inventoryJO["more_start"]);
#pragma warning disable 4014
                moreInv.FetchInventoryAsync(this.MoreInvLoaded);
#pragma warning restore
                while (!callbackFired)
                    Thread.Yield();
                if (moreInv.InventoryLoaded)
                    items.AddRange(moreInv.Items);
            }
            invLoaded = true;
            this.Items = items;
            Callback:
            callback(this);
        }

        private void MoreInvLoaded(CInventory inventory)
        {
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

        public static CInventory FetchInventory(SteamWeb web, SteamID owner, InventoryType type, FetchType fType = FetchType.Inventory, ulong iStart = 0)
        {
            CInventory inv = new CInventory(web, owner, type, fType, iStart);
            inv.FetchInventory();
            return inv;
        }

        public static async Task FetchInventoryAsync(SteamWeb web, SteamID owner, InventoryType type, OnInventoryLoaded callback, FetchType fType = FetchType.Inventory, ulong iStart = 0)
        {
            await new CInventory(web, owner, type, fType, iStart).FetchInventoryAsync(callback);
        }

        public static IEnumerable<CInventory> FetchInventories(SteamWeb web, SteamID owner, IEnumerable<string> types, FetchType fType = FetchType.Inventory, ulong iStart = 0)
        {
            List<CInventory> inventories = new List<CInventory>();
            foreach (string type in types)
            {
                InventoryType parsedType = InventoryType.Parse(type);
                if (parsedType != null)
                {
                    CInventory iInv = new CInventory(web, owner, parsedType, fType, iStart);
                    iInv.FetchInventory();
                    if (iInv.InventoryLoaded)
                        inventories.Add(iInv);
                }
            }
            return inventories;
        }

        public static void FetchInventoriesAsync(SteamWeb web, SteamID owner, IEnumerable<string> types, OnInventoryLoaded callback, FetchType fType = FetchType.Inventory, ulong iStart = 0)
        {
            foreach (string type in types)
            {
                InventoryType parsedType = InventoryType.Parse(type);
                if (parsedType != null)
                {
                    CInventory iInv = new CInventory(web, owner, parsedType, fType, iStart);
#pragma warning disable 4014
                    iInv.FetchInventoryAsync(callback);
#pragma warning restore 4014
                }
            }
        }

        public static IEnumerable<CInventory> FetchInventories(SteamWeb web, SteamID owner, IEnumerable<InventoryType> types, FetchType fType = FetchType.Inventory, ulong iStart = 0)
        {
            List<CInventory> inventories = new List<CInventory>();
            foreach (InventoryType type in types)
            {
                CInventory iInv = new CInventory(web, owner, type, fType, iStart);
                iInv.FetchInventory();
                if (iInv.InventoryLoaded)
                    inventories.Add(iInv);
            }
            return inventories;
        }

        public static void FetchInventoriesAsync(SteamWeb web, SteamID owner, IEnumerable<InventoryType> types, OnInventoryLoaded callback, FetchType fType = FetchType.Inventory, ulong iStart = 0)
        {
            foreach (InventoryType type in types)
            {
                CInventory iInv = new CInventory(web, owner, type, fType, iStart);
#pragma warning disable 4014
                iInv.FetchInventoryAsync(callback);
#pragma warning restore 4014
            }
        }

        public static void FetchInventories(IEnumerable<CInventory> inventories)
        {
            foreach (CInventory inv in inventories)
                inv.FetchInventory();
        }

        public static void FetchInventoriesAsync(IEnumerable<CInventory> inventories, OnInventoryLoaded callback)
        {
            foreach (CInventory inv in inventories)
#pragma warning disable 4014
                inv.FetchInventoryAsync(callback);
#pragma warning restore 4014
        }
    }
}