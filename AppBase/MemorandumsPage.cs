using System;
using System.Collections.Generic;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class MemorandumsPage : AppBasePage
    {
        private readonly MemorandumService _memorandums = new MemorandumService();

        public string PageTitle => "Memorandums";
        public List<MemorandumItem> ActiveMemorandums { get; private set; } = new List<MemorandumItem>();
        public MemorandumItem Selected { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            ActiveMemorandums = _memorandums.GetActiveMemorandums();

            var id = QueryInt("id");
            if (id.HasValue && id > 0)
                Selected = _memorandums.GetById(id.Value);
            else if (ActiveMemorandums.Count > 0)
                Selected = ActiveMemorandums[0];
        }
    }
}
