using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamTrade.Inventory
{
    public class InventoryTag
    {
        public string InternalName { get; internal set; }
        public string Name { get; internal set; }
        public string Category { get; internal set; }
        public string CategoryName { get; internal set; }
        public string Color { get; internal set; }
    }
}
