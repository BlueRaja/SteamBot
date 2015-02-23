using System;
using System.Collections.Generic;
using System.Linq;
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

        private SteamWeb web;
        private FetchType fType;
        private ulong start;

        public bool InventoryLoaded
        {
            get
            {
                return Items != null && Items.Any();
            }
        }

        public Inventory(SteamWeb web, SteamID owner, InventoryType type, FetchType fType = FetchType.Inventory, ulong iStart = 0)
        {
            this.web = web;
            InventoryOwner = owner;
            InventoryType = type;
            this.fType = fType;
            start = iStart;
        }

        /// <summary>
        /// Fetch inventory synchronously.
        /// </summary>
        public void FetchInventory()
        {
            FetchInventoryAsync(delegate(Inventory inventory) { }).Wait();
        }

        /// <summary>
        /// Fetch inventory asynchronously.
        /// </summary>
        /// <param name="callback">Method to fire once inventory is loaded alright.</param>
        public async Task FetchInventoryAsync(OnInventoryLoaded callback)
        {
            await ParseInventoryAsync(callback);
        }

        private async Task ParseInventoryAsync(OnInventoryLoaded callback)
        {
            if (callback == null)
                throw new ArgumentNullException("Callback MUST NOT BE NULL! You might of intended an anonymous method.");
            string invJson = await new InventoryJsonDownloader(web).FetchInventoryAsync(InventoryOwner, InventoryType, start, fType);
            if (String.IsNullOrWhiteSpace(invJson))
                throw new InventoryFetchException(InventoryOwner);
            JObject invJO = JObject.Parse(invJson);
            if (invJO == null)
                throw new Exception("Unable to parse inventory");
            if (!(bool)invJO["success"])
                throw new InventoryFetchException(InventoryOwner);
            List<InventoryItem> items = new List<InventoryItem>();
            foreach (JProperty itemProperty in invJO["rgInventory"])
            {
                JObject itemJO = itemProperty.Value.ToObject<JObject>();
                string descriptionName = itemJO["classid"] + "_" + itemJO["instanceid"];
                JObject descriptionJO = invJO["rgDescriptions"][descriptionName].ToObject<JObject>();
                InventoryItem item = GenerateItemFromJson(itemJO, descriptionJO);
                items.Add(item);
            }
            if ((bool)invJO["more"])
            {
                //Inventory has more then 2500 items.
                //Note: No more items are added IF callback isn't fired. Hence why this is asynced.
                await new Inventory(web, InventoryOwner, InventoryType, fType, (int)invJO["more_start"]).FetchInventoryAsync(delegate(Inventory inventory)
                {
                    if (inventory.InventoryLoaded)
                        items.AddRange(inventory.Items);
                });
            }
            this.Items = items;
            callback(this);
        }

        private IEnumerable<InventoryItem> Parse(string inventoryJson)
        {
            List<InventoryItem> items = new List<InventoryItem>();
            JObject inventoryJO = JObject.Parse(inventoryJson);

            foreach(JProperty itemProperty in inventoryJO["rgInventory"])
            {
                JObject itemJO = (JObject) itemProperty.Value;
                string descriptionName = itemJO["classid"] + "_" + itemJO["instanceid"];
                JObject descriptionJO = (JObject) inventoryJO["rgDescriptions"][descriptionName];

                InventoryItem item = GenerateItemFromJson(itemJO, descriptionJO);
                items.Add(item);
            }

            return items;
        }

        private InventoryItem GenerateItemFromJson(JObject itemJo, JObject descriptionJo)
        {
            InventoryItem item = new InventoryItem();
            item.InventoryType = InventoryType;
            item.Id = (ulong)itemJo["id"];
            item.InventoryPosition = (int)itemJo["pos"];
            item.BackgroundColor = (string) descriptionJo["background_color"];
            item.IconUrl = @"http://steamcommunity-a.akamaihd.net/economy/image/" + (string) descriptionJo["icon_url"];
            item.IconUrlLarge = @"http://steamcommunity-a.akamaihd.net/economy/image/" + (string)descriptionJo["icon_url_large"];
            item.OriginalName = (string) descriptionJo["market_name"];
            item.IsMarketable = (bool) descriptionJo["marketable"];
            item.DisplayName = (string) descriptionJo["name"];
            item.NameColor = (string) descriptionJo["name_color"];
            item.IsTradable = (bool) descriptionJo["tradable"];
            item.Type = (string)descriptionJo["type"];
            string desc = null;
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
            item.Description = desc == null ? "" : desc;
            IEnumerable<InventoryTag> tags = null;
            if (descriptionJo["tags"] != null)
            {
                List<InventoryTag> pTags = new List<InventoryTag>();
                foreach (JObject jTag in descriptionJo["tags"])
                {
                    InventoryTag nTag = new InventoryTag();
                    nTag.InternalName = (string)jTag["internal_name"];
                    nTag.Name = (string)jTag["name"];
                    nTag.Category = (string)jTag["category"];
                    nTag.CategoryName = (string)jTag["category_name"];
                    nTag.Color = (string)jTag["color"];
                    pTags.Add(nTag);
                }
                tags = pTags;
            }
            item.Tags = tags;
            return item;
        }

        /// <summary>
        /// Gets an item from assetId
        /// </summary>
        /// <param name="assetId">unique id of item</param>
        /// <returns>Instance of item if found.</returns>
        public InventoryItem GetItem(ulong assetId)
        {
            if (!InventoryLoaded)
                return null;
            foreach (var item in Items)
            {
                if (item.Id == assetId)
                    return item;
            }
            return null;
        }

        #region Static Methods
        /// <summary>
        /// Instantiates then begins fetching an inventory.
        /// </summary>
        /// <param name="web">Instance of a valid steamweb.</param>
        /// <param name="owner">SteamID of user to fetch inventory of.</param>
        /// <param name="type">InventoryType instance of game and contextid to fetch.</param>
        /// <param name="fType">Should we fetch in a regular way? Trade way? Trade offer way?</param>
        /// <param name="iStart"><remarks>Only used by the regular fetch way.</remarks>Where to begin list from.</param>
        /// <returns>Instance of inventory if fetched properly. Null otherwise.</returns>
        public static Inventory FetchInventory(SteamWeb web, SteamID owner, InventoryType type, FetchType fType = FetchType.Inventory, ulong iStart = 0)
        {
            Inventory ret = null;
            var sFITask = FetchInventoryAsync(web, owner, type, delegate(Inventory inventory) { if (inventory.InventoryLoaded) ret = inventory; }, fType, iStart);
            sFITask.Wait();
            return ret;
        }

        /// <summary>
        /// Asynchronous fetching of inventory. See synchronous version of this method for args not explained here.
        /// </summary>
        /// <remarks>Not marked async due to warning associated with no awaits in async marked methods.</remarks>
        /// <param name="callback">Method for inventory to fire if fetched properly.</param>
        public static async Task FetchInventoryAsync(SteamWeb web, SteamID owner, InventoryType type, OnInventoryLoaded callback, FetchType fType = FetchType.Inventory, ulong iStart = 0)
        {
            await new Inventory(web, owner, type, fType, iStart).FetchInventoryAsync(callback);
        }

        /// <summary>
        /// Given a list of inventory instances, begin fetching them.
        /// </summary>
        /// <param name="inventories">List of inventories.</param>
        public static void FetchInventories(IEnumerable<Inventory> inventories)
        {
            foreach (Inventory inv in inventories)
                inv.FetchInventory();
        }

        /// <summary>
        /// Asynchronous fetching of a list of inventories. See synchronous version of this method for args not explained here.
        /// </summary>
        /// <param name="callback">Method for inventory to fire if fetched properly.</param>
        public static void FetchInventoriesAsync(IEnumerable<Inventory> inventories, OnInventoryLoaded callback)
        {
            foreach (Inventory inv in inventories)
            {
                Task task = inv.FetchInventoryAsync(callback);
                if (task.Status == TaskStatus.Created)
                    task.Start();
            }
        }

        /// <summary>
        /// Given a list of inventory types, instantiate and fetch inventories.
        /// </summary>
        /// <param name="web">Instance of a valid steamweb.</param>
        /// <param name="owner">SteamID of user to fetch inventory of.</param>
        /// <param name="types">List of InventoryType instances(Game + contextid).</param>
        /// <param name="fType">Should we fetch in a regular way? Trade way? Trade offer way?</param>
        /// <param name="iStart"><remarks>Only used by the regular fetch way.</remarks>Where to begin list from.</param>
        /// <returns>A list of properly fetched inventories.</returns>
        public static IEnumerable<Inventory> FetchInventories(SteamWeb web, SteamID owner, IEnumerable<InventoryType> types, FetchType fType = FetchType.Inventory, ulong iStart = 0)
        {
            List<Inventory> ret = new List<Inventory>();
            Task.Factory.StartNew(() =>
                {
                    InventoryType lastType = types.Last();
                    bool lastInvLoaded = false;
                    FetchInventoriesAsync(web, owner, types, delegate(Inventory inventory)
                    {
                        if (inventory.InventoryLoaded)
                            ret.Add(inventory);
                        if (inventory.InventoryType == lastType)
                            lastInvLoaded = true;
                    }, fType, iStart);
                    while (!lastInvLoaded)
                        Thread.Yield();
                }).Wait(240000);
            return ret;
        }

        /// <summary>
        /// Given a list of inventory types, instantiate and ansynchronously fetch inventories. See synchronous version of this method for args not explained here.
        /// </summary>
        /// <remarks>Not marked async due to warning associated with no awaits in async marked methods.</remarks>
        /// <param name="callback">Method for inventory to fire if fetched properly.</param>
        public static void FetchInventoriesAsync(SteamWeb web, SteamID owner, IEnumerable<InventoryType> types, OnInventoryLoaded callback, FetchType fType = FetchType.Inventory, ulong iStart = 0)
        {
            foreach (InventoryType type in types)
            {
                Task task = new Inventory(web, owner, type, fType, iStart).FetchInventoryAsync(callback);
                if (task.Status == TaskStatus.Created)
                    task.Start();
            }
        }
        #endregion
    }
}
