using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class UserRecord
    {
        public int UserID { get; set; }
        public string UserCode { get; set; } = "";
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public bool IsAdmin { get; set; }
    }

    public partial class UserSetupPage : AppBasePage
    {
        private readonly DataAccessScopeService _dataScope = new DataAccessScopeService();

        public string PageTitle => "User Setup";
        public UserRecord Input { get; set; } = new UserRecord { IsActive = true };
        public List<UserRecord> Users { get; set; } = new List<UserRecord>();
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
                    SoftDelete(int.TryParse(Request.Form["deleteId"], out var d) ? d : 0);
                    return;
                }
                Save();
                return;
            }
            LoadPage(QueryInt("editId"));
        }

        private void LoadPage(int? editId)
        {
            LoadAlert(out var msg, out var typ);
            AlertMessage = msg; AlertType = typ;
            if (editId.HasValue && editId > 0) LoadForEdit(editId.Value);
            else Input.UserCode = GenerateNextUserCode();
            LoadUsers();
        }

        private void Save()
        {
            if (!Perms.CanWrite("UserSetup"))
            {
                SetAlert(PermissionService.AccessRestrictedMessage, "error");
                Response.Redirect("~/UserSetup.aspx");
                return;
            }

            var userId = int.TryParse(Request.Form["userId"], out var id) ? id : 0;
            var userCode = FormString("userCode");
            var username = FormString("username");
            var fullName = FormString("fullName");
            var email = FormString("email");
            var newPassword = FormString("newPassword");
            var isActive = FormBool("isActive");
            var isAdmin = FormBool("isAdmin");

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName))
            {
                SetAlert("Username and Full Name are required.", "error");
                Response.Redirect("~/UserSetup.aspx" + (userId > 0 ? "?editId=" + userId : ""));
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    if (userId > 0)
                    {
                        using (var cmd = new SqlCommand(@"
UPDATE tblUser SET UserCode=@Code, Username=@Username, FullName=@FullName, Email=@Email,
  IsActive=@IsActive, IsAdmin=@IsAdmin, ModifiedOn=GETDATE() WHERE UserID=@ID;", conn))
                        {
                            cmd.Parameters.AddWithValue("@ID", userId);
                            cmd.Parameters.AddWithValue("@Code", string.IsNullOrWhiteSpace(userCode) ? (object)DBNull.Value : userCode);
                            cmd.Parameters.AddWithValue("@Username", username);
                            cmd.Parameters.AddWithValue("@FullName", fullName);
                            cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(email) ? (object)DBNull.Value : email);
                            cmd.Parameters.AddWithValue("@IsActive", isActive);
                            cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);
                            cmd.ExecuteNonQuery();
                        }
                        if (!string.IsNullOrWhiteSpace(newPassword))
                        {
                            using (var cmd = new SqlCommand("UPDATE tblUser SET PasswordHash=@Hash WHERE UserID=@ID;", conn))
                            {
                                cmd.Parameters.AddWithValue("@ID", userId);
                                cmd.Parameters.AddWithValue("@Hash", PasswordHelper.HashPassword(newPassword));
                                cmd.ExecuteNonQuery();
                            }
                        }
                        SetAlert("User updated successfully.");
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(newPassword)) newPassword = "ChangeMe@123";
                        using (var cmd = new SqlCommand(@"
INSERT INTO tblUser (UserCode, Username, PasswordHash, FullName, Email, IsActive, IsAdmin, CreatedOn)
VALUES (@Code,@Username,@Hash,@FullName,@Email,@IsActive,@IsAdmin,GETDATE());", conn))
                        {
                            cmd.Parameters.AddWithValue("@Code", string.IsNullOrWhiteSpace(userCode) ? (object)DBNull.Value : userCode);
                            cmd.Parameters.AddWithValue("@Username", username);
                            cmd.Parameters.AddWithValue("@Hash", PasswordHelper.HashPassword(newPassword));
                            cmd.Parameters.AddWithValue("@FullName", fullName);
                            cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(email) ? (object)DBNull.Value : email);
                            cmd.Parameters.AddWithValue("@IsActive", isActive);
                            cmd.Parameters.AddWithValue("@IsAdmin", isAdmin);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("User added successfully.");
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                SetAlert("Username already exists.", "error");
                Response.Redirect("~/UserSetup.aspx" + (userId > 0 ? "?editId=" + userId : ""));
                return;
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
                Response.Redirect("~/UserSetup.aspx" + (userId > 0 ? "?editId=" + userId : ""));
                return;
            }
            Response.Redirect("~/UserSetup.aspx");
        }

        private void SoftDelete(int id)
        {
            if (!Perms.CanDelete("UserSetup") && !Perms.CanWrite("UserSetup"))
            {
                SetAlert(PermissionService.AccessRestrictedMessage, "error");
                Response.Redirect("~/UserSetup.aspx");
                return;
            }
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand("UPDATE tblUser SET IsActive=0, ModifiedOn=GETDATE() WHERE UserID=@ID;", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("User deactivated.");
            }
            catch (Exception ex) { SetAlert("Error: " + ex.Message, "error"); }
            Response.Redirect("~/UserSetup.aspx");
        }

        private void LoadForEdit(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT UserID, UserCode, Username, FullName, Email, IsActive, IsAdmin FROM tblUser WHERE UserID=@ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        Input = new UserRecord
                        {
                            UserID = Convert.ToInt32(dr["UserID"]),
                            UserCode = dr["UserCode"] == DBNull.Value ? "" : dr["UserCode"].ToString() ?? "",
                            Username = dr["Username"].ToString() ?? "",
                            FullName = dr["FullName"].ToString() ?? "",
                            Email = dr["Email"] == DBNull.Value ? "" : dr["Email"].ToString() ?? "",
                            IsActive = Convert.ToBoolean(dr["IsActive"]),
                            IsAdmin = Convert.ToBoolean(dr["IsAdmin"])
                        };
                    }
                }
            }
        }

        private void LoadUsers()
        {
            Users.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT UserID, UserCode, Username, FullName, Email, IsActive, IsAdmin FROM tblUser ORDER BY IsActive DESC, Username;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Users.Add(new UserRecord
                        {
                            UserID = Convert.ToInt32(dr["UserID"]),
                            UserCode = dr["UserCode"] == DBNull.Value ? "" : dr["UserCode"].ToString() ?? "",
                            Username = dr["Username"].ToString() ?? "",
                            FullName = dr["FullName"].ToString() ?? "",
                            Email = dr["Email"] == DBNull.Value ? "" : dr["Email"].ToString() ?? "",
                            IsActive = Convert.ToBoolean(dr["IsActive"]),
                            IsAdmin = Convert.ToBoolean(dr["IsAdmin"])
                        });
                    }
                }
            }
        }

        private string GenerateNextUserCode()
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand("SELECT TOP 1 UserCode FROM tblUser WHERE UserCode LIKE 'GB-US-%' ORDER BY UserCode DESC;", conn))
            {
                conn.Open();
                var last = cmd.ExecuteScalar() as string;
                if (!string.IsNullOrEmpty(last) && last.Length >= 9 && int.TryParse(last.Substring(6), out int num))
                    return "GB-US-" + (num + 1).ToString("D5");
                return "GB-US-00001";
            }
        }
    }
}
