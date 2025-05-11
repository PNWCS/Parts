using QB_Items_Lib;
using Xunit;

namespace QB_Items_Test
{
    public class ComparatorTest
    {
        private void CompareItems(Item expected, Item actual)
        {
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.SalesPrice, actual.SalesPrice);
            Assert.Equal(expected.ManufacturerPartNumber, actual.ManufacturerPartNumber);
            Assert.Equal(expected.QB_ID, actual.QB_ID);
        }

        [Fact]
        public void TestItemComparison()
        {
            var item1 = new Item("ItemA", 99.99M, "ABC123")
            {
                QB_ID = "12345"
            };
            var item2 = new Item("ItemA", 99.99M, "ABC123")
            {
                QB_ID = "12345"
            };
            CompareItems(item1, item2);
        }
    }
}
