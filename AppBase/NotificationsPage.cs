using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class NotificationsPage : AppBasePage
    {
        private readonly NotificationService _notifications = new NotificationService();

        public string PageTitle => "Notifications";
        public List<NotificationItem> ActiveNotifications { get; private set; } = new List<NotificationItem>();
        public NotificationItem Selected { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            ActiveNotifications = _notifications.GetActiveNotifications();

            var id = QueryInt("id");
            if (id.HasValue && id > 0)
                Selected = ActiveNotifications.FirstOrDefault(n => n.NotificationID == id.Value);
            else if (ActiveNotifications.Count > 0)
                Selected = ActiveNotifications[0];
        }
    }
}
