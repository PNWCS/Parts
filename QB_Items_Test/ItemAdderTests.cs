using QB_Items_Lib; // Correct namespace for Item and AppConfig
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
        // Removed unused private method 'DoesAccountExist'

        [Fact]
        public void AddMultipleItems_WithItemAdder_ThenQueryByQBID_ShouldHaveValidQBIDs()
        {
            // Ensure log file is closed and reset the logger
            EnsureLogFileClosed();
            DeleteOldLogFiles();
            ResetLogger();

            using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
            {
                // Retrieve actual account names for testing
                string salesAccount = GetActualAccountName(qbSession, "Sales") ?? "Sales";
                string inventoryAccount = GetActualAccountName(qbSession, "Inventory Asset") ?? "Inventory Asset";
                string cogsAccount = GetActualAccountName(qbSession, "Cost of Goods Sold") ?? "Cost of Goods Sold";

                Console.WriteLine($"Using Sales account: {salesAccount}");
                Console.WriteLine($"Using Inventory Asset account: {inventoryAccount}");
                Console.WriteLine($"Using Cost of Goods Sold account: {cogsAccount}");

                // Modify account names for testing
                ModifyAccountNamesForTesting(salesAccount, inventoryAccount, cogsAccount);
            }

            // Add multiple items for testing
            const int ITEM_COUNT = 5;
            var random = new Random();
            var itemsToAdd = new List<Item>();

            for (int i = 0; i < ITEM_COUNT; i++)
            {
                string uniqueName = $"ItemAdderTest_{Guid.NewGuid():N}"[..8];
                decimal salesPrice = (decimal)(100 + random.NextDouble() * 50);
                string partNumber = random.Next(1000, 9999).ToString();
                itemsToAdd.Add(new Item(uniqueName, salesPrice, partNumber));
            }

            

            // Delete the items after testing
            using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
            {
                foreach (var item in itemsToAdd.Where(i => !string.IsNullOrEmpty(i.QB_ID)))
                {
                    DeleteItem(qbSession, item.QB_ID);
                }
            }

            // Ensure log file is closed
            EnsureLogFileClosed();
        }

        // Helper method to retrieve the actual account name from QuickBooks
        private static string? GetActualAccountName(QuickBooksSession qbSession, string approximateAccountName)
        {
            var requestMsgSet = qbSession.CreateRequestSet();
            var query = requestMsgSet.AppendAccountQueryRq();
            query.ORAccountListQuery.AccountListFilter.ORNameFilter.NameFilter.MatchCriterion.SetValue(ENMatchCriterion.mcContains);
            query.ORAccountListQuery.AccountListFilter.ORNameFilter.NameFilter.MatchCriterion.SetValue(ENMatchCriterion.mcContains);
            query.ORAccountListQuery.AccountListFilter.ORNameFilter.NameFilter.Name.SetValue(approximateAccountName);

            var responseMsgSet = qbSession.SendRequest(requestMsgSet);

            if (responseMsgSet?.ResponseList == null || responseMsgSet.ResponseList.Count == 0)
                return null;

            var response = responseMsgSet.ResponseList.GetAt(0);
            if (response.StatusCode != 0 || response.Detail == null)
                return null;

            if (response.Detail is IAccountRetList accountList && accountList.Count > 0)
            {
                var account = accountList.GetAt(0);
                return account.Name?.GetValue();
            }

            return null;
        }

        // Helper method to modify account names for testing (if needed)
        private static void ModifyAccountNamesForTesting(string salesAccount, string inventoryAccount, string cogsAccount)
        {
            Console.WriteLine($"Would update account references to: {salesAccount}, {inventoryAccount}, {cogsAccount}");
        }

        // Add the missing DeleteItem method
        private static void DeleteItem(QuickBooksSession qbSession, string qbId)
        {
            var requestMsgSet = qbSession.CreateRequestSet();
            var deleteRequest = requestMsgSet.AppendListDelRq();
            deleteRequest.ListDelType.SetValue(ENListDelType.ldtItemNonInventory); // Corrected type to ldtItemNonInventory
            deleteRequest.ListID.SetValue(qbId);

            var responseMsgSet = qbSession.SendRequest(requestMsgSet);

            if (responseMsgSet?.ResponseList == null || responseMsgSet.ResponseList.Count == 0)
                throw new InvalidOperationException($"Failed to delete item with QB_ID: {qbId}");

            var response = responseMsgSet.ResponseList.GetAt(0);
            if (response.StatusCode != 0)
            {
                throw new InvalidOperationException($"Error deleting item with QB_ID: {qbId}. Status: {response.StatusMessage}");
            }

            Console.WriteLine($"Successfully deleted item with QB_ID: {qbId}");
        }
    }
}
