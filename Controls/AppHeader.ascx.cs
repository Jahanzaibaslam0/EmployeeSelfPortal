using System;
using System.Collections.Generic;
using System.Web.UI;
using HRMS.Services;

namespace HRMS.Controls
{
    public partial class AppHeader : UserControl
    {
        public string PageTitleText { get; set; } = "";

        protected bool ShowUserInfo { get; private set; }
        protected string Username { get; private set; } = "";
        protected bool IsAdmin { get; private set; }
        protected int InboxUnread { get; private set; }

        protected bool ShowLoginAlertsPopup { get; private set; }
        protected List<NotificationItem> PopupNotifications { get; private set; } = new List<NotificationItem>();
        protected List<MemorandumItem> PopupMemorandums { get; private set; } = new List<MemorandumItem>();

        protected void Page_Load(object sender, EventArgs e)
        {
            var auth = new AuthService();
            ShowUserInfo = auth.IsLoggedIn;
            Username = auth.CurrentUsername;
            IsAdmin = auth.IsAdmin;

            if (auth.IsLoggedIn && auth.CurrentUserId.HasValue)
            {
                try
                {
                    var inbox = new AppInboxService();
                    inbox.EnsureSchema();
                    InboxUnread = inbox.UnreadCount(auth.CurrentUserId.Value);
                    var changes = new ProfileChangeRequestService();
                    changes.EnsureSchema();
                }
                catch { }
            }

            LoadLoginAlertsPopup();
        }

        private void LoadLoginAlertsPopup()
        {
            PopupNotifications = new List<NotificationItem>();
            PopupMemorandums = new List<MemorandumItem>();
            ShowLoginAlertsPopup = false;

            if (Session == null) return;

            var showNotif = Session["ShowNotificationPopup"] != null
                && Convert.ToInt32(Session["ShowNotificationPopup"]) == 1;
            var showMemo = Session["ShowMemorandumPopup"] != null
                && Convert.ToInt32(Session["ShowMemorandumPopup"]) == 1;

            if (showNotif)
            {
                Session.Remove("ShowNotificationPopup");
                PopupNotifications = new NotificationService().GetActiveNotifications();
            }

            if (showMemo)
            {
                Session.Remove("ShowMemorandumPopup");
                PopupMemorandums = new MemorandumService().GetActiveMemorandums();
            }

            ShowLoginAlertsPopup = PopupNotifications.Count > 0 || PopupMemorandums.Count > 0;
        }
    }
}
