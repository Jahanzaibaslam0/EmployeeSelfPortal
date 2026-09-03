using System;

namespace HRMS
{
    public class LookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class ProductLookupItem
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string ItemID { get; set; } = "";
        public string HSCode { get; set; } = "";
        public string UnitOfMeasure { get; set; } = "";
        public string SellingPrice { get; set; } = "";
    }

    public class PartyLookupItem
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public class SetupLinkItem
    {
        public string FormKey { get; set; } = "";
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    public class SetupCategoryGroup
    {
        public string Category { get; set; } = "";
        public string Icon { get; set; } = "";
        public System.Collections.Generic.List<SetupLinkItem> Links { get; set; } =
            new System.Collections.Generic.List<SetupLinkItem>();
    }

    public class QuickLinkItem
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Url { get; set; } = "";
        public string Category { get; set; } = "";
        public string Icon { get; set; } = "";
        public bool External { get; set; }
    }

    public class CityRecord
    {
        public int CityID { get; set; }
        public string CityCode { get; set; } = "";
        public string CityName { get; set; } = "";
        public string AliasName { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class ProvinceRecord
    {
        public int ProvinceID { get; set; }
        public string ProvinceCode { get; set; } = "";
        public string ProvinceName { get; set; } = "";
        public string AliasName { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class CurrencyRecord
    {
        public int CurrencyID { get; set; }
        public string CurrencyCode { get; set; } = "";
        public string CurrencyName { get; set; } = "";
        public string AliasName { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }
}
