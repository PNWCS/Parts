using QBFC16Lib;
using Serilog;
using System.Collections.Generic;

namespace QB_Items_Lib
{
    public static class ItemReader
    {
        public static List<Item> QueryAllItems()
        {
            Log.Information("ItemReader Initialized");

<<<<<<< HEAD
            var items = new List<Item>(); // Simplified collection initialization
=======
            var items = new List<Item>();
>>>>>>> d65a978 (deleted requested folder)
            bool sessionBegun = false;
            bool connectionOpen = false;
            QBSessionManager? sessionManager = null;

            try
            {
<<<<<<< HEAD
                // Create the session Manager object
=======
>>>>>>> d65a978 (deleted requested folder)
                sessionManager = new();

                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                BuildItemQueryRq(requestMsgSet);

                sessionManager.OpenConnection("", AppConfig.QB_APP_NAME);
                connectionOpen = true;
                sessionManager.BeginSession("", ENOpenMode.omDontCare);
                sessionBegun = true;

                IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                sessionManager.EndSession();
                sessionBegun = false;
                sessionManager.CloseConnection();
                connectionOpen = false;

                items = WalkItemQueryRs(responseMsgSet);
            }
            catch (Exception e)
            {
                Log.Error("Error while querying items from QuickBooks: " + e.Message);
                if (sessionBegun && sessionManager != null)
                {
                    sessionManager.EndSession();
                }
                if (connectionOpen && sessionManager != null)
                {
                    sessionManager.CloseConnection();
                }
            }

            Log.Information("ItemReader Completed");
            return items;
        }

        private static void BuildItemQueryRq(IMsgSetRequest requestMsgSet)
        {
            Log.Information("Fetching Item List from QuickBooks...");

            requestMsgSet.AppendItemInventoryQueryRq();
            requestMsgSet.AppendItemNonInventoryQueryRq();
            requestMsgSet.AppendItemServiceQueryRq();
            requestMsgSet.AppendItemOtherChargeQueryRq();
            requestMsgSet.AppendItemPaymentQueryRq();
            requestMsgSet.AppendItemDiscountQueryRq();
        }

<<<<<<< HEAD
        // Process the response and map it to a list of Items
        private static List<Item> WalkItemInventoryQueryRs(IMsgSetResponse? responseMsgSet)
        {
            var items = new List<Item>(); // Simplified collection initialization

            if (responseMsgSet == null) return items;

            IResponseList? responseList = responseMsgSet.ResponseList;
=======
        private static List<Item> WalkItemQueryRs(IMsgSetResponse? responseMsgSet)
        {
            var items = new List<Item>();

            if (responseMsgSet == null) return items;

            var responseList = responseMsgSet.ResponseList;
>>>>>>> d65a978 (deleted requested folder)
            if (responseList == null) return items;

            for (int i = 0; i < responseList.Count; i++)
            {
                var response = responseList.GetAt(i);

                if (response.StatusCode >= 0 && response.Detail != null)
                {
                    var responseType = (ENResponseType)response.Type.GetValue();

                    if (responseType == ENResponseType.rtItemInventoryQueryRs)
                    {
                        var retList = (IItemInventoryRetList)response.Detail;
                        for (int j = 0; j < retList.Count; j++)
                        {
                            var ret = retList.GetAt(j);
                            items.Add(new Item(
                                ret.Name.GetValue(),
                                ret.SalesPrice != null ? (decimal)ret.SalesPrice.GetValue() : 0,
                                ret.ManufacturerPartNumber?.GetValue() ?? "N/A"
                            )
                            { QB_ID = ret.ListID.GetValue() });
                        }
                    }
                    else
                    {
                        // For Non-Inventory, Service, OtherCharge, Payment, Discount Items
                        dynamic retList = response.Detail;
                        for (int j = 0; j < retList.Count; j++)
                        {
                            var ret = retList.GetAt(j);
                            items.Add(new Item(
                                ret.Name.GetValue(),
                                0,          // No price field available
                                "N/A"       // No manufacturer part number field
                            )
                            { QB_ID = ret.ListID.GetValue() });
                        }
                    }
                }
            }

            return items;
        }
<<<<<<< HEAD

        // Map the IItemInventoryRetList to Item objects
        private static List<Item> WalkItemInventoryRet(IItemInventoryRetList? ItemInventoryRetList)
        {
            var items = new List<Item>(); // Simplified collection initialization

            if (ItemInventoryRetList == null) return items;

            for (int i = 0; i < ItemInventoryRetList.Count; i++)
            {
                IItemInventoryRet itemInventoryRet = ItemInventoryRetList.GetAt(i);

                string name = itemInventoryRet.Name.GetValue();
                decimal salesPrice = itemInventoryRet.SalesPrice != null ? (decimal)itemInventoryRet.SalesPrice.GetValue() : 0;
                string manufacturerPartNumber = itemInventoryRet.ManufacturerPartNumber != null ? itemInventoryRet.ManufacturerPartNumber.GetValue() : "N/A";
                string listID = itemInventoryRet.ListID.GetValue();

                // Create Item object and add it to the list
                var item = new Item(name, salesPrice, manufacturerPartNumber) { QB_ID = listID };
                items.Add(item);

                Log.Information($"Successfully retrieved {name} from QB");
            }

            return items;
        }
=======
>>>>>>> d65a978 (deleted requested folder)
    }
}
