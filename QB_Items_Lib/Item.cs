namespace QB_Items_Lib
{
    public class Item
    {
        // Properties for the item
        public string Name { get; set; }
        public decimal SalesPrice { get; set; }
        public string ManufacturerPartNumber { get; set; }
        public string QB_ID { get; set; } // Represents the QB ListID

        // Constructor to initialize the Item
        public Item(string name, decimal salesPrice, string manufacturerPartNumber)
        {
            Name = name;
            SalesPrice = salesPrice;
            ManufacturerPartNumber = manufacturerPartNumber;
        }
    }
}
