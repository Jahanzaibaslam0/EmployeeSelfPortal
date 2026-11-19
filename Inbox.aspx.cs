using System;
using System.Collections.Generic;
using HRMS.Services;

namespace HRMS
{
    public partial class InboxPage : AppBasePage
    {
        private readonly AppInboxService _svc = new AppInboxService();

        public string PageTitle => "Notifications";
        public List<InboxMessage> Items { get; private set; } = new List<InboxMessage>();

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;
            if (!Auth.CurrentUserId.HasValue)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }
            _svc.EnsureSchema();
            var openId = QueryInt("open");
            if (openId.HasValue)
            {
                _svc.MarkRead(openId.Value, Auth.CurrentUserId.Value);
                var items = _svc.List(Auth.CurrentUserId.Value, 100);
                var found = items.Find(i => i.InboxID == openId.Value);
                if (found != null && !string.IsNullOrEmpty(found.LinkUrl))
                {
                    Response.Redirect(found.LinkUrl);
                    return;
                }
            }
            Items = _svc.List(Auth.CurrentUserId.Value, 100);
        }
    }
}
