
﻿using System;
using QB_Items_Lib;
using QBFC16Lib;
using QB_Items_Lib; // Correct namespace for Item and AppConfig
using Serilog;
using System;
using QB_Items_Lib;
using QBFC16Lib;
 d65a978 (deleted requested folder)
 16bfe1f543f7eaf03d2c866dfb4e911722576bb2

namespace QB_Items_CLI
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("QB Items CLI - Command Line Interface for QuickBooks Item Management");
            Console.WriteLine("=====================================================================");

            if (args.Length == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  query                       - Query all items from QuickBooks");
                Console.WriteLine("  add [csvPath]               - Add items to QuickBooks from CSV");
                Console.WriteLine("  compare                     - Compare CSV data with QuickBooks and add missing items");
                return;
            }

            string command = args[0].ToLower();

            if (command == "query")
            {
                Console.WriteLine("Querying all items from QuickBooks...");
                var items = ItemReader.QueryAllItems();

<<<<<<< HEAD
                Console.WriteLine("\nItems from QuickBooks:");
                foreach (var item in items)
                {
                    Console.WriteLine($"- {item.Name} | SalesPrice: {item.SalesPrice} | ManufacturerPartNumber: {item.ManufacturerPartNumber}");
=======
<<<<<<< HEAD
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
=======
                Console.WriteLine("\nItems from QuickBooks:");
                foreach (var item in items)
                {
                    Console.WriteLine($"- {item.Name} | SalesPrice: {item.SalesPrice} | ManufacturerPartNumber: {item.ManufacturerPartNumber}");
>>>>>>> d65a978 (deleted requested folder)
>>>>>>> 16bfe1f543f7eaf03d2c866dfb4e911722576bb2
                }
            }
            else if (command == "add")
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: add [csvPath]");
                    return;
                }

                string csvPath = args[1];

                Console.WriteLine($"Adding items from {csvPath}...");
                var items = ItemComparator.ReadItemsFromCSV(csvPath);

                var sessionManager = new QBSessionManager();
                try
                {
                    sessionManager.OpenConnection("", AppConfig.QB_APP_NAME);
                    sessionManager.BeginSession("", ENOpenMode.omDontCare);

                    ItemAdder.AddItems(items, sessionManager);
                }
                finally
                {
                    sessionManager.EndSession();
                    sessionManager.CloseConnection();
                }

                Console.WriteLine("Items added successfully.");
            }
            else if (command == "compare")
            {
                string csvPath = @"C:\Users\SreekurmamN\comparator\Parts-new\Parts-new\TestItems.csv";

                try
                {
                    Console.WriteLine($"Comparing items between CSV and QuickBooks...");

                    var csvItems = ItemComparator.ReadItemsFromCSV(csvPath);
                    var comparisonResults = ItemComparator.CompareWithQuickBooks(csvItems);
                    ItemComparator.AddMissingItemsToQuickBooks(comparisonResults);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during comparison: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Unknown command.");
            }

            Console.WriteLine("\nProcess complete. Press any key to exit.");
            Console.ReadKey();
        }
    }
}


