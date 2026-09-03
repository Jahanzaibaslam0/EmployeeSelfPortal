using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;

namespace HRMS
{
    /// <summary>Minimal list/edit shell for transaction masters (Customer, Vendor, etc.).</summary>
    public abstract class SimpleListMasterPage : AppBasePage
    {
        public abstract string PageTitle { get; }
        public abstract string ListSql { get; }
        public virtual string IdColumn => "Id";
        public DataTable Rows { get; private set; } = new DataTable();
        public string AlertMessage { get; set; } = "";
        public string AlertType { get; set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg; AlertType = typ;
            LoadList();
        }

        protected virtual void LoadList()
        {
            Rows = new DataTable();
            using (var conn = new SqlConnection(Conn))
            using (var da = new SqlDataAdapter(ListSql, conn))
            {
                da.Fill(Rows);
            }
        }
    }
}
