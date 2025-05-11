using QBFC16Lib;
using Serilog;
using System.Collections.Generic;

namespace QB_Items_Lib
{
    public static class ItemAdder
    {
        public static int AddItems(List<Item> items, QBSessionManager sessionManager)
        {
            Log.Information("ItemAdder Initialized");

            int successCount = 0;

            try
            {
                // ✅ Create the request ONCE
                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                // ✅ Append ALL items to the same request
                foreach (var item in items)
                {
                    try
                    {
                        Log.Information($"Processing item: {item.Name}");

                        var itemAddRq = requestMsgSet.AppendItemInventoryAddRq();

                        itemAddRq.Name.SetValue(item.Name);
                        itemAddRq.SalesPrice.SetValue((double)item.SalesPrice);

                        itemAddRq.IncomeAccountRef.FullName.SetValue("Sales");
                        itemAddRq.AssetAccountRef.FullName.SetValue("Inventory Asset");
                        itemAddRq.COGSAccountRef.FullName.SetValue("Cost of Goods Sold");

                        itemAddRq.SalesDesc.SetValue(item.Name);
                        itemAddRq.PurchaseDesc.SetValue(item.Name);
                        itemAddRq.PurchaseCost.SetValue((double)(item.SalesPrice * 0.7m)); // Cost = 70% of price
                        itemAddRq.QuantityOnHand.SetValue(10); // Default quantity

                        if (!string.IsNullOrWhiteSpace(item.ManufacturerPartNumber) && item.ManufacturerPartNumber != "N/A")
                        {
                            itemAddRq.ManufacturerPartNumber.SetValue(item.ManufacturerPartNumber);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Error constructing request for item: {item.Name}");
                    }
                }

                // ✅ After all items are appended, send the request once
                IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                var responseList = responseMsgSet.ResponseList;
                if (responseList != null)
                {
                    for (int i = 0; i < responseList.Count; i++)
                    {
                        var response = responseList.GetAt(i);
                        if (response.StatusCode == 0)
                        {
                            successCount++;
                            Log.Information($"Item added successfully: {i + 1}");
                        }
                        else
                        {
                            Log.Warning($"Failed to add item: {response.StatusMessage}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error adding items to QuickBooks");
            }

            Log.Information($"ItemAdder completed. Added {successCount} out of {items.Count} items.");
            return successCount;
        }
    }
}
