using QBFC16Lib;
using Serilog;
using System.Collections.Generic;

namespace QB_Items_Lib
{
    public static class ItemReader
    {
        public static List<Item> QueryAllItems()
        {
            Log.Information("ItemReader Initialized"); // ✅ ADDED LINE

            var items = new List<Item>();
            bool sessionBegun = false;
            bool connectionOpen = false;
            QBSessionManager? sessionManager = null;

            try
            {
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

                foreach (var item in items)
                {
                    Log.Information("Successfully retrieved {ItemName} from QB", item.Name);
                }
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

        private static List<Item> WalkItemQueryRs(IMsgSetResponse? responseMsgSet)
        {
            var items = new List<Item>();

            if (responseMsgSet == null) return items;

            var responseList = responseMsgSet.ResponseList;
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
                        dynamic retList = response.Detail;
                        for (int j = 0; j < retList.Count; j++)
                        {
                            var ret = retList.GetAt(j);
                            items.Add(new Item(
                                ret.Name.GetValue(),
                                0,
                                "N/A"
                            )
                            { QB_ID = ret.ListID.GetValue() });
                        }
                    }
                }
            }

            return items;
        }
    }
}
