
using Newtonsoft.Json;
using SteamKit2;
using SteamTrade;
using SteamTrade.TradeOffer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Timers;
using TradeAsset = SteamTrade.TradeOffer.TradeOffer.TradeStatusUser.TradeAsset;

namespace SteamBot
{
    public class DepositTradeOfferUserHandler : UserHandler
    {
        public DepositTradeOfferUserHandler(Bot bot, SteamID sid) : base(bot, sid) { }

        System.Timers.Timer timer;

        public override void OnBotCommand(string command)
        {
            if (IsAdmin)
            {
                if (command.Equals("confirm"))
                {
                    Bot.AcceptAllMobileTradeConfirmations();
                    Log.Success("All trade offers confirmed... on bot: " + Bot.SteamUser.SteamID.ConvertToUInt64());
                }
            }
            if (command.Equals("skins"))
            {
                //Get current pot and all items in inventory
                string withdrawUrl = Bot.BotWebsiteURL + "/php/bot-withdraw.php";
                var withdrawRequest = (HttpWebRequest)WebRequest.Create(withdrawUrl);
                var withdrawResponse = (HttpWebResponse)withdrawRequest.GetResponse();
                string withdrawString = new StreamReader(withdrawResponse.GetResponseStream()).ReadToEnd();

                WithdrawResponse botInventory = JsonConvert.DeserializeObject<WithdrawResponse>(withdrawString);

                var data = botInventory.data;

                var rgInventory = data.rgInventory;
                var currentPot = data.currentPot;

                var withdrawTradeOffer = Bot.NewTradeOffer(new SteamID(Bot.ProfitAdmin));

                foreach (var inventoryItemKeyVal in rgInventory)
                {
                    var invItem = inventoryItemKeyVal.Value;
                    long classId = invItem.classid, instanceId = invItem.instanceid;

                    bool withdrawThisItem = true;
                    //Check to see if this item is in the current pot
                    foreach (var potItem in currentPot)
                    {
                        long classIdPot = potItem.classid, instanceIdPot = potItem.instanceid;

                        if (classId == classIdPot && instanceId == instanceIdPot)
                        {
                            withdrawThisItem = false;
                        }
                    }

                    if (withdrawThisItem)
                    {
                        if (invItem.instanceid != 0)
                        {
                            if (invItem.instanceid != 519977179)
                            {
                                var assetId = invItem.id;
                                withdrawTradeOffer.Items.AddMyItem(730, 2, assetId, 1);
                            }
                        }
                    }
                }

                if (withdrawTradeOffer.Items.GetMyItems().Count != 0)
                {
                    string withdrawOfferId;
                    withdrawTradeOffer.Send(out withdrawOfferId, "Here are the withdraw items requested.");
                    Bot.AcceptAllMobileTradeConfirmations();
                    Log.Success("Withdraw trade offer sent. Offer ID: " + withdrawOfferId);
                }
                else
                {
                    Log.Error("There are no profit items to withdraw at this time.");
                }
            }
            if (command.Equals("withdraw"))
            {
                //Get current pot and all items in inventory
                string withdrawUrl = Bot.BotWebsiteURL + "/php/bot-withdraw.php";
                var withdrawRequest = (HttpWebRequest)WebRequest.Create(withdrawUrl);
                var withdrawResponse = (HttpWebResponse)withdrawRequest.GetResponse();
                string withdrawString = new StreamReader(withdrawResponse.GetResponseStream()).ReadToEnd();

                WithdrawResponse botInventory = JsonConvert.DeserializeObject<WithdrawResponse>(withdrawString);

                var data = botInventory.data;

                var rgInventory = data.rgInventory;
                var currentPot = data.currentPot;

                var withdrawTradeOffer = Bot.NewTradeOffer(new SteamID(Bot.ProfitAdmin));

                foreach (var inventoryItemKeyVal in rgInventory)
                {
                    var invItem = inventoryItemKeyVal.Value;
                    long classId = invItem.classid, instanceId = invItem.instanceid;

                    bool withdrawThisItem = true;
                    //Check to see if this item is in the current pot
                    foreach (var potItem in currentPot)
                    {
                        long classIdPot = potItem.classid, instanceIdPot = potItem.instanceid;

                        if (classId == classIdPot && instanceId == instanceIdPot)
                        {
                            withdrawThisItem = false;
                        }
                    }

                    if (withdrawThisItem)
                    {

                        var assetId = invItem.id;
                        withdrawTradeOffer.Items.AddMyItem(730, 2, assetId, 1);

                    }
                }

                if (withdrawTradeOffer.Items.GetMyItems().Count != 0)
                {
                    string withdrawOfferId;
                    withdrawTradeOffer.Send(out withdrawOfferId, "Here are the withdraw items requested.");
                    Bot.AcceptAllMobileTradeConfirmations();
                    Log.Success("Withdraw trade offer sent. Offer ID: " + withdrawOfferId);
                }
                else
                {
                    Log.Error("There are no profit items to withdraw at this time.");
                }
            }
        }

        public class WithdrawResponse
        {
            public int success;
            public string errMsg;
            public WithdrawData data;
        }

        public class WithdrawData
        {
            public IDictionary<string, inventoryItem> rgInventory;
            public List<inventoryItem> currentPot;
        }

        public override void OnNewTradeOffer(TradeOffer offer)
        {

            var escrow = Bot.GetEscrowDuration(offer.TradeOfferId);

            if (escrow.DaysMyEscrow != 0 || escrow.DaysTheirEscrow != 0)
            {
                doWebWithCatch(1, () =>
                {
                    if (offer.Decline())
                    {
                        Log.Error("User has not been using the Mobile Authenticator for 7 days or has turned off trade confirmations, offer declined.");
                    }
                });

            }
            else
            {

                //Get password from file on desktop
                string pass = Bot.BotDBPassword;

                //Get items in the trade, and ID of user sending trade
                var theirItems = offer.Items.GetTheirItems();
                var myItems = offer.Items.GetMyItems();
                var userID = offer.PartnerSteamId;

                bool shouldDecline = false;

                //Check if they are trying to get items from the bot
                if (myItems.Count > 0 || theirItems.Count == 0)
                {
                    //shouldDecline = true;
                    Log.Error("Offer declined because the offer wasn't a gift; the user wanted items instead of giving.");
                }

                //Check to make sure all items are for CS: GO.
                foreach (TradeAsset item in theirItems)
                {
                    if (item.AppId != 730)
                    {
                        shouldDecline = true;
                        Log.Error("Offer declined because one or more items was not for CS: GO.");
                    }
                }

                //Check if there are more than 10 items in the trade
                if (theirItems.Count > 5)
                {
                    shouldDecline = true;
                    Log.Error("Offer declined because there were more than 5 items in the deposit.");
                }

                if (shouldDecline)
                {
                    doWebWithCatch(1, () =>
                    {
                        if (offer.Decline())
                        {
                            Log.Error("Offer declined.");
                        }
                    });

                    return;
                }

                Log.Success("Offer approved, accepting.");
                //Send items to server and check if all items add up to more than $1.
                //If they do, accept the trade. If they don't, decline the trade.
                string postData = "password=" + pass;
                postData += "&owner=" + userID;

                string theirItemsJSON = JsonConvert.SerializeObject(theirItems);

                postData += "&items=" + theirItemsJSON;

                string url = Bot.BotWebsiteURL + "/php/check-items.php";
                var request = (HttpWebRequest)WebRequest.Create(url);

                var data = Encoding.ASCII.GetBytes(postData);

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                //Uncomment this line to view the response from check-items.php
                //Log.Success ("Response received from check-items.php: \n" + responseString);

                JSONClass responseJsonObj = JsonConvert.DeserializeObject<JSONClass>(responseString);

                if (responseJsonObj.success == 1)
                {
                    //Get data array from json
                    var jsonData = responseJsonObj.data;

                    if (jsonData.minDeposit == 1)
                    {
                        doWebWithCatch(10, () =>
                        {
                            if (offer.Accept())
                            {
                                Log.Success("Offer accepted from " + userID);

                                //Put items into the pot
                                string urlPutItemsIn = Bot.BotWebsiteURL + "/php/deposit.php";
                                var requestUrlPutItemsIn = (HttpWebRequest)WebRequest.Create(urlPutItemsIn);

                                string postDataPutItemsIn = "password=" + pass;
                                postDataPutItemsIn += "&owner=" + userID;
                                postDataPutItemsIn += "&items=" + jsonData.allItems;
                                //Log.Success (jsonData.allItems);

                                var dataPutItemsIn = Encoding.ASCII.GetBytes(postDataPutItemsIn);

                                requestUrlPutItemsIn.Method = "POST";
                                requestUrlPutItemsIn.ContentType = "application/x-www-form-urlencoded";
                                requestUrlPutItemsIn.ContentLength = dataPutItemsIn.Length;

                                using (var stream = requestUrlPutItemsIn.GetRequestStream())
                                {
                                    stream.Write(dataPutItemsIn, 0, dataPutItemsIn.Length);
                                }

                                var responsePutItemsIn = (HttpWebResponse)requestUrlPutItemsIn.GetResponse();
                                string responsePutItemsInString = new StreamReader(responsePutItemsIn.GetResponseStream()).ReadToEnd();

                                //Uncomment this line to view the response from deposit.php
                                //Log.Success ("Response received from deposit.php: " + responsePutItemsInString);

                                JSONClass responseJsonObjPutItemsIn = JsonConvert.DeserializeObject<JSONClass>(responsePutItemsInString);
                                jsonData = responseJsonObjPutItemsIn.data;
                            }
                        });

                        //Check if it should start the timer
                        if (jsonData.startTimer == 1)
                        {
                            //Check if the timer is already running.
                            if (!timerRunning)
                            {
                                timer = new System.Timers.Timer();
                                timer.Elapsed += new ElapsedEventHandler(timerEvent);
                                timer.Interval = 1000;
                                timer.Start();

                                timerRunning = true;
                            }
                        }

                        //Check if the pot is over
                        if (jsonData.potOver == 1)
                        {
                            //End the timer
                            timerTime = 0;
                            timer.Stop();

                            //Get items to give and keep, and the winner and their trade token
                            var itemsToGive = jsonData.tradeItems;
                            var itemsToKeep = jsonData.profitItems;

                            string winnerSteamIDString = jsonData.winnerSteamId;
                            SteamID winnerSteamID = new SteamID(winnerSteamIDString);

                            string winnerTradeToken = jsonData.winnerTradeToken;

                            Log.Success("Winner steam id: " + winnerSteamIDString + ", token: " + winnerTradeToken);

                            //Get bot's inventory json
                            string botInvUrl = "http://steamcommunity.com/profiles/" + Bot.SteamUser.SteamID.ConvertToUInt64() + "/inventory/json/730/2";
                            var botInvRequest = (HttpWebRequest)WebRequest.Create(botInvUrl);
                            var botInvResponse = (HttpWebResponse)botInvRequest.GetResponse();
                            string botInvString = new StreamReader(botInvResponse.GetResponseStream()).ReadToEnd();

                            BotInventory botInventory = JsonConvert.DeserializeObject<BotInventory>(botInvString);
                            if (botInventory.success != true)
                            {
                                Log.Error("An error occured while fetching the bot's inventory.");
                                return;
                            }
                            var rgInventory = botInventory.rgInventory;

                            //Create trade offer for the winner
                            var winnerTradeOffer = Bot.NewTradeOffer(winnerSteamID);

                            //Loop through all winner's items and add them to trade
                            List<long> alreadyAddedToWinnerTrade = new List<long>();
                            foreach (CSGOItemFromWeb item in itemsToGive)
                            {
                                long classId = item.classId, instanceId = item.instanceId;

                                //Loop through all inventory items and find the asset id for the item
                                long assetId = 0;
                                foreach (var inventoryItem in rgInventory)
                                {
                                    var value = inventoryItem.Value;
                                    long tAssetId = value.id, tClassId = value.classid, tInstanceId = value.instanceid;

                                    if (tClassId == classId && tInstanceId == instanceId)
                                    {
                                        //Check if this assetId has already been added to the trade
                                        if (alreadyAddedToWinnerTrade.Contains(tAssetId))
                                        {
                                            continue;
                                            //This is for when there are 2 of the same weapon, but they have different assetIds
                                        }
                                        assetId = tAssetId;
                                        break;
                                    }
                                }

                                //Log.Success ("Adding item to winner trade offer. Asset ID: " + assetId);

                                winnerTradeOffer.Items.AddMyItem(730, 2, assetId, 1);
                                alreadyAddedToWinnerTrade.Add(assetId);
                            }
                            //Send trade offer to winner
                            if (itemsToGive.Count > 0)
                            {
                                string winnerTradeOfferId, winnerMessage = "Congratulations, you have won on " + Bot.BotWebsiteName + "! Here are your items.";

                                doWebWithCatch(-1, () =>
                                {
                                    if (winnerTradeOffer.SendWithToken(out winnerTradeOfferId, winnerTradeToken, winnerMessage))
                                    {
                                        Bot.AcceptAllMobileTradeConfirmations();
                                        Log.Success("Offer sent to winner.");
                                    }
                                });
                            }
                            else
                            {
                                Log.Info("No items to give... strange");
                            }

                            //Now, send all of the profit items to my own account
                            //Put your own Steam ID here

                            var profitTradeOffer = Bot.NewTradeOffer(new SteamID(Bot.ProfitAdmin));

                            //Loop through all profit items and add them to trade
                            List<long> alreadyAddedToProfitTrade = new List<long>();
                            foreach (CSGOItemFromWeb item in itemsToKeep)
                            {
                                long classId = item.classId, instanceId = item.instanceId;

                                //Loop through all inventory items and find the asset id for the item
                                long assetId = 0;
                                foreach (var inventoryItem in rgInventory)
                                {
                                    var value = inventoryItem.Value;
                                    long tAssetId = value.id, tClassId = value.classid, tInstanceId = value.instanceid;

                                    if (tClassId == classId && tInstanceId == instanceId)
                                    {
                                        //Check if this assetId has already been added to the trade
                                        if (alreadyAddedToProfitTrade.Contains(tAssetId))
                                        {
                                            continue;
                                            //This is for when there are 2 of the same weapon, but they have different assetIds
                                        }
                                        assetId = tAssetId;
                                        break;
                                    }
                                }

                                //Log.Success ("Adding item to winner trade offer. Asset ID: " + assetId);

                                profitTradeOffer.Items.AddMyItem(730, 2, assetId, 1);
                                alreadyAddedToProfitTrade.Add(assetId);
                            }

                            //Send trade offer to myself with profit items
                            Log.Success(itemsToKeep.Count + "");
                            if (itemsToKeep.Count > 0)
                            {
                                string profitTradeOfferId, profitMessage = "Here are the profit items from the round.";

                                doWebWithCatch(10, () =>
                                {
                                    if (profitTradeOffer.Send(out profitTradeOfferId, profitMessage))
                                    { //Don't need the token because I am friends with the bot.
                                        Bot.AcceptAllMobileTradeConfirmations();
                                        Log.Success("Profit trade offer sent.");
                                    }
                                });
                            }
                        }
                        else
                        {
                            //Only try this one time, because even if it gives an error, it still gets declined.
                            doWebWithCatch(1, () =>
                            {
                                if (offer.Decline())
                                {
                                    Log.Error("Server deposit request failed, declining trade. Error message:\n" + responseJsonObj.errMsg);
                                }
                            });
                        }
                    }
                    else
                    {
                        //Only try this one time, because even if it gives an error, it still gets declined.
                        doWebWithCatch(1, () =>
                        {
                            if (offer.Decline())
                            {
                                Log.Error("Minimum deposit not reached, offer declined.");
                            }
                        });
                    }
                }

            }
        }

        //Timer stuff
        bool timerRunning = false;
        int timerTime = 0;
        private void timerEvent(object CancellationTokenSource, ElapsedEventArgs e)
        {
            timerTime++;

            //Check if the timer is at 2 minutes
            if (timerTime >= 120)
            {
                //If the timer is done, stop the timer and call to server to end the round/pick a winner
                timer.Stop();
                timerTime = 0;
                timerRunning = false;

                //Get password from file on desktop
                string pass = Bot.BotDBPassword;

                string urlTimerEnd = Bot.BotWebsiteURL + "/php/timer-end.php";
                var requestUrlTimerEnd = (HttpWebRequest)WebRequest.Create(urlTimerEnd);

                string postDataTimerEnd = "password=" + pass;

                var dataTimerEnd = Encoding.ASCII.GetBytes(postDataTimerEnd);

                requestUrlTimerEnd.Method = "POST";
                requestUrlTimerEnd.ContentType = "application/x-www-form-urlencoded";
                requestUrlTimerEnd.ContentLength = dataTimerEnd.Length;

                using (var stream = requestUrlTimerEnd.GetRequestStream())
                {
                    stream.Write(dataTimerEnd, 0, dataTimerEnd.Length);
                }

                var responseTimerEnd = (HttpWebResponse)requestUrlTimerEnd.GetResponse();
                string responseTimerEndString = new StreamReader(responseTimerEnd.GetResponseStream()).ReadToEnd();

                //Uncomment this line to print out the response from timer-end.php
                //Log.Success ("Response received from timer-end.php: " + responseTimerEndString);

                JSONClass timerEndJsonObj = JsonConvert.DeserializeObject<JSONClass>(responseTimerEndString);

                if (timerEndJsonObj.success != 1)
                {
                    Log.Error("Server request failed. Error message:\n" + timerEndJsonObj.errMsg);

                    return;
                }

                Data timerEndData = timerEndJsonObj.data;

                //Get items to give and keep, and the winner and their trade token
                var itemsToGive = timerEndData.tradeItems;
                var itemsToKeep = timerEndData.profitItems;

                string winnerSteamIDString = timerEndData.winnerSteamId;
                SteamID winnerSteamID = new SteamID(winnerSteamIDString);

                string winnerTradeToken = timerEndData.winnerTradeToken;

                Log.Success("Winner steam id: " + winnerSteamIDString + ", token: " + winnerTradeToken);

                //Get bot's inventory json
                string botInvUrl = "http://steamcommunity.com/profiles/" + Bot.SteamUser.SteamID.ConvertToUInt64() + "/inventory/json/730/2";
                var botInvRequest = (HttpWebRequest)WebRequest.Create(botInvUrl);
                var botInvResponse = (HttpWebResponse)botInvRequest.GetResponse();
                string botInvString = new StreamReader(botInvResponse.GetResponseStream()).ReadToEnd();

                BotInventory botInventory = JsonConvert.DeserializeObject<BotInventory>(botInvString);
                if (botInventory.success != true)
                {
                    Log.Error("An error occured while fetching the bot's inventory.");
                    return;
                }
                var rgInventory = botInventory.rgInventory;

                //Create trade offer for the winner
                var winnerTradeOffer = Bot.NewTradeOffer(winnerSteamID);

                //Loop through all winner's items and add them to trade
                List<long> alreadyAddedToWinnerTrade = new List<long>();
                foreach (CSGOItemFromWeb item in itemsToGive)
                {
                    long classId = item.classId, instanceId = item.instanceId;

                    //Loop through all inventory items and find the asset id for the item
                    long assetId = 0;
                    foreach (var inventoryItem in rgInventory)
                    {
                        var value = inventoryItem.Value;
                        long tAssetId = value.id, tClassId = value.classid, tInstanceId = value.instanceid;

                        if (tClassId == classId && tInstanceId == instanceId)
                        {
                            //Check if this assetId has already been added to the trade
                            if (alreadyAddedToWinnerTrade.Contains(tAssetId))
                            {
                                continue;
                                //This is for when there are 2 of the same weapon, but they have different assetIds
                            }
                            assetId = tAssetId;
                            break;
                        }
                    }

                    //Log.Success ("Adding item to winner trade offer. Asset ID: " + assetId);

                    winnerTradeOffer.Items.AddMyItem(730, 2, assetId, 1);
                    alreadyAddedToWinnerTrade.Add(assetId);
                }

                //Send trade offer to winner
                if (itemsToGive.Count > 0)
                {
                    string winnerTradeOfferId, winnerMessage = "Congratulations, you have won on " + Bot.BotWebsiteName + "! Here are your items.";

                    doWebWithCatch(-1, () =>
                    {
                        winnerTradeOffer.SendWithToken(out winnerTradeOfferId, winnerTradeToken, winnerMessage);
                    });
                    Bot.AcceptAllMobileTradeConfirmations();
                    Log.Success("Offer sent to winner.");
                }
                else
                {
                    Log.Info("No items to give... strange");
                }

                //Now, send all of the profit items to my own account
                var profitTradeOffer = Bot.NewTradeOffer(new SteamID(Bot.ProfitAdmin));

                //Loop through all profit items and add them to trade
                List<long> alreadyAddedToProfitTrade = new List<long>();
                foreach (CSGOItemFromWeb item in itemsToKeep)
                {
                    long classId = item.classId, instanceId = item.instanceId;

                    //Loop through all inventory items and find the asset id for the item
                    long assetId = 0;
                    foreach (var inventoryItem in rgInventory)
                    {
                        var value = inventoryItem.Value;
                        long tAssetId = value.id, tClassId = value.classid, tInstanceId = value.instanceid;

                        if (tClassId == classId && tInstanceId == instanceId)
                        {
                            //Check if this assetId has already been added to the trade
                            if (alreadyAddedToProfitTrade.Contains(tAssetId))
                            {
                                continue;
                                //This is for when there are 2 of the same weapon, but they have different assetIds
                            }
                            assetId = tAssetId;
                            break;
                        }
                    }

                    //Log.Success ("Adding item to winner trade offer. Asset ID: " + assetId);

                    profitTradeOffer.Items.AddMyItem(730, 2, assetId, 1);
                    alreadyAddedToProfitTrade.Add(assetId);
                }

                //Send trade offer to myself with profit items
                if (itemsToKeep.Count > 0)
                {
                    string profitTradeOfferId, profitMessage = "Here are the profit items from the round.";

                    doWebWithCatch(10, () =>
                    {
                        profitTradeOffer.Send(out profitTradeOfferId, profitMessage);
                    });
                    Bot.AcceptAllMobileTradeConfirmations();
                    Log.Success("Profit offer sent.");
                }
            }
        }



        private void doWebWithCatch(int tries, Action callback)
        {
            if (tries == 0)
            {
                Log.Error("Tried total number of tries, giving up.");
                return;
            }

            try
            {
                callback();
            }
            catch (WebException e)
            {
                Log.Error("Steam API error encountered. Trying again." + ((tries > 0) ? " Tries: " + tries : ""));
                //Log.Error("Steam API error encountered. Trying again." + ((tries > 0) ? " Tries: " + tries + " - " + e : ""));

                System.Timers.Timer t = new System.Timers.Timer();
                t.Elapsed += delegate
                {
                    doWebWithCatch(--tries, callback);
                    t.Stop();
                };
                t.Interval = 1000;
                t.Start();
            }
        }

        public override void OnMessage(string message, EChatEntryType type)
        {
            SendChatMessage(Bot.ChatResponse);
        }

        public override bool OnGroupAdd() { return false; }

        public override bool OnFriendAdd() { return IsAdmin; }

        public override void OnFriendRemove() { }

        public override void OnLoginCompleted() { }

        public override bool OnTradeRequest() { return false; }

        public override void OnTradeError(string error) { }

        public override void OnTradeTimeout() { }

        public override void OnTradeSuccess() { }

        public override void OnTradeAwaitingConfirmation(long tradeOfferID)
        {
            Log.Warn("Trade ended awaiting confirmation");
            SendChatMessage("Please complete the confirmation to finish the trade");
        }

        public override void OnTradeInit() { }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeMessage(string message) { }

        public override void OnTradeReady(bool ready) { }

        public override void OnTradeAccept() { }
    }

    public class CSGOItem
    {
        public long appId;
        public long contextId;
        public long assetId;
    }

    public class JSONClass
    {
        public int success;
        public string errMsg; //Error message
        public Data data;
    }

    public class Data
    {
        public int minDeposit;
        public int potOver;
        public int startTimer;
        public string allItems;
        public string winnerSteamId;
        public string winnerTradeToken;
        public List<CSGOItemFromWeb> tradeItems;
        public List<CSGOItemFromWeb> profitItems;
    }

    public class CSGOItemFromWeb
    {
        public long classId;
        public long instanceId;
    }

    //Classes for json bot inventory
    public class BotInventory
    {
        public bool success;
        public IDictionary<string, inventoryItem> rgInventory;
    }

    public class inventoryItem
    {
        public long id;
        public long classid;
        public long instanceid;
    }
}