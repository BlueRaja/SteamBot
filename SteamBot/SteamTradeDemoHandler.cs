using System.Collections.Generic;
using System.Linq;
using SteamKit2;
using SteamTrade;
using SteamTrade.Inventory;

namespace SteamBot
{
    public class SteamTradeDemoHandler : UserHandler
    {
        // NEW ------------------------------------------------------------------
        private bool tested;
        // ----------------------------------------------------------------------

        public SteamTradeDemoHandler(Bot bot, SteamID sid) : base(bot, sid)
        {
        }

        public override bool OnGroupAdd()
        {
            return false;
        }

        public override bool OnFriendAdd () 
        {
            return true;
        }

        public override void OnLoginCompleted() {}

        public override void OnChatRoomMessage(SteamID chatID, SteamID sender, string message)
        {
            Log.Info(Bot.SteamFriends.GetFriendPersonaName(sender) + ": " + message);
            base.OnChatRoomMessage(chatID, sender, message);
        }

        public override void OnFriendRemove () {}
        
        public override void OnMessage (string message, EChatEntryType type) 
        {
            SendChatMessage(Bot.ChatResponse);
        }

        public override bool OnTradeRequest() 
        {
            return true;
        }
        
        public override void OnTradeError (string error) 
        {
            SendChatMessage("Oh, there was an error: {0}.", error);
            Bot.log.Warn (error);
        }
        
        public override void OnTradeTimeout () 
        {
            SendChatMessage("Sorry, but you were AFK and the trade was canceled.");
            Bot.log.Info ("User was kicked because he was AFK.");
        }
        
        public override void OnTradeInit() 
        {
            // NEW -------------------------------------------------------------------------------
            tested = false;
            SendTradeMessage("Type 'test' to start.");
            // -----------------------------------------------------------------------------------
        }
        
        public override void OnTradeAddItem (InventoryItem inventoryItem) {
            // USELESS DEBUG MESSAGES -------------------------------------------------------------------------------
            SendTradeMessage("Object AppID: {0}", inventoryItem.InventoryType.Game);
            SendTradeMessage("Object ContextId: {0}", inventoryItem.InventoryType.ContextId);

            switch (inventoryItem.InventoryType.Game)
            {
                case Game.TeamFortress2:
                    SendTradeMessage("TF2 Item Added.");
                    SendTradeMessage("Name: {0}", inventoryItem.DisplayName);
                    SendTradeMessage("Quality: {0}", inventoryItem.Tags.ToArray()[0].Name);
                    break;
                case Game.Steam:
                    SendTradeMessage("Steam Inventory Item Added.");
                    SendTradeMessage("Type: {0}", inventoryItem.Type);
                    SendTradeMessage("Marketable: {0}", inventoryItem.IsMarketable ? "Yes" : "No");
                    break;
                default:
                    SendTradeMessage("Item name: {0}", inventoryItem.DisplayName);
                    break;
            }
            // ------------------------------------------------------------------------------------------------------
        }
        
        public override void OnTradeRemoveItem (InventoryItem inventoryItem) {}
        
        public override void OnTradeMessage (string message) {
            switch (message.ToLower())
            {
                case "test":
                    if (tested)
                    {
                        Trade.RemoveAllItems();
                    }
                    else
                    {
                        foreach (var inv in Bot.MyInventory)
                        {
                            if (inv.InventoryType.Equals(InventoryType.Steam_Community))
                                Trade.AddAllItemsByInventory(inv);
                        }
                    }

                    tested = !tested;

                break;

                case "remove":
                    foreach (var inv in Bot.MyInventory)
                    {
                        if (inv.InventoryType.Equals(InventoryType.Steam_Community))
                            Trade.AddAllItemsByInventory(inv);
                    }
                break;
            }
        }
        
        public override void OnTradeReady (bool ready) 
        {
            //Because SetReady must use its own version, it's important
            //we poll the trade to make sure everything is up-to-date.
            Trade.Poll();
            if (!ready)
            {
                Trade.SetReady (false);
            }
            else
            {
                if(Validate () | IsAdmin)
                {
                    Trade.SetReady (true);
                }
            }
        }

        public override void OnTradeSuccess()
        {
            // Trade completed successfully
            Log.Success("Trade Complete.");
        }

        public override void OnTradeAccept() 
        {
            if (Validate() | IsAdmin)
            {
                //Even if it is successful, AcceptTrade can fail on
                //trades with a lot of items so we use a try-catch
                try {
                    Trade.AcceptTrade();
                }
                catch {
                    Log.Warn ("The trade might have failed, but we can't be sure.");
                }

                Log.Success ("Trade Complete!");
            }
        }

        public bool Validate ()
        {            
            List<string> errors = new List<string> ();
            errors.Add("This demo is meant to show you how to handle SteamInventory Items. Trade cannot be completed, unless you're an Admin.");

            // send the errors
            if (errors.Count != 0)
                SendTradeMessage("There were errors in your trade: ");

            foreach (string error in errors)
            {
                SendTradeMessage(error);
            }
            
            return errors.Count == 0;
        }
        
    }
 
}

