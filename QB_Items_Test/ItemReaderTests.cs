using System.Diagnostics;
using System.IO;
using System.Linq;
using Serilog;
using QB_Items_Lib;  // Assumes Item, ItemReader, AppConfig, etc. are defined here.
using QBFC16Lib;             // For QuickBooks session and API interaction.
using Xunit;
using static QB_Items_Test.CommonMethods;
using QB_Items_Test;

namespace QB_Items_Test
{
    [Collection("Sequential Tests")]
    public class ItemReaderTests
    {
        [Fact]
        public void AddAndReadMultipleItems_FromQuickBooks_And_Verify_Logs()
        {
            const int ITEM_COUNT = 5;
            const int STARTING_COMPANY_ID = 100;
            var itemsToAdd = new List<Item>();

            // 1) Ensure Serilog has released file access before deleting old logs.
            EnsureLogFileClosed();
            DeleteOldLogFiles();
            ResetLogger();

            // 2) Build a list of random Item objects.
            // Each item has a random name, a sales price, and a manufacturer's part number (used as company id).
            for (int i = 0; i < ITEM_COUNT; i++)
            {
                string randomName = "TestItem_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                int companyID = STARTING_COMPANY_ID + i;
                // For this test, sales price is set to a base plus the index (for uniqueness).
                decimal salesPrice = 100.00m + i;
                // Use the manufacturer's part number field to store the company id.
                string manufacturerPartNumber = companyID.ToString();
                itemsToAdd.Add(new Item(randomName, salesPrice, manufacturerPartNumber));
            }

            // 3) Add items directly to QuickBooks.
            using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
            {
                foreach (var item in itemsToAdd)
                {
                    string qbID = AddItem(qbSession, item.Name, item.SalesPrice, item.ManufacturerPartNumber);
                    item.QB_ID = qbID; // Store the returned QB ListID.
                }
            }

            // 4) Query QuickBooks to retrieve all items.
            var allQBItems = ItemReader.QueryAllItems();

            // 5) Verify that all added items are present in QuickBooks.
            foreach (var item in itemsToAdd)
            {
                var matchingItem = allQBItems.FirstOrDefault(i => i.QB_ID == item.QB_ID);
                Assert.NotNull(matchingItem);
                Assert.Equal(item.Name, matchingItem.Name);
                Assert.Equal(item.SalesPrice, matchingItem.SalesPrice);
                Assert.Equal(item.ManufacturerPartNumber, matchingItem.ManufacturerPartNumber);
            }

            // 6) Cleanup: Delete the added items.
            using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
            {
                foreach (var item in itemsToAdd.Where(i => !string.IsNullOrEmpty(i.QB_ID)))
                {
                    DeleteItem(qbSession, item.QB_ID);
                }
            }

            // 7) Ensure logs are fully flushed before accessing them.
            EnsureLogFileClosed();

            // 8) Verify that a new log file exists.
            string logFile = GetLatestLogFile();
            EnsureLogFileExists(logFile);

            // 9) Read the log file content.
            string logContents = File.ReadAllText(logFile);

            // 10) Assert expected log messages exist.
            Assert.Contains("ItemReader Initialized", logContents);
            Assert.Contains("ItemReader Completed", logContents);

            // 11) Verify that each retrieved item was logged properly.
            foreach (var item in itemsToAdd)
            {
                string expectedLogMessage = $"Successfully retrieved {item.Name} from QB";
                Assert.Contains(expectedLogMessage, logContents);
            }
        }

        private string AddItem(QuickBooksSession qbSession, string name, decimal salesPrice, string manufacturerPartNumber)
        {
            IMsgSetRequest requestMsgSet = qbSession.CreateRequestSet();
            // Create the item add request.
            IItemAddRq itemAddRq = requestMsgSet.AppendItemInventoryAddRq();
            itemAddRq.Name.SetValue(name);
            itemAddRq.SalesPrice.SetValue(salesPrice);
            itemAddRq.ManufacturerPartNumber.SetValue(manufacturerPartNumber);
            // Additional item fields can be set here as needed.

            IMsgSetResponse responseMsgSet = qbSession.SendRequest(requestMsgSet);
            return ExtractListIDFromResponse(responseMsgSet);
        }

        private string ExtractListIDFromResponse(IMsgSetResponse responseMsgSet)
        {
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null || responseList.Count == 0)
                throw new Exception("No response from ItemAddRq.");

            IResponse response = responseList.GetAt(0);
            if (response.StatusCode != 0)
                throw new Exception($"ItemAdd failed: {response.StatusMessage}");

            // Attempt to cast the response detail to IItemRet.
            IItemRet? itemRet = response.Detail as IItemRet;
            if (itemRet == null)
                throw new Exception("No IItemRet returned after adding Item.");

            return itemRet.ListID?.GetValue()
                ?? throw new Exception("ListID is missing in QuickBooks response.");
        }

        private void DeleteItem(QuickBooksSession qbSession, string listID)
        {
            IMsgSetRequest requestMsgSet = qbSession.CreateRequestSet();
            IListDel listDelRq = requestMsgSet.AppendListDelRq();
            listDelRq.ListDelType.SetValue(ENListDelType.ldtItemInventory);
            listDelRq.ListID.SetValue(listID);

            IMsgSetResponse responseMsgSet = qbSession.SendRequest(requestMsgSet);
            WalkListDelResponse(responseMsgSet, listID);
        }

        private void WalkListDelResponse(IMsgSetResponse responseMsgSet, string listID)
        {
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null || responseList.Count == 0)
                return;

            IResponse response = responseList.GetAt(0);
            if (response.StatusCode == 0 && response.Detail != null)
            {
                Debug.WriteLine($"Successfully deleted Item (ListID: {listID}).");
            }
            else
            {
                throw new Exception($"Error Deleting Item (ListID: {listID}): {response.StatusMessage}. Status code: {response.StatusCode}");
            }
        }
    }
}
