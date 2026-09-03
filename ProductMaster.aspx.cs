using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class ProductListItem
    {
        public int ProductID { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string ItemID { get; set; } = "";
        public string InventoryType { get; set; } = "";
        public string ProductGroupName { get; set; } = "";
        public string BrandCode { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class ProductInput
    {
        public int ProductID { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public int InventoryTypeID { get; set; }
        public string ItemID { get; set; } = "";
        public int SUUnitOfMeasureID { get; set; }
        public int PUUnitOfMeasureID { get; set; }
        public int IUUnitOfMeasureID { get; set; }
        public int ProductNatureID { get; set; }
        public int ItemRegisteredID { get; set; }
        public int BrandCodeID { get; set; }
        public int BrandGroupID { get; set; }
        public int ProductGroupID { get; set; }
        public int ProductSalesGroupID { get; set; }
        public int ItemGroupID { get; set; }
        public int SalesCategoryID { get; set; }
        public int ProductDivisionID { get; set; }
        public int ProductTeamID { get; set; }
        public int HSCodeID { get; set; }
        public string SellingPrice { get; set; } = "";
    }

    public partial class ProductMasterPage : AppBasePage
    {
        public string PageTitle => "Product Master";
        public List<ProductListItem> Products { get; set; } = new List<ProductListItem>();
        public ProductInput Input { get; set; } = new ProductInput();
        public List<LookupItem> ProductNatures { get; set; } = new List<LookupItem>();
        public List<LookupItem> ItemRegisteredList { get; set; } = new List<LookupItem>();
        public List<LookupItem> BrandCodes { get; set; } = new List<LookupItem>();
        public List<LookupItem> BrandGroups { get; set; } = new List<LookupItem>();
        public List<LookupItem> ProductGroups { get; set; } = new List<LookupItem>();
        public List<LookupItem> SalesGroups { get; set; } = new List<LookupItem>();
        public List<LookupItem> ItemGroups { get; set; } = new List<LookupItem>();
        public List<LookupItem> SalesCategories { get; set; } = new List<LookupItem>();
        public List<LookupItem> Divisions { get; set; } = new List<LookupItem>();
        public List<LookupItem> Teams { get; set; } = new List<LookupItem>();
        public List<LookupItem> HSCodes { get; set; } = new List<LookupItem>();
        public List<LookupItem> InventoryTypes { get; set; } = new List<LookupItem>();
        public List<LookupItem> UnitOfMeasures { get; set; } = new List<LookupItem>();
        public bool EditMode { get; set; }
        public bool ShowForm { get; set; }
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "Save";
                if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostDelete(int.TryParse(Request.Form["deleteId"], out var d) ? d : 0);
                    return;
                }
                OnPostSave();
                return;
            }

            var newProduct = Request.QueryString["newProduct"] == "1" || Request.QueryString["newProduct"] == "true";
            OnGet(QueryInt("editId"), newProduct);
        }

        private void OnGet(int? editId, bool newProduct)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            ShowForm = (editId.HasValue && editId > 0) || newProduct;

            if (ShowForm)
            {
                LoadLookups();
                if (editId.HasValue && editId > 0)
                {
                    LoadForEdit(editId.Value);
                    EditMode = true;
                }
                else
                {
                    Input.ProductCode = GenerateNextProductCode();
                }
            }
            else
            {
                LoadProducts();
            }
        }

        private void OnPostSave()
        {
            EditMode = FormBool("EditMode");
            Input = new ProductInput
            {
                ProductID = int.TryParse(Request.Form["ProductID"], out var pid) ? pid : 0,
                ProductName = FormString("ProductName"),
                InventoryTypeID = int.TryParse(Request.Form["InventoryTypeID"], out var a) ? a : 0,
                ItemID = FormString("ItemID"),
                SUUnitOfMeasureID = int.TryParse(Request.Form["SUUnitOfMeasureID"], out var b) ? b : 0,
                PUUnitOfMeasureID = int.TryParse(Request.Form["PUUnitOfMeasureID"], out var c) ? c : 0,
                IUUnitOfMeasureID = int.TryParse(Request.Form["IUUnitOfMeasureID"], out var d) ? d : 0,
                ProductNatureID = int.TryParse(Request.Form["ProductNatureID"], out var e) ? e : 0,
                ItemRegisteredID = int.TryParse(Request.Form["ItemRegisteredID"], out var f) ? f : 0,
                BrandCodeID = int.TryParse(Request.Form["BrandCodeID"], out var g) ? g : 0,
                BrandGroupID = int.TryParse(Request.Form["BrandGroupID"], out var h) ? h : 0,
                ProductGroupID = int.TryParse(Request.Form["ProductGroupID"], out var i) ? i : 0,
                ProductSalesGroupID = int.TryParse(Request.Form["ProductSalesGroupID"], out var j) ? j : 0,
                ItemGroupID = int.TryParse(Request.Form["ItemGroupID"], out var k) ? k : 0,
                SalesCategoryID = int.TryParse(Request.Form["SalesCategoryID"], out var l) ? l : 0,
                ProductDivisionID = int.TryParse(Request.Form["ProductDivisionID"], out var m) ? m : 0,
                ProductTeamID = int.TryParse(Request.Form["ProductTeamID"], out var n) ? n : 0,
                HSCodeID = int.TryParse(Request.Form["HSCodeID"], out var o) ? o : 0,
                SellingPrice = FormString("SellingPrice"),
                IsActive = FormBool("IsActive")
            };

            if (string.IsNullOrWhiteSpace(Input.ProductName))
            {
                SetFormError("Product name is required.");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    SaveRecord(conn, Input, EditMode);
                }
                SetAlert(EditMode ? "Product updated successfully." : "Product created successfully.");
                Response.Redirect("~/ProductMaster.aspx?editId=" + Input.ProductID);
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                SetFormError("A product with this ID or duplicate value already exists.");
            }
            catch (Exception ex)
            {
                SetFormError("Error: " + ex.Message);
            }
        }

        private void OnPostDelete(int deleteId)
        {
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
UPDATE tblProduct SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
WHERE ProductID = @ProductID;", conn))
                {
                    cmd.Parameters.AddWithValue("@ProductID", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Product removed successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error removing product: " + ex.Message, "error");
            }
            Response.Redirect("~/ProductMaster.aspx");
        }

        private void SetFormError(string message)
        {
            AlertMessage = message;
            AlertType = "error";
            LoadLookups();
            ShowForm = true;
            if (!EditMode)
                Input.ProductCode = GenerateNextProductCode();
        }

        private void SaveRecord(SqlConnection conn, ProductInput input, bool editMode)
        {
            if (editMode && input.ProductID > 0)
            {
                using (var cmd = new SqlCommand(@"
UPDATE tblProduct SET ProductName=@ProductName, IsActive=@IsActive, InventoryTypeID=@InventoryTypeID,
  ItemID=@ItemID, SUUnitOfMeasureID=@SUUnitOfMeasureID, PUUnitOfMeasureID=@PUUnitOfMeasureID,
  IUUnitOfMeasureID=@IUUnitOfMeasureID, ProductNatureID=@ProductNatureID, ItemRegisteredID=@ItemRegisteredID,
  BrandCodeID=@BrandCodeID, BrandGroupID=@BrandGroupID, ProductGroupID=@ProductGroupID,
  ProductSalesGroupID=@ProductSalesGroupID, ItemGroupID=@ItemGroupID, SalesCategoryID=@SalesCategoryID,
  ProductDivisionID=@ProductDivisionID, ProductTeamID=@ProductTeamID, HSCodeID=@HSCodeID,
  SellingPrice=@SellingPrice, ModifiedOn=GETDATE(), ModifiedByUserID=@ModifiedByUserID
WHERE ProductID=@ProductID;", conn))
                {
                    BindParams(cmd, input);
                    cmd.Parameters.AddWithValue("@ProductID", input.ProductID);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    cmd.ExecuteNonQuery();
                }
                return;
            }

            input.ProductCode = GenerateNextProductCode(conn);
            using (var ins = new SqlCommand(@"
INSERT INTO tblProduct
 (ProductCode, ProductName, IsActive, InventoryTypeID, ItemID,
  SUUnitOfMeasureID, PUUnitOfMeasureID, IUUnitOfMeasureID,
  ProductNatureID, ItemRegisteredID, BrandCodeID, BrandGroupID, ProductGroupID,
  ProductSalesGroupID, ItemGroupID, SalesCategoryID, ProductDivisionID, ProductTeamID, HSCodeID,
  SellingPrice, CreatedOn, CreatedByUserID)
VALUES
 (@ProductCode, @ProductName, @IsActive, @InventoryTypeID, @ItemID,
  @SUUnitOfMeasureID, @PUUnitOfMeasureID, @IUUnitOfMeasureID,
  @ProductNatureID, @ItemRegisteredID, @BrandCodeID, @BrandGroupID, @ProductGroupID,
  @ProductSalesGroupID, @ItemGroupID, @SalesCategoryID, @ProductDivisionID, @ProductTeamID, @HSCodeID,
  @SellingPrice, GETDATE(), @CreatedByUserID);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn))
            {
                BindParams(ins, input);
                ins.Parameters.AddWithValue("@ProductCode", input.ProductCode);
                AuditHelper.AddCreatedBy(ins, Auth.CurrentUserId);
                input.ProductID = (int)ins.ExecuteScalar();
            }
        }

        private static void BindParams(SqlCommand cmd, ProductInput input)
        {
            cmd.Parameters.AddWithValue("@ProductName", input.ProductName);
            cmd.Parameters.AddWithValue("@IsActive", input.IsActive);
            cmd.Parameters.AddWithValue("@InventoryTypeID", Fk(input.InventoryTypeID));
            cmd.Parameters.AddWithValue("@ItemID", string.IsNullOrWhiteSpace(input.ItemID) ? (object)DBNull.Value : input.ItemID);
            cmd.Parameters.AddWithValue("@SUUnitOfMeasureID", Fk(input.SUUnitOfMeasureID));
            cmd.Parameters.AddWithValue("@PUUnitOfMeasureID", Fk(input.PUUnitOfMeasureID));
            cmd.Parameters.AddWithValue("@IUUnitOfMeasureID", Fk(input.IUUnitOfMeasureID));
            cmd.Parameters.AddWithValue("@ProductNatureID", Fk(input.ProductNatureID));
            cmd.Parameters.AddWithValue("@ItemRegisteredID", Fk(input.ItemRegisteredID));
            cmd.Parameters.AddWithValue("@BrandCodeID", Fk(input.BrandCodeID));
            cmd.Parameters.AddWithValue("@BrandGroupID", Fk(input.BrandGroupID));
            cmd.Parameters.AddWithValue("@ProductGroupID", Fk(input.ProductGroupID));
            cmd.Parameters.AddWithValue("@ProductSalesGroupID", Fk(input.ProductSalesGroupID));
            cmd.Parameters.AddWithValue("@ItemGroupID", Fk(input.ItemGroupID));
            cmd.Parameters.AddWithValue("@SalesCategoryID", Fk(input.SalesCategoryID));
            cmd.Parameters.AddWithValue("@ProductDivisionID", Fk(input.ProductDivisionID));
            cmd.Parameters.AddWithValue("@ProductTeamID", Fk(input.ProductTeamID));
            cmd.Parameters.AddWithValue("@HSCodeID", Fk(input.HSCodeID));
            cmd.Parameters.AddWithValue("@SellingPrice", DecimalOrNull(input.SellingPrice));
        }

        private static object DecimalOrNull(string value) =>
            decimal.TryParse(value, out var d) ? (object)d : DBNull.Value;

        private static object Fk(int id) => id > 0 ? (object)id : DBNull.Value;

        private string GenerateNextProductCode(SqlConnection conn = null)
        {
            var owns = conn == null;
            if (owns) { conn = new SqlConnection(Conn); conn.Open(); }
            try
            {
                using (var cmd = new SqlCommand(@"
SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(ProductCode, 4, 10) AS INT)), 0)
FROM tblProduct WHERE ProductCode LIKE 'PRD[0-9]%';", conn))
                {
                    var next = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
                    return "PRD" + next.ToString("D6");
                }
            }
            finally { if (owns) conn.Dispose(); }
        }

        private void LoadProducts()
        {
            Products.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT p.ProductID, p.ProductCode, p.ProductName, ISNULL(p.ItemID, ''),
       ISNULL(it.InventoryTypeName, ''), ISNULL(pg.ProductGroupName, ''),
       ISNULL(bc.BrandCode, ''), p.IsActive
FROM tblProduct p
LEFT JOIN tblInventoryType it ON it.InventoryTypeID = p.InventoryTypeID
LEFT JOIN tblProductGroup pg ON pg.ProductGroupID = p.ProductGroupID
LEFT JOIN tblBrandCode bc ON bc.BrandCodeID = p.BrandCodeID
ORDER BY p.IsActive DESC, p.ProductCode;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Products.Add(new ProductListItem
                        {
                            ProductID = dr.GetInt32(0),
                            ProductCode = dr.GetString(1),
                            ProductName = dr.GetString(2),
                            ItemID = dr.GetString(3),
                            InventoryType = dr.GetString(4),
                            ProductGroupName = dr.GetString(5),
                            BrandCode = dr.GetString(6),
                            IsActive = dr.GetBoolean(7)
                        });
                    }
                }
            }
        }

        private void LoadForEdit(int productId)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT ProductID, ProductCode, ProductName, IsActive, InventoryTypeID, ItemID,
       SUUnitOfMeasureID, PUUnitOfMeasureID, IUUnitOfMeasureID,
       ProductNatureID, ItemRegisteredID, BrandCodeID, BrandGroupID, ProductGroupID,
       ProductSalesGroupID, ItemGroupID, SalesCategoryID, ProductDivisionID, ProductTeamID, HSCodeID,
       SellingPrice
FROM tblProduct WHERE ProductID = @ProductID;", conn))
            {
                cmd.Parameters.AddWithValue("@ProductID", productId);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return;
                    Input = new ProductInput
                    {
                        ProductID = dr.GetInt32(0),
                        ProductCode = dr.GetString(1),
                        ProductName = dr.GetString(2),
                        IsActive = dr.GetBoolean(3),
                        InventoryTypeID = dr.IsDBNull(4) ? 0 : dr.GetInt32(4),
                        ItemID = dr.IsDBNull(5) ? "" : dr.GetString(5),
                        SUUnitOfMeasureID = dr.IsDBNull(6) ? 0 : dr.GetInt32(6),
                        PUUnitOfMeasureID = dr.IsDBNull(7) ? 0 : dr.GetInt32(7),
                        IUUnitOfMeasureID = dr.IsDBNull(8) ? 0 : dr.GetInt32(8),
                        ProductNatureID = dr.IsDBNull(9) ? 0 : dr.GetInt32(9),
                        ItemRegisteredID = dr.IsDBNull(10) ? 0 : dr.GetInt32(10),
                        BrandCodeID = dr.IsDBNull(11) ? 0 : dr.GetInt32(11),
                        BrandGroupID = dr.IsDBNull(12) ? 0 : dr.GetInt32(12),
                        ProductGroupID = dr.IsDBNull(13) ? 0 : dr.GetInt32(13),
                        ProductSalesGroupID = dr.IsDBNull(14) ? 0 : dr.GetInt32(14),
                        ItemGroupID = dr.IsDBNull(15) ? 0 : dr.GetInt32(15),
                        SalesCategoryID = dr.IsDBNull(16) ? 0 : dr.GetInt32(16),
                        ProductDivisionID = dr.IsDBNull(17) ? 0 : dr.GetInt32(17),
                        ProductTeamID = dr.IsDBNull(18) ? 0 : dr.GetInt32(18),
                        HSCodeID = dr.IsDBNull(19) ? 0 : dr.GetInt32(19),
                        SellingPrice = dr.IsDBNull(20) ? "" : dr.GetDecimal(20).ToString("0.####")
                    };
                }
            }
        }

        private void LoadLookups()
        {
            ProductNatures = LoadLookup("tblProductNature", "ProductNatureID", "ProductNatureName");
            ItemRegisteredList = LoadLookup("tblItemRegistered", "ItemRegisteredID", "ItemRegisteredName");
            BrandCodes = LoadBrandCodeLookup();
            BrandGroups = LoadLookup("tblBrandGroup", "BrandGroupID", "BrandGroupName");
            ProductGroups = LoadLookup("tblProductGroup", "ProductGroupID", "ProductGroupName");
            SalesGroups = LoadLookup("tblProductSalesGroup", "ProductSalesGroupID", "ProductSalesGroupName");
            ItemGroups = LoadLookup("tblItemGroup", "ItemGroupID", "ItemGroupName");
            SalesCategories = LoadLookup("tblSalesCategory", "SalesCategoryID", "SalesCategoryName");
            Divisions = LoadLookup("tblProductDivision", "ProductDivisionID", "ProductDivisionName");
            Teams = LoadLookup("tblProductTeam", "ProductTeamID", "ProductTeamName");
            HSCodes = LoadHSCodeLookup();
            InventoryTypes = LoadLookup("tblInventoryType", "InventoryTypeID", "InventoryTypeName");
            UnitOfMeasures = LoadLookup("tblUnitOfMeasure", "UnitOfMeasureID", "UnitOfMeasureName");
        }

        private List<LookupItem> LoadLookup(string table, string idCol, string nameCol)
        {
            var items = new List<LookupItem>();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(
                "SELECT " + idCol + ", " + nameCol + " FROM " + table + " WHERE IsActive=1 ORDER BY " + nameCol + ";", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        items.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) });
            }
            return items;
        }

        private List<LookupItem> LoadBrandCodeLookup()
        {
            var items = new List<LookupItem>();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT BrandCodeID, BrandCode, BrandName FROM tblBrandCode WHERE IsActive=1 ORDER BY BrandCode;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        items.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) + " – " + dr.GetString(2) });
            }
            return items;
        }

        private List<LookupItem> LoadHSCodeLookup()
        {
            var items = new List<LookupItem>();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT HSCodeID, HSCode, HSCodeDescription FROM tblHSCode WHERE IsActive=1 ORDER BY HSCode;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        items.Add(new LookupItem { Id = dr.GetInt32(0), Name = dr.GetString(1) + " – " + dr.GetString(2) });
            }
            return items;
        }
    }
}
