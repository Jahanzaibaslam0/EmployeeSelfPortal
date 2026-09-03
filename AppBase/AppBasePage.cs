using System;
using System.Configuration;
using System.Web;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public abstract class AppBasePage : Page
    {
        protected string Conn =>
            ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";

        protected AuthService Auth => new AuthService();
        protected PermissionService Perms => new PermissionService();
        protected AuditService Audit => new AuditService();

        protected virtual bool IsPublicPage => false;

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if (IsPublicPage) return;

            if (!Auth.IsLoggedIn)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            var path = VirtualPathUtility.ToAppRelative(Request.Path).TrimStart('~');
            if (!Perms.CanAccessPage(path))
            {
                Response.Redirect("~/Home.aspx?accessDenied=1");
            }
        }

        private const string FlashCookieName = "HRMS_FlashAlert";

        protected void SetAlert(string message, string type = "success")
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            type = string.IsNullOrWhiteSpace(type) ? "success" : type.Trim();

            try
            {
                Session["Alert"] = message;
                Session["AlertType"] = type;
            }
            catch
            {
                // Session may be unavailable on some production hosts.
            }

            // Cookie fallback so alerts survive Session loss (web farm / recycle / sticky-session gaps).
            try
            {
                var payload = HttpUtility.UrlEncode(type) + "|" + HttpUtility.UrlEncode(message);
                var cookie = new HttpCookie(FlashCookieName, payload)
                {
                    HttpOnly = true,
                    Path = "/",
                    Expires = DateTime.Now.AddMinutes(3)
                };
                Response.Cookies.Set(cookie);
            }
            catch
            {
                // Ignore cookie write failures; Session may still work.
            }
        }

        protected void LoadAlert(out string message, out string type)
        {
            message = "";
            type = "success";

            try
            {
                message = Session["Alert"] as string ?? "";
                type = Session["AlertType"] as string ?? "success";
                Session.Remove("Alert");
                Session.Remove("AlertType");
            }
            catch
            {
                // ignore
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                try
                {
                    var cookie = Request.Cookies[FlashCookieName];
                    if (cookie != null && !string.IsNullOrWhiteSpace(cookie.Value))
                    {
                        var raw = cookie.Value;
                        var sep = raw.IndexOf('|');
                        if (sep > 0)
                        {
                            type = HttpUtility.UrlDecode(raw.Substring(0, sep)) ?? "success";
                            message = HttpUtility.UrlDecode(raw.Substring(sep + 1)) ?? "";
                        }
                        else
                        {
                            message = HttpUtility.UrlDecode(raw) ?? "";
                        }
                    }
                }
                catch
                {
                    // ignore
                }
            }

            // Always clear flash cookie after read attempt.
            try
            {
                var clear = new HttpCookie(FlashCookieName, "")
                {
                    HttpOnly = true,
                    Path = "/",
                    Expires = DateTime.Now.AddDays(-1)
                };
                Response.Cookies.Set(clear);
            }
            catch
            {
                // ignore
            }
        }

        protected bool HasAlert
        {
            get
            {
                try
                {
                    if (Session["Alert"] != null) return true;
                }
                catch { }
                try
                {
                    var cookie = Request.Cookies[FlashCookieName];
                    return cookie != null && !string.IsNullOrWhiteSpace(cookie.Value);
                }
                catch { return false; }
            }
        }

        protected int? QueryInt(string name)
        {
            var raw = Request.QueryString[name];
            if (string.IsNullOrWhiteSpace(raw)) return null;
            return int.TryParse(raw, out var id) ? id : null;
        }

        protected string FormString(string name) => Request.Form[name]?.Trim() ?? "";

        protected int FormInt(string name)
        {
            int.TryParse(Request.Form[name], out var id);
            return id;
        }

        protected bool FormBool(string name) =>
            string.Equals(Request.Form[name], "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Request.Form[name], "on", StringComparison.OrdinalIgnoreCase);
    }
}
