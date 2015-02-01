﻿using System.Collections.Generic;

namespace SteamTrade.Inventory
{
    public class InventoryItem
    {
        public InventoryType InventoryType { get; internal set; }
        public string DisplayName { get; internal set; }
        public string OriginalName { get; internal set; }
        public bool IsTradable { get; internal set; }
        public bool IsMarketable { get; internal set; }
        public string IconUrl { get; internal set; }
        public string IconUrlLarge { get; internal set; }
        public string Type { get; internal set; }
        public ulong Id { get; internal set; }
        public int InventoryPosition { get; internal set; }
        public string NameColor { get; internal set; }
        public string BackgroundColor { get; internal set; }
        public string Description { get; internal set; }
        public IEnumerable<InventoryTag> Tags { get; internal set; }
    }
}