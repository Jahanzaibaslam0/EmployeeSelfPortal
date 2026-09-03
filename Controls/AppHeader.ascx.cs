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
        protected bool ShowDashboard { get; private set; }
        protected bool ShowMyDocuments { get; private set; }
        protected bool ShowLmsLibrary { get; private set; }

        protected bool ShowMasterMenu { get; private set; }
        protected bool ShowOrgSetupMenu { get; private set; }
        protected bool ShowSecurityMenu { get; private set; }

        protected bool CanEmployeeMaster { get; private set; }
        protected bool CanPositionMaster { get; private set; }
        protected bool CanCustomerMaster { get; private set; }
        protected bool CanContactMaster { get; private set; }
        protected bool CanProductMaster { get; private set; }
        protected bool CanInvoiceMaster { get; private set; }

        protected bool CanDivisionSetup { get; private set; }
        protected bool CanDepartmentSetup { get; private set; }
        protected bool CanGenderSetup { get; private set; }
        protected bool CanBankSetup { get; private set; }
        protected bool CanCurrencySetup { get; private set; }
        protected bool CanCitySetup { get; private set; }
        protected bool CanSkillSetup { get; private set; }
        protected bool CanLmsDocumentSetup { get; private set; }

        protected bool CanUserSetup { get; private set; }
        protected bool CanUserRightsSetup { get; private set; }

        // Login alerts popup (same behavior as Razor _NotificationPopup)
        protected bool ShowLoginAlertsPopup { get; private set; }
        protected List<NotificationItem> PopupNotifications { get; private set; } = new List<NotificationItem>();
        protected List<MemorandumItem> PopupMemorandums { get; private set; } = new List<MemorandumItem>();

        protected void Page_Load(object sender, EventArgs e)
        {
            var auth = new AuthService();
            var perms = new PermissionService();
            var profileAccess = new EmployeeProfileAccessService();
            ShowUserInfo = auth.IsLoggedIn;
            Username = auth.CurrentUsername;
            IsAdmin = auth.IsAdmin;
            ShowDashboard = auth.IsAdmin || perms.CanRead("Dashboard");
            ShowMyDocuments = profileAccess.CanAccessMyDocuments();
            ShowLmsLibrary = new LmsDocumentService().CanAccessLibrary();

            CanEmployeeMaster = perms.CanRead("EmployeeMaster");
            CanPositionMaster = perms.CanRead("PositionMaster");
            CanCustomerMaster = perms.CanRead("CustomerMaster");
            CanContactMaster = perms.CanRead("ContactMaster");
            CanProductMaster = perms.CanRead("ProductMaster");
            CanInvoiceMaster = perms.CanRead("InvoiceMaster");
            ShowMasterMenu = CanEmployeeMaster || CanPositionMaster || CanCustomerMaster
                || CanContactMaster || CanProductMaster || CanInvoiceMaster;

            CanDivisionSetup = perms.CanRead("DivisionSetup");
            CanDepartmentSetup = perms.CanRead("DepartmentSetup");
            CanGenderSetup = perms.CanRead("GenderSetup");
            CanBankSetup = perms.CanRead("BankSetup");
            CanCurrencySetup = perms.CanRead("CurrencySetup");
            CanCitySetup = perms.CanRead("CitySetup");
            CanSkillSetup = perms.CanRead("SkillSetup");
            CanLmsDocumentSetup = perms.CanRead("LmsDocumentSetup");
            ShowOrgSetupMenu = CanDivisionSetup || CanDepartmentSetup || CanGenderSetup
                || CanBankSetup || CanCurrencySetup || CanCitySetup || CanSkillSetup || CanLmsDocumentSetup;

            CanUserSetup = perms.CanRead("UserSetup");
            CanUserRightsSetup = perms.CanRead("UserRightsSetup");
            ShowSecurityMenu = CanUserSetup || CanUserRightsSetup || IsAdmin;

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
