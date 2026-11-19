using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

using CustomerInput = global::HRMS.CustomerInput;
using VendorInput = global::HRMS.VendorInput;
using ProductInput = global::HRMS.ProductInput;

namespace HRMS.Services
{
    public class MasterProfileCompletionService
    {
        public const decimal CategoryWeight = 20m;
        private readonly string _conn;

        public MasterProfileCompletionService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public EmployeeProfileCompletionScore CalculateCustomer(CustomerInput input)
        {
            var c = input ?? new CustomerInput();
            return Assemble(
                Category("Identity",
                    HasText(c.CustomerCode),
                    HasText(c.Name),
                    HasText(c.SearchName),
                    HasText(c.CNIC),
                    HasText(c.NTN)),
                Category("Location",
                    c.DealForBranchID > 0,
                    c.CityID > 0,
                    c.ProvinceID > 0,
                    c.ModeOfDeliveryID > 0),
                Category("Classification",
                    c.CustomerGroupID > 0,
                    c.CustomerClassID > 0,
                    c.MethodOfPaymentID > 0,
                    c.TermsOfPaymentID > 0,
                    c.CurrencyID > 0),
                Category("Compliance",
                    c.BillPreferenceID > 0,
                    c.FBRStatusID > 0,
                    c.TaxGroupID > 0),
                Category("Commercial",
                    HasNumber(c.TotalBusinessPotential),
                    HasNumber(c.TargetBusinessSharePercent),
                    HasNumber(c.TargetBusinessAmount),
                    HasNumber(c.CreditLimit),
                    HasNumber(c.AHDCreditLimit),
                    HasNumber(c.PHDCreditLimit),
                    HasNumber(c.HHDCreditLimit)));
        }

        public EmployeeProfileCompletionScore CalculateVendor(VendorInput input)
        {
            var v = input ?? new VendorInput();
            return Assemble(
                Category("Identity",
                    HasText(v.VendorCode),
                    HasText(v.Name),
                    HasText(v.SearchName),
                    HasText(v.CNIC),
                    HasText(v.NTN)),
                Category("Location",
                    v.DealForBranchID > 0,
                    v.CityID > 0,
                    v.ProvinceID > 0,
                    v.ModeOfDeliveryID > 0),
                Category("Classification",
                    v.CustomerGroupID > 0,
                    v.CustomerClassID > 0,
                    v.MethodOfPaymentID > 0,
                    v.TermsOfPaymentID > 0,
                    v.CurrencyID > 0),
                Category("Compliance",
                    v.BillPreferenceID > 0,
                    v.FBRStatusID > 0,
                    v.TaxGroupID > 0),
                Category("Commercial",
                    HasNumber(v.TotalBusinessPotential),
                    HasNumber(v.TargetBusinessSharePercent),
                    HasNumber(v.TargetBusinessAmount),
                    HasNumber(v.CreditLimit),
                    HasNumber(v.AHDCreditLimit),
                    HasNumber(v.PHDCreditLimit),
                    HasNumber(v.HHDCreditLimit)));
        }

        public EmployeeProfileCompletionScore CalculateProduct(ProductInput input)
        {
            var p = input ?? new ProductInput();
            return Assemble(
                Category("Identity",
                    HasText(p.ProductCode),
                    HasText(p.ProductName),
                    HasText(p.ItemID),
                    HasNumber(p.SellingPrice)),
                Category("Units",
                    p.InventoryTypeID > 0,
                    p.SUUnitOfMeasureID > 0,
                    p.PUUnitOfMeasureID > 0,
                    p.IUUnitOfMeasureID > 0),
                Category("Brand",
                    p.ProductNatureID > 0,
                    p.ItemRegisteredID > 0,
                    p.BrandCodeID > 0,
                    p.BrandGroupID > 0),
                Category("Grouping",
                    p.ProductGroupID > 0,
                    p.ProductSalesGroupID > 0,
                    p.ItemGroupID > 0,
                    p.SalesCategoryID > 0),
                Category("Organization",
                    p.ProductDivisionID > 0,
                    p.ProductTeamID > 0,
                    p.HSCodeID > 0,
                    HasText(p.GTINNumber)));
        }

        public Dictionary<int, EmployeeProfileCompletionScore> ScoreCustomers(IList<int> ids)
        {
            return ScoreParty("tblCustomer", "CustomerID", ids, MapCustomer);
        }

        public Dictionary<int, EmployeeProfileCompletionScore> ScoreVendors(IList<int> ids)
        {
            return ScoreParty("tblVendor", "VendorID", ids, MapVendor);
        }

        public Dictionary<int, EmployeeProfileCompletionScore> ScoreProducts(IList<int> ids)
        {
            var map = new Dictionary<int, EmployeeProfileCompletionScore>();
            var list = Normalize(ids);
            if (list.Count == 0 || string.IsNullOrWhiteSpace(_conn)) return map;
            try
            {
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
SELECT ProductID, ProductCode, ProductName, InventoryTypeID, ItemID,
       SUUnitOfMeasureID, PUUnitOfMeasureID, IUUnitOfMeasureID,
       ProductNatureID, ItemRegisteredID, BrandCodeID, BrandGroupID, ProductGroupID,
       ProductSalesGroupID, ItemGroupID, SalesCategoryID, ProductDivisionID, ProductTeamID, HSCodeID,
       SellingPrice, GTINNumber
FROM tblProduct WHERE ProductID IN (" + JoinIds(list) + ");", conn))
                {
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var id = Convert.ToInt32(dr["ProductID"]);
                            map[id] = CalculateProduct(new ProductInput
                            {
                                ProductID = id,
                                ProductCode = Str(dr, "ProductCode"),
                                ProductName = Str(dr, "ProductName"),
                                InventoryTypeID = IntVal(dr, "InventoryTypeID"),
                                ItemID = Str(dr, "ItemID"),
                                SUUnitOfMeasureID = IntVal(dr, "SUUnitOfMeasureID"),
                                PUUnitOfMeasureID = IntVal(dr, "PUUnitOfMeasureID"),
                                IUUnitOfMeasureID = IntVal(dr, "IUUnitOfMeasureID"),
                                ProductNatureID = IntVal(dr, "ProductNatureID"),
                                ItemRegisteredID = IntVal(dr, "ItemRegisteredID"),
                                BrandCodeID = IntVal(dr, "BrandCodeID"),
                                BrandGroupID = IntVal(dr, "BrandGroupID"),
                                ProductGroupID = IntVal(dr, "ProductGroupID"),
                                ProductSalesGroupID = IntVal(dr, "ProductSalesGroupID"),
                                ItemGroupID = IntVal(dr, "ItemGroupID"),
                                SalesCategoryID = IntVal(dr, "SalesCategoryID"),
                                ProductDivisionID = IntVal(dr, "ProductDivisionID"),
                                ProductTeamID = IntVal(dr, "ProductTeamID"),
                                HSCodeID = IntVal(dr, "HSCodeID"),
                                SellingPrice = MoneyStr(dr, "SellingPrice"),
                                GTINNumber = Str(dr, "GTINNumber")
                            });
                        }
                    }
                }
            }
            catch { }
            return map;
        }

        private Dictionary<int, EmployeeProfileCompletionScore> ScoreParty(
            string table, string idCol, IList<int> ids,
            Func<SqlDataReader, int, EmployeeProfileCompletionScore> mapRow)
        {
            var map = new Dictionary<int, EmployeeProfileCompletionScore>();
            var list = Normalize(ids);
            if (list.Count == 0 || string.IsNullOrWhiteSpace(_conn)) return map;
            try
            {
                using (var conn = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(@"
SELECT " + idCol + @", " + (table == "tblCustomer" ? "CustomerCode" : "VendorCode") + @", Name, SearchName,
       DealForBranchID, CityID, ProvinceID, ModeOfDeliveryID, CustomerGroupID, CustomerClassID,
       MethodOfPaymentID, TermsOfPaymentID, CurrencyID, BillPreferenceID, FBRStatusID, TaxGroupID,
       CNIC, NTN, TotalBusinessPotential, TargetBusinessSharePercent, TargetBusinessAmount,
       CreditLimit, AHDCreditLimit, PHDCreditLimit, HHDCreditLimit
FROM " + table + " WHERE " + idCol + " IN (" + JoinIds(list) + ");", conn))
                {
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var id = Convert.ToInt32(dr[0]);
                            map[id] = mapRow(dr, id);
                        }
                    }
                }
            }
            catch { }
            return map;
        }

        private EmployeeProfileCompletionScore MapCustomer(SqlDataReader dr, int id)
        {
            return CalculateCustomer(new CustomerInput
            {
                CustomerID = id,
                CustomerCode = Str(dr, "CustomerCode"),
                Name = Str(dr, "Name"),
                SearchName = Str(dr, "SearchName"),
                DealForBranchID = IntVal(dr, "DealForBranchID"),
                CityID = IntVal(dr, "CityID"),
                ProvinceID = IntVal(dr, "ProvinceID"),
                ModeOfDeliveryID = IntVal(dr, "ModeOfDeliveryID"),
                CustomerGroupID = IntVal(dr, "CustomerGroupID"),
                CustomerClassID = IntVal(dr, "CustomerClassID"),
                MethodOfPaymentID = IntVal(dr, "MethodOfPaymentID"),
                TermsOfPaymentID = IntVal(dr, "TermsOfPaymentID"),
                CurrencyID = IntVal(dr, "CurrencyID"),
                BillPreferenceID = IntVal(dr, "BillPreferenceID"),
                FBRStatusID = IntVal(dr, "FBRStatusID"),
                TaxGroupID = IntVal(dr, "TaxGroupID"),
                CNIC = Str(dr, "CNIC"),
                NTN = Str(dr, "NTN"),
                TotalBusinessPotential = MoneyStr(dr, "TotalBusinessPotential"),
                TargetBusinessSharePercent = MoneyStr(dr, "TargetBusinessSharePercent"),
                TargetBusinessAmount = MoneyStr(dr, "TargetBusinessAmount"),
                CreditLimit = MoneyStr(dr, "CreditLimit"),
                AHDCreditLimit = MoneyStr(dr, "AHDCreditLimit"),
                PHDCreditLimit = MoneyStr(dr, "PHDCreditLimit"),
                HHDCreditLimit = MoneyStr(dr, "HHDCreditLimit")
            });
        }

        private EmployeeProfileCompletionScore MapVendor(SqlDataReader dr, int id)
        {
            return CalculateVendor(new VendorInput
            {
                VendorID = id,
                VendorCode = Str(dr, "VendorCode"),
                Name = Str(dr, "Name"),
                SearchName = Str(dr, "SearchName"),
                DealForBranchID = IntVal(dr, "DealForBranchID"),
                CityID = IntVal(dr, "CityID"),
                ProvinceID = IntVal(dr, "ProvinceID"),
                ModeOfDeliveryID = IntVal(dr, "ModeOfDeliveryID"),
                CustomerGroupID = IntVal(dr, "CustomerGroupID"),
                CustomerClassID = IntVal(dr, "CustomerClassID"),
                MethodOfPaymentID = IntVal(dr, "MethodOfPaymentID"),
                TermsOfPaymentID = IntVal(dr, "TermsOfPaymentID"),
                CurrencyID = IntVal(dr, "CurrencyID"),
                BillPreferenceID = IntVal(dr, "BillPreferenceID"),
                FBRStatusID = IntVal(dr, "FBRStatusID"),
                TaxGroupID = IntVal(dr, "TaxGroupID"),
                CNIC = Str(dr, "CNIC"),
                NTN = Str(dr, "NTN"),
                TotalBusinessPotential = MoneyStr(dr, "TotalBusinessPotential"),
                TargetBusinessSharePercent = MoneyStr(dr, "TargetBusinessSharePercent"),
                TargetBusinessAmount = MoneyStr(dr, "TargetBusinessAmount"),
                CreditLimit = MoneyStr(dr, "CreditLimit"),
                AHDCreditLimit = MoneyStr(dr, "AHDCreditLimit"),
                PHDCreditLimit = MoneyStr(dr, "PHDCreditLimit"),
                HHDCreditLimit = MoneyStr(dr, "HHDCreditLimit")
            });
        }

        private static EmployeeProfileCompletionScore Assemble(params EmployeeProfileCompletionCategory[] cats)
        {
            var score = new EmployeeProfileCompletionScore();
            decimal total = 0;
            if (cats != null)
            {
                foreach (var cat in cats)
                {
                    if (cat == null) continue;
                    score.Categories.Add(cat);
                    total += cat.Score;
                }
            }
            if (total > 100m) total = 100m;
            if (total < 0m) total = 0m;
            score.Percent = decimal.Round(total, 1, MidpointRounding.AwayFromZero);
            if (score.Percent > 100m) score.Percent = 100m;
            return score;
        }

        private static EmployeeProfileCompletionCategory Category(string name, params bool[] flags)
        {
            var total = flags == null || flags.Length == 0 ? 1 : flags.Length;
            var filled = 0;
            if (flags != null)
            {
                foreach (var f in flags)
                    if (f) filled++;
            }
            var points = total <= 0 ? CategoryWeight : decimal.Round(CategoryWeight * filled / total, 4, MidpointRounding.AwayFromZero);
            if (filled >= total) points = CategoryWeight;
            return new EmployeeProfileCompletionCategory
            {
                Name = name,
                Filled = filled,
                Total = total,
                Score = points
            };
        }

        private static List<int> Normalize(IList<int> ids)
        {
            if (ids == null) return new List<int>();
            return ids.Where(id => id > 0).Distinct().ToList();
        }

        private static string JoinIds(List<int> ids)
            => string.Join(",", ids.Select(id => id.ToString(CultureInfo.InvariantCulture)).ToArray());

        private static bool HasText(string value) => !string.IsNullOrWhiteSpace(value);

        private static bool HasNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            decimal n;
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out n)
                || decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out n);
        }

        private static string Str(SqlDataReader dr, string col)
        {
            try
            {
                var i = dr.GetOrdinal(col);
                return dr.IsDBNull(i) ? "" : Convert.ToString(dr.GetValue(i)) ?? "";
            }
            catch { return ""; }
        }

        private static int IntVal(SqlDataReader dr, string col)
        {
            try
            {
                var i = dr.GetOrdinal(col);
                if (dr.IsDBNull(i)) return 0;
                return Convert.ToInt32(dr.GetValue(i));
            }
            catch { return 0; }
        }

        private static string MoneyStr(SqlDataReader dr, string col)
        {
            try
            {
                var i = dr.GetOrdinal(col);
                if (dr.IsDBNull(i)) return "";
                return Convert.ToString(dr.GetValue(i), CultureInfo.InvariantCulture) ?? "";
            }
            catch { return ""; }
        }
    }
}
