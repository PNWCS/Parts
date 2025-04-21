namespace QB_Items_Lib
{
    using QBFC16Lib;
    using Serilog;

    public static class ItemAdder
    {
        public static int AddItems(List<Item> items, QBSessionManager sessionManager)
        {
            Log.Information(messageTemplate: "ItemAdder Initialized");

            int successCount = 0;
            bool sessionBegun = false;
            bool connectionOpen = false;
            QBSessionManager qbSessionManager = new QBSessionManager() ?? SetupQBSessionManager();

            try
            {
                // Create the session Manager object
                qbSessionManager = new QBSessionManager();

                // Connect to QuickBooks and begin a session
                qbSessionManager.OpenConnection("", AppConfig.QB_APP_NAME);
                connectionOpen = true;
                qbSessionManager.BeginSession("", ENOpenMode.omDontCare);
                sessionBegun = true;

                // Process each item in the list
                foreach (var item in items)
                {
                    try
                    {
                        Log.Information($"Processing item: {item.Name}, SalesPrice: {item.SalesPrice}, ManufacturerPartNumber: {item.ManufacturerPartNumber}");
                        QBSessionManager quickBooksSessionManager = qbSessionManager;
                        quickBooksSessionManager.CreateMsgSetRequest("US", 16, 0).Attributes.OnError = ENRqOnError.roeContinue;

                        // Add an item using the provided information
                        AddSingleItem(quickBooksSessionManager.CreateMsgSetRequest("US", 16, 0), item);

                        // Send the request to QuickBooks
                        IMsgSetResponse responseMsgSet = quickBooksSessionManager.DoRequests(quickBooksSessionManager.CreateMsgSetRequest("US", 16, 0));

                        // Log the full response for debugging
                        Log.Information($"QuickBooks Response Status: {responseMsgSet.ResponseList?.GetAt(0)?.StatusCode}, Message: {responseMsgSet.ResponseList?.GetAt(0)?.StatusMessage}");

                        // Extract the ListID from the response
                        string listID = ExtractListIDFromResponse(responseMsgSet);

                        // If we got a valid ListID back, update the item and increment our success counter
                        if (!string.IsNullOrEmpty(listID))
                        {
                            item.QB_ID = listID;
                            successCount++;
                            Log.Information($"Successfully added item '{item.Name}' to QuickBooks with ListID: {listID}");
                        }
                        else
                        {
                            Log.Warning($"Failed to add item '{item.Name}' to QuickBooks (no ListID returned)");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Error adding item '{item.Name}' to QuickBooks");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error initializing QuickBooks session for adding items");
            }
            finally
            {
                // Clean up the session
                if (sessionBegun)
                {
                    try
                    {
                        qbSessionManager?.EndSession();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error ending QuickBooks session");
                    }
                }
                if (connectionOpen)
                {
                    try
                    {
                        qbSessionManager?.CloseConnection();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error closing QuickBooks connection");
                    }
                }
            }

            Log.Information($"ItemAdder completed. Added {successCount} out of {items.Count} items");
            return successCount;

            static QBSessionManager SetupQBSessionManager()
            {
                throw new InvalidOperationException("Failed to initialize QBSessionManager.");
            }

        }

        private static void AddSingleItem(IMsgSetRequest requestMsgSet, Item item)
        {
            Log.Information($"Constructing request for item: {item.Name}");

            try
            {
                // Create the item add request
                IItemInventoryAdd itemAddRq = requestMsgSet.AppendItemInventoryAddRq();

                // Set the required fields
                itemAddRq.Name.SetValue(item.Name);
                itemAddRq.SalesPrice.SetValue((double)item.SalesPrice);

                // Set manufacturer part number if available
                if (!string.IsNullOrEmpty(item.ManufacturerPartNumber))
                {
                    itemAddRq.ManufacturerPartNumber.SetValue(item.ManufacturerPartNumber);
                }

                // Set the required account references - these must match your QuickBooks account names exactly
                itemAddRq.IncomeAccountRef.FullName.SetValue("Sales");
                itemAddRq.AssetAccountRef.FullName.SetValue("Inventory Asset");
                itemAddRq.COGSAccountRef.FullName.SetValue("Cost of Goods Sold");

                // Set some additional default values
                itemAddRq.SalesDesc.SetValue(item.Name);
                itemAddRq.PurchaseDesc.SetValue(item.Name);
                itemAddRq.PurchaseCost.SetValue((double)item.SalesPrice * 0.7); // Cost is 70% of price
                itemAddRq.QuantityOnHand.SetValue(10); // Initial inventory

                Log.Information($"Request for item '{item.Name}' constructed successfully.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error constructing request for item: {item.Name}");
                throw;
            }
        }

        private static string ExtractListIDFromResponse(IMsgSetResponse responseMsgSet)
        {
            if (responseMsgSet == null)
            {
                Log.Warning("Null response from QuickBooks");
                return string.Empty;
            }

            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null || responseList.Count == 0)
            {
                Log.Warning("Empty response list from QuickBooks");
                return string.Empty;
            }

            IResponse response = responseList.GetAt(0);

            // Log the status code and message
            Log.Information($"Response Status: {response.StatusCode}, Message: {response.StatusMessage}");

            if (response.StatusCode != 0)
            {
                Log.Warning($"QuickBooks Error - StatusCode: {response.StatusCode}, Message: {response.StatusMessage}");
                return string.Empty;
            }

            // Try to get the ListID
            if (response.Detail is IItemInventoryRet inventoryRet && inventoryRet.ListID != null)
            {
                string listID = inventoryRet.ListID.GetValue();
                Log.Information($"Extracted ListID: {listID}");
                return listID;
            }

            Log.Warning("Could not extract ListID from response.");
            return string.Empty;
        }
    }
}
