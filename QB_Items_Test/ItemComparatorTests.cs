using System.Diagnostics;
using Serilog;
using QB_Items_Lib;          // Item, ItemStatus, ItemsComparator, ItemsAdder, ItemsReader
using QB_Vendors_Lib;        // Vendor model / adder
using QBFC16Lib;             // QuickBooks Desktop SDK
using static QB_Items_Test.CommonMethods;   // Re-use helpers for logs, etc.

namespace QB_Items_Test
{
    [Collection("Sequential Tests")]   // Prevent parallel QB access
    public class ItemComparatorTests
    {
        [Fact]
        public void CompareItems_InMemoryScenario_And_Verify_Logs()
        {
            const int NUM_ITEMS       = 5;
            const decimal BASE_PRICE  = 10m;

            // ── 1.  Make sure logging is clean ──────────────────────────────────────
            EnsureLogFileClosed();
            DeleteOldLogFiles();
            ResetLogger();

            // ── 2.  Create a temporary vendor (preferred-vendor field) ──────────────
            var tempVendor = new Vendor(
                name:  "TmpVend_" + Guid.NewGuid().ToString("N")[..8],
                companyName: "Temp Company");

            VendorAdder.AddVendors(new() { tempVendor });

            // ── 3.  Build five brand-new inventory items in memory ──────────────────
            var initialItems = new List<Item>();
            for (int i = 0; i < NUM_ITEMS; i++)
            {
                initialItems.Add(new Item(
                    name: $"TestItem_{Guid.NewGuid():N}".Substring(0, 12),
                    salesPrice: BASE_PRICE + i,
                    manufacturerPartNumber: $"MPN_{Guid.NewGuid():N}".Substring(0, 10))
                {
                    // If your Item model exposes PreferredVendor, set it here
                    // PreferredVendor = tempVendor.Name
                });
            }

            List<Item> firstCompareResult  = new();
            List<Item> secondCompareResult = new();

            try
            {
                // ── 4-A.  First compare – expect every item to be Added ────────────
                firstCompareResult = ItemsComparator.CompareItems(initialItems);

                foreach (var itm in firstCompareResult.Where(x => initialItems.Any(y => y.Name == x.Name)))
                    Assert.Equal(ItemStatus.Added, itm.Status);

                // ── 4-B.  Mutate the in-memory list to trigger all other statuses ──
                var updatedItems  = new List<Item>(initialItems);

                var removedItem   = updatedItems[0];          //  Missing
                var renamedItem   = updatedItems[1];          //  Different
                updatedItems.Remove(removedItem);
                renamedItem.Name += "_MOD";

                // ── 4-C.  Second compare – expect Missing / Different / Unchanged ─
                secondCompareResult = ItemsComparator.CompareItems(updatedItems);
                var resultDict = secondCompareResult.ToDictionary(x => x.QB_ID ?? x.Name);

                // Missing
                Assert.True(resultDict.ContainsKey(removedItem.QB_ID ?? removedItem.Name));
                Assert.Equal(ItemStatus.Missing, resultDict[removedItem.QB_ID ?? removedItem.Name].Status);

                // Different
                Assert.True(resultDict.ContainsKey(renamedItem.QB_ID ?? renamedItem.Name));
                Assert.Equal(ItemStatus.Different, resultDict[renamedItem.QB_ID ?? renamedItem.Name].Status);

                // Unchanged (all the rest)
                foreach (var itm in updatedItems.Except(new[] { renamedItem }))
                    Assert.Equal(ItemStatus.Unchanged, resultDict[itm.QB_ID ?? itm.Name].Status);
            }
            finally
            {
                // ── 5.  Clean up:  delete items first, then vendor ─────────────────
                var allAddedItems = firstCompareResult
                                    .Where(i => !string.IsNullOrEmpty(i.QB_ID))
                                    .Select(i => i.QB_ID!)
                                    .ToList();

                using var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME);

                foreach (var listId in allAddedItems)
                    DeleteInventoryItem(qbSession, listId);

                // Delete the temporary vendor
                if (!string.IsNullOrEmpty(tempVendor.QB_ID))
                    DeleteVendor(qbSession, tempVendor.QB_ID);
            }

            // ── 6.  Flush & inspect Serilog output ─────────────────────────────────
            EnsureLogFileClosed();
            string logFile = GetLatestLogFile();
            EnsureLogFileExists(logFile);

            string logs = File.ReadAllText(logFile);
            Assert.Contains("ItemsComparator Initialized", logs);
            Assert.Contains("ItemsComparator Completed",  logs);

            foreach (var itm in firstCompareResult.Concat(secondCompareResult))
                Assert.Contains($"Item {itm.Name} is {itm.Status}.", logs);
        }

        // ===== Helper methods to remove data from QB ===============================

        private void DeleteInventoryItem(QuickBooksSession session, string listID)
        {
            IMsgSetRequest rq = session.CreateRequestSet();
            IListDel delRq   = rq.AppendListDelRq();
            delRq.ListDelType.SetValue(ENListDelType.ldtItemInventory);
            delRq.ListID.SetValue(listID);

            IMsgSetResponse rs = session.SendRequest(rq);
            WalkListDelResponse(rs, listID, "Inventory Item");
        }

        private void DeleteVendor(QuickBooksSession session, string listID)
        {
            IMsgSetRequest rq = session.CreateRequestSet();
            IListDel delRq   = rq.AppendListDelRq();
            delRq.ListDelType.SetValue(ENListDelType.ldtVendor);
            delRq.ListID.SetValue(listID);

            IMsgSetResponse rs = session.SendRequest(rq);
            WalkListDelResponse(rs, listID, "Vendor");
        }

        private void WalkListDelResponse(IMsgSetResponse rs, string listID, string entityName)
        {
            if (rs?.ResponseList?.Count > 0)
            {
                IResponse resp = rs.ResponseList.GetAt(0);
                if (resp.StatusCode == 0)
                    Debug.WriteLine($"✔ Deleted {entityName} (ListID: {listID}).");
                else
                    Debug.WriteLine($"✖ Error deleting {entityName}: {resp.StatusMessage}");
            }
        }
    }
}
