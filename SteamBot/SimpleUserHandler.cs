using SteamKit2;
using System.Collections.Generic;
using SteamTrade;
using SteamTrade.TradeWebAPI;
using SteamTrade.Inventory;

namespace SteamBot
{
    public class SimpleUserHandler : UserHandler
    {
        public int ScrapPutUp;

        public SimpleUserHandler (Bot bot, SteamID sid) : base(bot, sid) {}

        public override bool OnGroupAdd()
        {
            return false;
        }

        public override bool OnFriendAdd () 
        {
            return true;
        }

        public override void OnLoginCompleted()
        {
        }

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

        private void OnInvCallback(CInventory inventory)
        {
            if (inventory.Items == null)
            {
                Log.Info("NO ITEMS!");
                return;
            }
            Log.Info("In total, we parsed over {0} items for {1}", ((List<InventoryItem>)inventory.Items).Count, inventory.InventoryType.Game);
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
            SendTradeMessage("Success. Please put up your items.");
        }
        
        public override void OnTradeAddItem (InventoryItem inventoryItem) {}
        
        public override void OnTradeRemoveItem (InventoryItem inventoryItem) {}
        
        public override void OnTradeMessage (string message) {}
        
        public override void OnTradeReady (bool ready) 
        {
            if (!ready)
            {
                Trade.SetReady (false);
            }
            else
            {
                if(Validate ())
                {
                    Trade.SetReady (true);
                }
                SendTradeMessage("Scrap: {0}", ScrapPutUp);
            }
        }

        public override void OnTradeSuccess()
        {
            // Trade completed successfully
            Log.Success("Trade Complete.");
        }

        public override void OnTradeAccept() 
        {
            if (Validate() || IsAdmin)
            {
                //Even if it is successful, AcceptTrade can fail on
                //trades with a lot of items so we use a try-catch
                try {
                    if (Trade.AcceptTrade())
                        Log.Success("Trade Accepted!");
                }
                catch {
                    Log.Warn ("The trade might have failed, but we can't be sure.");
                }
            }
        }

        public bool Validate ()
        {            
            ScrapPutUp = 0;
            
            List<string> errors = new List<string> ();
            
            foreach (TradeUserAssets asset in Trade.OtherOfferedItems)
            {
                var item = Trade.GetOtherItem(asset.assetid);
                if (item.OriginalName.Equals("Scrap Metal"))
                    ScrapPutUp++;
                else if (item.OriginalName.Equals("Reclaimed Metal"))
                    ScrapPutUp += 3;
                else if (item.OriginalName.Equals("Refined Metal"))
                    ScrapPutUp += 9;
                else
                    errors.Add ("Item " + item.DisplayName + " is not a metal.");
            }
            
            if (ScrapPutUp < 1)
            {
                errors.Add ("You must put up at least 1 scrap.");
            }
            
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

