using QB_Items_Lib;
using Serilog;
using System;
using System.Collections.Generic;

namespace QB_Items_CLI
{
    class Program
    {
        static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("cli-log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var items = new List<Item>
            {
                new Item("SampleItem1", 150, "MPN150"),
                new Item("SampleItem2", 220, "MPN220")
            };

            ItemAdder.AddItems(items);
            Log.Information("Add operation complete.");
        }
    }
}
