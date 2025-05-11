namespace QB_Items_Lib
{
    public class Item(string name, decimal salesPrice, string manufacturerPartNumber)
    {
        // Properties for the item
        public string Name { get; init; } = name;
        public decimal SalesPrice { get; init; } = salesPrice;
        public string ManufacturerPartNumber { get; init; } = manufacturerPartNumber;
        public string QB_ID { get; set; } = string.Empty;
    }
}