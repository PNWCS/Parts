using QB_Items_Lib; // Correct namespace for Item and AppConfig
using Serilog;

namespace QB_Items_CLI
{
    class Program
    {
        static void Main()
        {
            // Configure logging
            LoggerConfig.ConfigureLogging();

            try
            {
                Log.Information("Starting QuickBooks item query...");

                // Query all items from QuickBooks using the ItemReader
                var items = ItemReader.QueryAllItems();

                if (items == null || items.Count == 0) // Prefer Count over Any() for clarity and performance
                {
                    Log.Warning("No items were retrieved from QuickBooks.");
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


