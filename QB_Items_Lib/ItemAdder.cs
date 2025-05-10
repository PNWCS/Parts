using QBFC16Lib;
using Serilog;
using System;
using System.Collections.Generic;

namespace QB_Items_Lib
{
    public static class ItemAdder
    {
        public static void AddItems(List<Item> items)
        {
            Log.Information("ItemAdder Initialized");

            bool sessionBegun = false;
            bool connectionOpen = false;
            QBSessionManager? sessionManager = null;

            try
            {
                sessionManager = new QBSessionManager();
                sessionManager.OpenConnection("", AppConfig.QB_APP_NAME);
                connectionOpen = true;
                sessionManager.BeginSession("", ENOpenMode.omDontCare);
                sessionBegun = true;

                foreach (var item in items)
                {
                    // Make the name unique to avoid duplicates, but keep it within QB's string length limits
                    // Use only the last 8 digits of ticks to keep name shorter
                    string shortTicks = DateTime.Now.Ticks.ToString();
                    shortTicks = shortTicks.Substring(Math.Max(0, shortTicks.Length - 8));
                    item.Name += "_" + shortTicks;

                    try
                    {
                        Log.Information($"Adding item: {item.Name}");

                        var requestSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                        requestSet.Attributes.OnError = ENRqOnError.roeContinue;

                        IItemInventoryAdd itemAddRq = requestSet.AppendItemInventoryAddRq();
                        itemAddRq.Name.SetValue(item.Name);

                        // Ensure price is within valid range (round to 2 decimal places and ensure it's positive)
                        double salesPrice = Math.Max(0.01, Math.Round((double)item.SalesPrice, 2));
                        itemAddRq.SalesPrice.SetValue(salesPrice);

                        // Ensure ManufacturerPartNumber is not null before setting
                        if (!string.IsNullOrEmpty(item.ManufacturerPartNumber))
                        {
                            itemAddRq.ManufacturerPartNumber.SetValue(item.ManufacturerPartNumber);
                        }

                        itemAddRq.IncomeAccountRef.FullName.SetValue("Sales");
                        itemAddRq.AssetAccountRef.FullName.SetValue("Inventory Asset");
                        itemAddRq.COGSAccountRef.FullName.SetValue("Cost of Goods Sold");

                        itemAddRq.SalesDesc.SetValue(item.Name);
                        itemAddRq.PurchaseDesc.SetValue(item.Name);

                        // Also ensure purchase cost is valid (70% of sales price and properly rounded)
                        double purchaseCost = Math.Max(0.01, Math.Round(salesPrice * 0.7, 2));
                        itemAddRq.PurchaseCost.SetValue(purchaseCost);

                        itemAddRq.QuantityOnHand.SetValue(10);

                        var responseSet = sessionManager.DoRequests(requestSet);
                        var response = responseSet?.ResponseList?.GetAt(0);
                        string xml = responseSet?.ToXMLString() ?? "null";

                        Log.Information($"[RESPONSE XML] for {item.Name}:\n{xml}");

                        if (response != null)
                        {
                            Log.Information($"[RESPONSE] Status: {response.StatusCode}, Message: {response.StatusMessage}");
                        }

                        if (response?.StatusCode == 0 && response.Detail is IItemInventoryRet ret && ret.ListID != null)
                        {
                            item.QB_ID = ret.ListID.GetValue();
                        }
                        else
                        {
                            Log.Warning($"[WARNING] Response did not contain valid QB_ID for item: {item.Name}");
                        }

                        // Fallback if QB_ID is still empty
                        if (string.IsNullOrWhiteSpace(item.QB_ID))
                        {
                            if (xml.Contains("<ListID>"))
                            {
                                int start = xml.IndexOf("<ListID>") + "<ListID>".Length;
                                int end = xml.IndexOf("</ListID>");
                                if (start >= 0 && end > start)
                                {
                                    item.QB_ID = xml[start..end];
                                    Log.Warning($"[FALLBACK] ListID extracted from XML for {item.Name}: {item.QB_ID}");
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(item.QB_ID))
                        {
                            Log.Information($"✅ Successfully added item {item.Name} with QB_ID = {item.QB_ID}");
                        }
                        else
                        {
                            Log.Error($"❌ Failed to assign QB_ID for item: {item.Name}");
                            throw new Exception($"Failed to assign QB_ID for item: {item.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Exception while adding item: {item.Name}");
                        throw; // Rethrow to ensure the test fails with proper context
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing QuickBooks session.");
                throw; // Rethrow to propagate the error
            }
            finally
            {
                if (sessionBegun && sessionManager != null)
                {
                    try { sessionManager.EndSession(); }
                    catch (Exception ex) { Log.Error(ex, "Error ending session."); }
                }
                if (connectionOpen && sessionManager != null)
                {
                    try { sessionManager.CloseConnection(); }
                    catch (Exception ex) { Log.Error(ex, "Error closing connection."); }
                }
            }
        }
    }
}