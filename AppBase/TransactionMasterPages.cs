namespace HRMS
{
    public class InventoryMasterPage : SimpleListMasterPage
    {
        public override string PageTitle => "Inventory Master";
        public override string ListSql => @"SELECT TOP 500 t.InventoryTransactionID AS ID, p.ProductCode, p.ProductName, t.TransactionType, t.Quantity, t.TransactionDate
FROM tblInventoryTransaction t LEFT JOIN tblProduct p ON p.ProductID=t.ProductID ORDER BY t.InventoryTransactionID DESC;";
    }
}
