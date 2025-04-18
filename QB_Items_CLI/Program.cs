using QB_Items_Lib;  // Import the Lib namespace
using Serilog;

namespace QB_Items_CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            // Configure logging
            LoggerConfig.ConfigureLogging();

            try
            {
                Log.Information("Starting QuickBooks item query...");

                // Query all items from QuickBooks using the ItemReader
                var items = ItemReader.QueryAllItems();

                if (items == null)
                {
                    Log.Warning("QueryAllItems returned null. No items fetched.");
                    Console.WriteLine("No items were retrieved.");
                }
                else if (!items.Any())  // Check if list is empty
                {
                    Log.Warning("QueryAllItems returned an empty list.");
                    Console.WriteLine("No items found.");
                }
                else
                {
                    // Log and display fetched items
                    foreach (var item in items)
                    {
                        Log.Information("Fetched Item: {Name}, Price: {Price}, Part#: {PartNumber}",
                            item.Name, item.SalesPrice, item.ManufacturerPartNumber);
                        Console.WriteLine($"Item Name: {item.Name}, SalesPrice: {item.SalesPrice}, ManufacturerPartNumber: {item.ManufacturerPartNumber}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while querying QuickBooks items.");
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                LoggerConfig.ResetLogger();
            }
        }
    }
}
