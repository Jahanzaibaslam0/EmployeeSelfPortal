using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace HRMS.Services
{
    public class NotificationItem
    {
        public int NotificationID { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int? DepartmentID { get; set; }
        public string DepartmentName { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime ValidTillDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class NotificationService
    {
        private readonly string _conn;

        public NotificationService()
        {
            _conn = ConfigurationManager.ConnectionStrings["HRMSConnection"]?.ConnectionString ?? "";
        }

        public List<NotificationItem> GetActiveNotifications()
        {
            var list = new List<NotificationItem>();

            using (var conn = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
            SELECT n.NotificationID, n.NotificationName, n.Description,
                   n.DepartmentID, d.DepartmentName,
                   n.StartDate, n.ValidTillDate, n.IsActive
            FROM tblNotification n
            LEFT JOIN tblDepartment d ON d.DepartmentID = n.DepartmentID
            WHERE n.IsActive = 1
              AND n.StartDate <= CAST(GETDATE() AS DATE)
              AND n.ValidTillDate >= CAST(GETDATE() AS DATE)
            ORDER BY n.StartDate DESC, n.NotificationID DESC;", conn))
            {
                conn.Open();

                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new NotificationItem
                        {
                            NotificationID = dr.GetInt32(0),
                            Name = dr.GetString(1),
                            Description = dr.IsDBNull(2) ? "" : dr.GetString(2),
                            DepartmentID = dr.IsDBNull(3) ? (int?)null : dr.GetInt32(3),
                            DepartmentName = dr.IsDBNull(4) ? "All Departments" : dr.GetString(4),
                            StartDate = dr.GetDateTime(5),
                            ValidTillDate = dr.GetDateTime(6),
                            IsActive = dr.GetBoolean(7)
                        });
                    }
                }
            }

            return list;
        }
    }
}
