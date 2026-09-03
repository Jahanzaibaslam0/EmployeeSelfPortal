using System;
using System.Configuration;
using System.Data.SqlClient;
using HRMS.Services;

namespace HRMS
{
    /// <summary>
    /// Runs database setup on application start.
    /// Full schema: run D:\Project\DATA\Script.sql and UserSecurity_Script.sql on SQL Server first.
    /// </summary>
    public static class StartupMigrations
    {
        public static void Run()
        {
            var connStr = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connStr)) return;

            try
            {
                DatabaseBootstrap.EnsureCoreSchema(connStr);
                new AuthService().EnsureUserTableAndAdmin();
            }
            catch
            {
                // Database not ready — run scripts in D:\Project\DATA
            }
        }

        public static void Seed()
        {
            var connStr = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connStr)) return;

            try
            {
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    foreach (var form in AppForms.All)
                    {
                        using (var cmd = new SqlCommand(@"
                            IF NOT EXISTS (SELECT 1 FROM tblAppForm WHERE FormKey = @Key)
                            INSERT INTO tblAppForm (FormKey, FormName, PagePath, Category, SortOrder)
                            VALUES (@Key, @Name, @Path, @Category, @Order);", conn))
                        {
                            cmd.Parameters.AddWithValue("@Key", form.Key);
                            cmd.Parameters.AddWithValue("@Name", form.Name);
                            cmd.Parameters.AddWithValue("@Path", form.Path + ".aspx");
                            cmd.Parameters.AddWithValue("@Category", form.Category);
                            cmd.Parameters.AddWithValue("@Order", form.SortOrder);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch
            {
                // Seed skipped if DB not ready
            }
        }
    }
}
