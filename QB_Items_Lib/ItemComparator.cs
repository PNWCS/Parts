using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QBFC16Lib;

namespace QB_Items_Lib
{
    public enum ItemStatus
    {
        MATCHED,
        MISSING_IN_QB,
        CONFLICT,
        MISSING_IN_CSV
    }

    public class ItemComparisonResult
    {
        public Item CsvItem { get; set; } = new Item("", 0, "");
        public Item? QuickBooksItem { get; set; }
        public ItemStatus Status { get; set; }
    }

    public static class ItemComparator
    {
        public static List<Item> ReadItemsFromCSV(string filePath)
        {
            var items = new List<Item>();
            var lines = File.ReadAllLines(filePath);

            Console.WriteLine($"Read {lines.Length} lines from CSV file.");

            for (int i = 1; i < lines.Length; i++) // Skip header
            {
                var parts = lines[i].Split(',');

                if (parts.Length >= 3)
                {
                    string name = parts[0].Trim();
                    if (decimal.TryParse(parts[1].Trim(), out var salesPrice))
                    {
                        string manufacturerPartNumber = parts[2].Trim();
                        items.Add(new Item(name, salesPrice, manufacturerPartNumber));
                    }
                    else
                    {
                        Console.WriteLine($"Warning: SalesPrice parse failed at line {i + 1}");
                    }
                }
            }

            Console.WriteLine($"Found {items.Count} items in CSV file.");
            return items;
        }

        public static List<ItemComparisonResult> CompareWithQuickBooks(List<Item> csvItems)
        {
            var qbItems = ItemReader.QueryAllItems();
            var results = new List<ItemComparisonResult>();

            Console.WriteLine($"Reading items from QuickBooks...");
            Console.WriteLine($"Found {qbItems.Count} items in QuickBooks.\n");

            Console.WriteLine("QuickBooks Items:");
            foreach (var item in qbItems)
            {
                Console.WriteLine($"  - {item.Name} | ${item.SalesPrice} | Part#: {item.ManufacturerPartNumber}");
            }

            var csvDict = csvItems.ToDictionary(i => i.Name.Trim(), StringComparer.OrdinalIgnoreCase);
            var qbDict = qbItems.ToDictionary(i => i.Name.Trim(), StringComparer.OrdinalIgnoreCase);

            Console.WriteLine("\nComparison Results:");
            Console.WriteLine("-------------------");

            foreach (var csvItem in csvItems)
            {
                if (qbDict.TryGetValue(csvItem.Name.Trim(), out var qbItem))
                {
                    if (ItemsAreEqual(csvItem, qbItem))
                    {
                        results.Add(new ItemComparisonResult { CsvItem = csvItem, QuickBooksItem = qbItem, Status = ItemStatus.MATCHED });
                        Console.WriteLine($"{csvItem.Name}: MATCHED");
                    }
                    else
                    {
                        results.Add(new ItemComparisonResult { CsvItem = csvItem, QuickBooksItem = qbItem, Status = ItemStatus.CONFLICT });
                        Console.WriteLine($"{csvItem.Name}: CONFLICT");
                    }
                }
                else
                {
                    results.Add(new ItemComparisonResult { CsvItem = csvItem, QuickBooksItem = null, Status = ItemStatus.MISSING_IN_QB });
                    Console.WriteLine($"{csvItem.Name}: MISSING_IN_QB");
                }
            }

            foreach (var qbItem in qbItems)
            {
                if (!csvDict.ContainsKey(qbItem.Name.Trim()))
                {
                    Console.WriteLine($"{qbItem.Name}: MISSING_IN_CSV");
                    results.Add(new ItemComparisonResult { CsvItem = new Item(qbItem.Name, qbItem.SalesPrice, qbItem.ManufacturerPartNumber), QuickBooksItem = qbItem, Status = ItemStatus.MISSING_IN_CSV });
                }
            }

            return results;
        }

        private static bool ItemsAreEqual(Item i1, Item i2)
        {
            return string.Equals(i1.Name.Trim(), i2.Name.Trim(), StringComparison.OrdinalIgnoreCase)
                && i1.SalesPrice == i2.SalesPrice
                && string.Equals(i1.ManufacturerPartNumber.Trim(), i2.ManufacturerPartNumber.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public static void AddMissingItemsToQuickBooks(List<ItemComparisonResult> results)
        {
            var missing = results.Where(r => r.Status == ItemStatus.MISSING_IN_QB).Select(r => r.CsvItem).ToList();

            if (missing.Count > 0)
            {
                Console.WriteLine($"\n{missing.Count} items missing in QuickBooks.");
                Console.Write("Do you want to add these items to QuickBooks? (Y/N): ");
                var key = Console.ReadKey();
                Console.WriteLine();

                if (key.Key == ConsoleKey.Y)
                {
                    var sessionManager = new QBSessionManager();
                    try
                    {
                        sessionManager.OpenConnection("", AppConfig.QB_APP_NAME);
                        sessionManager.BeginSession("", ENOpenMode.omDontCare);
                        ItemAdder.AddItems(missing, sessionManager);
                    }
                    finally
                    {
                        sessionManager.EndSession();
                        sessionManager.CloseConnection();
                    }

                    Console.WriteLine("Missing items added to QuickBooks.");
                }
                else
                {
                    Console.WriteLine("Addition cancelled.");
                }
            }
            else
            {
                Console.WriteLine("\nNo missing items to add.");
            }
        }
    }
}
