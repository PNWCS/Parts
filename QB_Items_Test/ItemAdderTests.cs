// ==========================
// File: ItemAdderTests.cs
// ==========================
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
            EnsureLogFileClosed();
            DeleteOldLogFiles();
            ResetLogger();

            const int ITEM_COUNT = 5;
            var random = new Random();
            var itemsToAdd = new List<Item>();
            for (int i = 0; i < ITEM_COUNT; i++)
            {
                string uniqueName = "ItemAdderTest_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                decimal salesPrice = Math.Round((decimal)(100 + random.NextDouble() * 50), 2);
                string partNumber = random.Next(1000, 9999).ToString();
                itemsToAdd.Add(new Item(uniqueName, salesPrice, partNumber));
            }

            ItemAdder.AddItems(itemsToAdd);

            foreach (var item in itemsToAdd)
            {
                Assert.False(string.IsNullOrWhiteSpace(item.QB_ID),
                             $"Expected QB_ID to be set for item '{item.Name}', but it was null/empty.");
            }

            using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
            {
                foreach (var addedItem in itemsToAdd)
                {
                    var queriedItem = QueryItemByListID(qbSession, addedItem.QB_ID!);
                    Assert.NotNull(queriedItem);
                    Assert.Equal(addedItem.Name, queriedItem!.Name);
                    Assert.Equal(addedItem.ManufacturerPartNumber, queriedItem.ManufacturerPartNumber);
                    Assert.Equal(decimal.Round(addedItem.SalesPrice, 2), decimal.Round(queriedItem.SalesPrice, 2));
                }
            }

            using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
            {
                foreach (var item in itemsToAdd.Where(i => !string.IsNullOrEmpty(i.QB_ID)))
                {
                    try
                    {
                        DeleteItem(qbSession, item.QB_ID!);
                    }
                    catch (Exception ex)
                    {
                        // Log instead of failing test: items might be in use in QB
                        Console.WriteLine($"⚠️ Could not delete item {item.Name} (ListID: {item.QB_ID}): {ex.Message}");
                    }
                }
            }

            EnsureLogFileClosed();
        }

        private Item? QueryItemByListID(QuickBooksSession qbSession, string listID)
        {
            IMsgSetRequest requestMsgSet = qbSession.CreateRequestSet();
            requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

            IItemInventoryQuery queryRq = requestMsgSet.AppendItemInventoryQueryRq();
            queryRq.ORListQueryWithOwnerIDAndClass.ListIDList.Add(listID);

            IMsgSetResponse responseMsgSet = qbSession.SendRequest(requestMsgSet);
            return ExtractItemFromQueryResponse(responseMsgSet);
        }

        private Item? ExtractItemFromQueryResponse(IMsgSetResponse responseMsgSet)
        {
            if (responseMsgSet == null) return null;
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null || responseList.Count == 0) return null;

            IResponse response = responseList.GetAt(0);
            if (response.StatusCode != 0) return null;

            if (response.Detail is IItemInventoryRetList itemInventoryRet)
            {
                return ConvertItemInventoryRetToItem(itemInventoryRet);
            }

            return null;
        }

        private Item? ConvertItemInventoryRetToItem(IItemInventoryRetList retList)
        {
            if (retList == null) return null;

            IItemInventoryRet singleRet = retList.GetAt(0);
            if (singleRet == null) return null;

            var newItem = new Item(
                name: singleRet.Name?.GetValue() ?? string.Empty,
                salesPrice: (decimal?)(singleRet.SalesPrice?.GetValue()) ?? 0m,
                manufacturerPartNumber: singleRet.ManufacturerPartNumber?.GetValue() ?? string.Empty
            );

            newItem.QB_ID = singleRet.ListID?.GetValue();
            return newItem;
        }

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
                    $"Error Deleting Item (ListID: {listID}): {response.StatusMessage}. StatusCode: {response.StatusCode}");
            }
        }
    }
}
