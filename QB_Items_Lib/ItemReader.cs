using QBFC16Lib;
using Serilog;

namespace QB_Items_Lib
{
    public class ItemReader
    {
        // Query all items from QuickBooks
        public static List<Item> QueryAllItems()
        {
            Log.Information("ItemReader Initialized");

            var items = new List<Item>(); // Simplified collection initialization
            bool sessionBegun = false;
            bool connectionOpen = false;
            QBSessionManager? sessionManager = null;

            try
            {
                // Create the session Manager object
                sessionManager = new();

                // Create the message set request object to hold our request
                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                // Build the query for ItemInventory
                BuildItemInventoryQueryRq(requestMsgSet);

                // Connect to QuickBooks and begin a session
                sessionManager.OpenConnection("", AppConfig.QB_APP_NAME);
                connectionOpen = true;
                sessionManager.BeginSession("", ENOpenMode.omDontCare);
                sessionBegun = true;

                // Send the request and get the response from QuickBooks
                IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                // End the session and close the connection to QuickBooks
                sessionManager.EndSession();
                sessionBegun = false;
                sessionManager.CloseConnection();
                connectionOpen = false;

                // Process the response from QuickBooks and map it to the Item list
                items = WalkItemInventoryQueryRs(responseMsgSet);
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

        // Build the request for ItemInventory query
        private static void BuildItemInventoryQueryRq(IMsgSetRequest requestMsgSet)
        {
            IItemInventoryQuery ItemInventoryQueryRq = requestMsgSet.AppendItemInventoryQueryRq();
            Log.Information("Fetching Item List from QuickBooks...");

            // Add ListID to the elements we want returned
            ItemInventoryQueryRq.IncludeRetElementList.Add("ListID");
            ItemInventoryQueryRq.IncludeRetElementList.Add("Name");
            ItemInventoryQueryRq.IncludeRetElementList.Add("SalesPrice");
            ItemInventoryQueryRq.IncludeRetElementList.Add("ManufacturerPartNumber");
        }

        // Process the response and map it to a list of Items
        private static List<Item> WalkItemInventoryQueryRs(IMsgSetResponse? responseMsgSet)
        {
            var items = new List<Item>(); // Simplified collection initialization

            if (responseMsgSet == null) return items;

            IResponseList? responseList = responseMsgSet.ResponseList;
            if (responseList == null) return items;

            for (int i = 0; i < responseList.Count; i++)
            {
                IResponse response = responseList.GetAt(i);

                // Check the response status code
                if (response.StatusCode >= 0 && response.Detail != null)
                {
                    ENResponseType responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtItemInventoryQueryRs)
                    {
                        IItemInventoryRetList ItemInventoryRet = (IItemInventoryRetList)response.Detail;
                        items.AddRange(WalkItemInventoryRet(ItemInventoryRet));
                    }
                }
            }

            return items;
        }

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
    }
}