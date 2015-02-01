using System;
using System.Collections.Generic;

namespace SteamTrade.Inventory
{
    public class InventoryItem : IEquatable<InventoryItem>, IComparable<InventoryItem>
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

        public bool Equals(InventoryItem other)
        {
            return CompareTo(other) == 0;
        }

        public override bool Equals(object other)
        {
            InventoryItem otherCasted = other as InventoryItem;
            return (otherCasted != null && Equals(otherCasted));
        }

        public override int GetHashCode()
        {
            return this.InventoryType.ContextId.GetHashCode() ^ Id.GetHashCode() ^ this.InventoryType.Game.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("id:{0}, appid:{1}, contextid:{2}", this.Id, this.InventoryType.Game, this.InventoryType.ContextId);
        }

        public int CompareTo(InventoryItem other)
        {
            if (this.InventoryType.Game != other.InventoryType.Game)
                return this.InventoryType.Game < other.InventoryType.Game ? -1 : 1;
            if (this.InventoryType.ContextId != other.InventoryType.ContextId)
                return this.InventoryType.ContextId < other.InventoryType.ContextId ? -1 : 1;
            if (this.Id != other.Id)
                return this.Id < other.Id ? -1 : 1;
            return 0;
        }
    }
}