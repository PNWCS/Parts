using QB_Items_Lib;
using QBFC16Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static QB_Items_Test.CommonMethods;

namespace QB_Items_Test
{
    [Collection("Sequential Tests")]
    public class ItemAdderTests
    {
        [Fact]
        public void AddMultipleItems_WithItemAdder_ThenQueryByQBID_ShouldHaveValidQBIDs()
        {
            // 1) Ensure Serilog (or any other logger) has released file access before deleting old logs (optional, if you're also testing logging).
            EnsureLogFileClosed();
            DeleteOldLogFiles();
            ResetLogger();

            // 2) Create a small batch of items to add.
            const int ITEM_COUNT = 5;
            var random = new Random();
            var itemsToAdd = new List<Item>();
            for (int i = 0; i < ITEM_COUNT; i++)
            {
                string uniqueName = "ItemAdderTest_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                decimal salesPrice = (decimal)(100 + random.NextDouble() * 50); // random-ish price
                // We'll use the ManufacturerPartNumber to store some distinct info, e.g. a random integer
                string partNumber = random.Next(1000, 9999).ToString();
                itemsToAdd.Add(new Item(uniqueName, salesPrice, partNumber));
            }

            // 3) Call the method under test: ItemAdder.AddItems(...)
            //    The assumption is that you have an ItemAdder class under test with a method AddItems(List<Item> items).
            //    This should populate each item's QB_ID (ListID) on success.
            ItemAdder.AddItems(itemsToAdd);

            // 4) Verify each item now has a QB_ID set.
            foreach (var item in itemsToAdd)
            {
                Assert.False(string.IsNullOrWhiteSpace(item.QB_ID),
                             $"Expected QB_ID to be set for item '{item.Name}', but it was null/empty.");
            }

            // 5) For each item, query QuickBooks by its QB_ID (ListID) using our own direct QB query logic
            //    (NOT using the Reader class) to confirm it actually exists.
            using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
            {
                foreach (var addedItem in itemsToAdd)
                {
                    var queriedItem = QueryItemByListID(qbSession, addedItem.QB_ID);
                    Assert.NotNull(queriedItem); // If not found, fail the test.

                    // Optional: Verify that the fields match
                    Assert.Equal(addedItem.Name, queriedItem.Name);
                    Assert.Equal(addedItem.SalesPrice, queriedItem.SalesPrice);
                    Assert.Equal(addedItem.ManufacturerPartNumber, queriedItem.ManufacturerPartNumber);
                }
            }

            // 6) (Optional) Cleanup: Delete the added items so the test is repeatable without polluting QB.
            using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
            {
                foreach (var item in itemsToAdd.Where(i => !string.IsNullOrEmpty(i.QB_ID)))
                {
                    DeleteItem(qbSession, item.QB_ID);
                }
            }

            // 7) Ensure logs are closed and verify if needed
            EnsureLogFileClosed();
            // Additional log checks, if your system logs these operations, can be added here.
        }

        /// <summary>
        /// Queries QuickBooks for an inventory item by its ListID.
        /// Returns a new Item object if found; otherwise returns null.
        /// </summary>
        private Item QueryItemByListID(QuickBooksSession qbSession, string listID)
        {
            IMsgSetRequest requestMsgSet = qbSession.CreateRequestSet();
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

            // Build the item inventory query request
            IItemInventoryQuery queryRq = requestMsgSet.AppendItemInventoryQueryRq();
            // Restrict the query to a specific ListID
            queryRq.ORListQueryWithOwnerIDAndClass.ListIDList.Add(listID);

            IMsgSetResponse responseMsgSet = qbSession.SendRequest(requestMsgSet);
            return ExtractItemFromQueryResponse(responseMsgSet);
        }

        /// <summary>
        /// Parses the response from an inventory item query
        /// and returns an Item object if found, otherwise null.
        /// </summary>
        private Item ExtractItemFromQueryResponse(IMsgSetResponse responseMsgSet)
        {
            if (responseMsgSet == null) return null;
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null || responseList.Count == 0) return null;

            IResponse response = responseList.GetAt(0);
            if (response.StatusCode != 0) return null; // If there's an error or the item isn't found, return null.

            // The response.Detail could be an IItemInventoryRetList or null
            if (response.Detail is IItemInventoryRetList itemInventoryRet)
            {
                // We expect only one match if we queried by ListID
                // But the QB SDK's "RetList" can hold multiple. We'll handle the first match.
                return ConvertItemInventoryRetToItem(itemInventoryRet);
            }

            return null;
        }

        /// <summary>
        /// Converts an IItemInventoryRetList to our Item model (taking the first record).
        /// Returns null if none found.
        /// </summary>
        private Item ConvertItemInventoryRetToItem(IItemInventoryRetList retList)
        {
            if (retList == null) return null;

            // Even though it's called "List", it usually only contains one item if we specified a single ListID.
            // But let's just handle the first item in the set.
            // The QuickBooks SDK can be a bit odd: there's no direct "List" object, but let's treat it carefully:
            IItemInventoryRet singleRet = retList; // This is how the QB FC 16 OSR structures the type

            if (singleRet == null)
                return null;

            var newItem = new Item(
                name: singleRet.Name?.GetValue() ?? string.Empty,
                salesPrice: (decimal?)(singleRet.SalesPrice?.GetValue()) ?? 0m,
                manufacturerPartNumber: singleRet.ManufacturerPartNumber?.GetValue() ?? string.Empty
            );

            newItem.QB_ID = singleRet.ListID?.GetValue();
            return newItem;
        }

        /// <summary>
        /// Helper to delete an item by ListID from QuickBooks. 
        /// This is optional, but helps keep test data clean.
        /// </summary>
        private static void DeleteItem(QuickBooksSession qbSession, string listID)
        {
            IMsgSetRequest requestMsgSet = qbSession.CreateRequestSet();
            IListDel listDelRq = requestMsgSet.AppendListDelRq();
            listDelRq.ListDelType.SetValue(ENListDelType.ldtItemInventory);
            listDelRq.ListID.SetValue(listID);

            IMsgSetResponse responseMsgSet = qbSession.SendRequest(requestMsgSet);
            ValidateDeleteResponse(responseMsgSet, listID);
        }

        private static void ValidateDeleteResponse(IMsgSetResponse responseMsgSet, string listID)
        {
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null || responseList.Count == 0)
                return;

            IResponse response = responseList.GetAt(0);
            if (response.StatusCode != 0)
            {
                throw new Exception(
                    $"Error Deleting Item (ListID: {listID}): {response.StatusMessage}. StatusCode: {response.StatusCode}"
                );
            }
        }
    }
}
