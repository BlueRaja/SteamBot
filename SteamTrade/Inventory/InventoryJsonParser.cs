using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace SteamTrade.Inventory
{
    public class InventoryJsonParser
    {
        public IEnumerable<InventoryItem> Parse(string inventoryJson)
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
            foreach (JProperty jDesc in descriptionJo["descriptions"])
            {
                if (desc == null)
                    desc = (string)((JObject)jDesc.Value)["value"];
                else
                    desc += "\n" + (string)((JObject)jDesc.Value)["value"];
            }
            item.Description = desc;
            List<InventoryTag> tags = new List<InventoryTag>();
            foreach (JProperty tag in descriptionJo["tags"])
            {
                JObject jTag = (JObject)tag.Value;
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