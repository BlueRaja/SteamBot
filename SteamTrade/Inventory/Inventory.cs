using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SteamKit2;

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

        public static void FetchInventories(IEnumerable<Inventory> inventories, OnInventoryLoaded callback)
        {
            foreach (Inventory inv in inventories)
                inv.FetchInventory(callback);
        }

        public static void FetchInventories(SteamWeb web, SteamID owner, OnInventoryLoaded callback, IEnumerable<InventoryType> invTypes, FetchType fType = FetchType.Inventory)
        {
            foreach (InventoryType type in invTypes)
                new Inventory(web, owner, type, fType).FetchInventory(callback);
        }

        public async void FetchInventory(OnInventoryLoaded callback)
        {
            await Parse(callback);
        }

#pragma warning disable 4014, 1998
        private async Task Parse(OnInventoryLoaded callback)
        {
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
            JObject inventoryJO = JObject.Parse(json);
            if (!(bool)inventoryJO["success"])
                goto FireCallback;
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
                moreInv.Parse(this.MoreInvLoaded);
                while (!callbackFired)
                    Thread.Yield();
                if (moreInv.InventoryLoaded)
                    items.AddRange(moreInv.Items);
            }
            InventoryLoaded = true;
            this.Items = items;
            FireCallback:
            callback(this);
        }
#pragma warning restore
        private void MoreInvLoaded(Inventory inventory)
        {
            Console.WriteLine("Loaded " + inventory.Items.Count() + " more items.");
            this.callbackFired = true;
        }

        private InventoryItem GenerateItemFromJson(JObject itemJo, JObject descriptionJo)
        {
            InventoryItem item = new InventoryItem();
            item.InventoryType = InventoryType;
            item.Id = (long) itemJo["id"];
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
    }
}