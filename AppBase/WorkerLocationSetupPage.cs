using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using HRMS.Services;

namespace HRMS
{
    public class WorkerLocationRecord
    {
        public int WorkerLocationID { get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public int PrimaryLocationID { get; set; }
        public string PrimaryLocationName { get; set; } = "";
        public int SecondaryLocationID { get; set; }
        public string SecondaryLocationName { get; set; } = "";
        public int WorkLocationTypeID { get; set; }
        public string WorkLocationTypeName { get; set; } = "";
        public int WorkArrangementID { get; set; }
        public string WorkArrangementName { get; set; } = "";
        public string HybridSchedule { get; set; } = "";
        public string TerritoryRegionAssignment { get; set; } = "";
        public string ClientSiteAccess { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class WorkerLocationSetupPage : AppBasePage
    {
        public string PageTitle => "Worker Location Setup";
        public WorkerLocationRecord Input { get; private set; } = new WorkerLocationRecord { IsActive = true };
        public List<WorkerLocationRecord> Records { get; private set; } = new List<WorkerLocationRecord>();
        public List<LookupItem> Employees { get; private set; } = new List<LookupItem>();
        public List<LookupItem> Locations { get; private set; } = new List<LookupItem>();
        public List<LookupItem> WorkLocationTypes { get; private set; } = new List<LookupItem>();
        public List<LookupItem> WorkArrangements { get; private set; } = new List<LookupItem>();
        public string AlertMessage { get; private set; } = "";
        public string AlertType { get; private set; } = "success";

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = Page.Master as SiteMaster;
            if (master != null) master.PageTitleText = PageTitle;

            if (IsPostBack)
            {
                var handler = Request.Form["__handler"] ?? "Save";
                if (string.Equals(handler, "Delete", StringComparison.OrdinalIgnoreCase))
                {
                    OnPostDelete();
                    return;
                }
                OnPostSave();
                return;
            }

            LoadAlert(out var msg, out var typ);
            AlertMessage = msg;
            AlertType = typ;
            LoadEmployees();
            LoadLocations();
            LoadWorkLocationTypes();
            LoadWorkArrangements();

            var editId = QueryInt("editId");
            if (editId.HasValue && editId.Value > 0)
                LoadForEdit(editId.Value);

            LoadRecords();
        }

        private void OnPostSave()
        {
            var workerLocationID = ParseInt(FormString("workerLocationID"));
            var employeeID = ParseInt(FormString("employeeID"));
            var primaryLocationID = ParseInt(FormString("primaryLocationID"));
            var secondaryLocationID = ParseInt(FormString("secondaryLocationID"));
            var workLocationTypeID = ParseInt(FormString("workLocationTypeID"));
            var workArrangementID = ParseInt(FormString("workArrangementID"));
            var hybridSchedule = FormString("hybridSchedule");
            var territoryRegionAssignment = FormString("territoryRegionAssignment");
            var clientSiteAccess = FormString("clientSiteAccess");
            var isActive = FormBool("isActive");

            if (employeeID <= 0)
            {
                SetAlert("Employee is required.", "error");
                Response.Redirect(RedirectEdit(workerLocationID));
                return;
            }
            if (primaryLocationID <= 0)
            {
                SetAlert("Primary Work Location is required.", "error");
                Response.Redirect(RedirectEdit(workerLocationID));
                return;
            }

            try
            {
                using (var conn = new SqlConnection(Conn))
                {
                    conn.Open();
                    if (workerLocationID > 0)
                    {
                        using (var cmd = new SqlCommand(@"
                    UPDATE tblWorkerLocation SET
                        EmployeeID                = @EmployeeID,
                        PrimaryLocationID         = @PrimaryLocationID,
                        SecondaryLocationID       = @SecondaryLocationID,
                        WorkLocationTypeID        = @WorkLocationTypeID,
                        WorkArrangementID         = @WorkArrangementID,
                        HybridSchedule            = @HybridSchedule,
                        TerritoryRegionAssignment = @TerritoryRegionAssignment,
                        ClientSiteAccess          = @ClientSiteAccess,
                        IsActive                  = @IsActive,
                        ModifiedOn                = GETDATE(),
                        ModifiedByUserID          = @ModifiedByUserID
                    WHERE WorkerLocationID = @WorkerLocationID;", conn))
                        {
                            AddParams(cmd, workerLocationID, employeeID, primaryLocationID, secondaryLocationID,
                                workLocationTypeID, workArrangementID, hybridSchedule,
                                territoryRegionAssignment, clientSiteAccess, isActive);
                            AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Worker location updated successfully.");
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(@"
                    INSERT INTO tblWorkerLocation
                        (EmployeeID, PrimaryLocationID, SecondaryLocationID,
                         WorkLocationTypeID, WorkArrangementID,
                         HybridSchedule, TerritoryRegionAssignment, ClientSiteAccess, IsActive, CreatedOn, CreatedByUserID)
                    VALUES
                        (@EmployeeID, @PrimaryLocationID, @SecondaryLocationID,
                         @WorkLocationTypeID, @WorkArrangementID,
                         @HybridSchedule, @TerritoryRegionAssignment, @ClientSiteAccess, @IsActive, GETDATE(), @CreatedByUserID);", conn))
                        {
                            AddParams(cmd, 0, employeeID, primaryLocationID, secondaryLocationID,
                                workLocationTypeID, workArrangementID, hybridSchedule,
                                territoryRegionAssignment, clientSiteAccess, isActive);
                            AuditHelper.AddCreatedBy(cmd, Auth.CurrentUserId);
                            cmd.ExecuteNonQuery();
                        }
                        SetAlert("Worker location added successfully.");
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                    SetAlert("A worker location record already exists for this employee.", "error");
                else
                    SetAlert("Error: " + ex.Message, "error");
                Response.Redirect(RedirectEdit(workerLocationID));
                return;
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
                Response.Redirect(RedirectEdit(workerLocationID));
                return;
            }

            Response.Redirect("~/WorkerLocationSetup.aspx");
        }

        private void OnPostDelete()
        {
            var deleteId = ParseInt(FormString("deleteId"));
            try
            {
                using (var conn = new SqlConnection(Conn))
                using (var cmd = new SqlCommand(@"
                UPDATE tblWorkerLocation SET IsActive = 0, ModifiedOn = GETDATE(), ModifiedByUserID = @ModifiedByUserID
                WHERE WorkerLocationID = @ID;", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", deleteId);
                    AuditHelper.AddModifiedBy(cmd, Auth.CurrentUserId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                SetAlert("Worker location deactivated successfully.");
            }
            catch (Exception ex)
            {
                SetAlert("Error: " + ex.Message, "error");
            }
            Response.Redirect("~/WorkerLocationSetup.aspx");
        }

        private string RedirectEdit(int id)
            => id > 0 ? "~/WorkerLocationSetup.aspx?editId=" + id : "~/WorkerLocationSetup.aspx";

        private static int ParseInt(string value)
        {
            int n;
            return int.TryParse(value, out n) ? n : 0;
        }

        private static void AddParams(
            SqlCommand cmd, int workerLocationID, int employeeID,
            int primaryLocationID, int secondaryLocationID,
            int workLocationTypeID, int workArrangementID,
            string hybridSchedule, string territoryRegionAssignment, string clientSiteAccess,
            bool isActive)
        {
            if (workerLocationID > 0)
                cmd.Parameters.AddWithValue("@WorkerLocationID", workerLocationID);

            cmd.Parameters.AddWithValue("@EmployeeID", employeeID);
            cmd.Parameters.AddWithValue("@PrimaryLocationID", primaryLocationID);
            cmd.Parameters.AddWithValue("@SecondaryLocationID", secondaryLocationID <= 0 ? (object)DBNull.Value : secondaryLocationID);
            cmd.Parameters.AddWithValue("@WorkLocationTypeID", workLocationTypeID <= 0 ? (object)DBNull.Value : workLocationTypeID);
            cmd.Parameters.AddWithValue("@WorkArrangementID", workArrangementID <= 0 ? (object)DBNull.Value : workArrangementID);
            cmd.Parameters.AddWithValue("@HybridSchedule", string.IsNullOrWhiteSpace(hybridSchedule) ? (object)DBNull.Value : hybridSchedule.Trim());
            cmd.Parameters.AddWithValue("@TerritoryRegionAssignment", string.IsNullOrWhiteSpace(territoryRegionAssignment) ? (object)DBNull.Value : territoryRegionAssignment.Trim());
            cmd.Parameters.AddWithValue("@ClientSiteAccess", string.IsNullOrWhiteSpace(clientSiteAccess) ? (object)DBNull.Value : clientSiteAccess.Trim());
            cmd.Parameters.AddWithValue("@IsActive", isActive);
        }

        private void LoadEmployees()
        {
            Employees.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT EmployeeID, EmployeeCode, FirstName + ' ' + LastName AS FullName
            FROM tblEmployee
            WHERE Status = 'Active'
            ORDER BY FirstName, LastName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var code = dr["EmployeeCode"].ToString() ?? "";
                        var name = dr["FullName"].ToString() ?? "";
                        Employees.Add(new LookupItem
                        {
                            Id = Convert.ToInt32(dr["EmployeeID"]),
                            Name = code + " – " + name
                        });
                    }
                }
            }
        }

        private void LoadLocations()
        {
            Locations.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT LocationID, LocationName FROM tblLocation
            WHERE IsActive = 1 ORDER BY LocationName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Locations.Add(new LookupItem
                        {
                            Id = Convert.ToInt32(dr["LocationID"]),
                            Name = dr["LocationName"].ToString() ?? ""
                        });
                    }
                }
            }
        }

        private void LoadWorkLocationTypes()
        {
            WorkLocationTypes.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT WorkLocationTypeID, WorkLocationTypeName FROM tblWorkLocationType
            WHERE IsActive = 1 ORDER BY WorkLocationTypeName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        WorkLocationTypes.Add(new LookupItem
                        {
                            Id = Convert.ToInt32(dr["WorkLocationTypeID"]),
                            Name = dr["WorkLocationTypeName"].ToString() ?? ""
                        });
                    }
                }
            }
        }

        private void LoadWorkArrangements()
        {
            WorkArrangements.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT WorkArrangementID, WorkArrangementName FROM tblWorkArrangement
            WHERE IsActive = 1 ORDER BY WorkArrangementName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        WorkArrangements.Add(new LookupItem
                        {
                            Id = Convert.ToInt32(dr["WorkArrangementID"]),
                            Name = dr["WorkArrangementName"].ToString() ?? ""
                        });
                    }
                }
            }
        }

        private void LoadForEdit(int id)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT WorkerLocationID, EmployeeID, PrimaryLocationID, SecondaryLocationID,
                   WorkLocationTypeID, WorkArrangementID,
                   HybridSchedule, TerritoryRegionAssignment, ClientSiteAccess, IsActive
            FROM tblWorkerLocation WHERE WorkerLocationID = @ID;", conn))
            {
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return;
                    Input = new WorkerLocationRecord
                    {
                        WorkerLocationID = id,
                        EmployeeID = IntOrZero(dr["EmployeeID"]),
                        PrimaryLocationID = IntOrZero(dr["PrimaryLocationID"]),
                        SecondaryLocationID = IntOrZero(dr["SecondaryLocationID"]),
                        WorkLocationTypeID = IntOrZero(dr["WorkLocationTypeID"]),
                        WorkArrangementID = IntOrZero(dr["WorkArrangementID"]),
                        HybridSchedule = Str(dr["HybridSchedule"]),
                        TerritoryRegionAssignment = Str(dr["TerritoryRegionAssignment"]),
                        ClientSiteAccess = Str(dr["ClientSiteAccess"]),
                        IsActive = Convert.ToBoolean(dr["IsActive"])
                    };
                }
            }
        }

        private void LoadRecords()
        {
            Records.Clear();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
            SELECT wl.WorkerLocationID, wl.EmployeeID,
                   e.EmployeeCode, e.FirstName + ' ' + e.LastName AS EmployeeName,
                   wl.PrimaryLocationID, pl.LocationName AS PrimaryLocationName,
                   wl.SecondaryLocationID, sl.LocationName AS SecondaryLocationName,
                   wl.WorkLocationTypeID, wlt.WorkLocationTypeName,
                   wl.WorkArrangementID, wa.WorkArrangementName,
                   wl.HybridSchedule, wl.TerritoryRegionAssignment, wl.ClientSiteAccess,
                   wl.IsActive
            FROM tblWorkerLocation wl
            INNER JOIN tblEmployee e ON e.EmployeeID = wl.EmployeeID
            LEFT JOIN tblLocation pl ON pl.LocationID = wl.PrimaryLocationID
            LEFT JOIN tblLocation sl ON sl.LocationID = wl.SecondaryLocationID
            LEFT JOIN tblWorkLocationType wlt ON wlt.WorkLocationTypeID = wl.WorkLocationTypeID
            LEFT JOIN tblWorkArrangement wa ON wa.WorkArrangementID = wl.WorkArrangementID
            ORDER BY wl.IsActive DESC, e.FirstName, e.LastName;", conn))
            {
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Records.Add(new WorkerLocationRecord
                        {
                            WorkerLocationID = Convert.ToInt32(dr["WorkerLocationID"]),
                            EmployeeID = IntOrZero(dr["EmployeeID"]),
                            EmployeeCode = Str(dr["EmployeeCode"]),
                            EmployeeName = Str(dr["EmployeeName"]),
                            PrimaryLocationID = IntOrZero(dr["PrimaryLocationID"]),
                            PrimaryLocationName = Str(dr["PrimaryLocationName"]),
                            SecondaryLocationID = IntOrZero(dr["SecondaryLocationID"]),
                            SecondaryLocationName = Str(dr["SecondaryLocationName"]),
                            WorkLocationTypeID = IntOrZero(dr["WorkLocationTypeID"]),
                            WorkLocationTypeName = Str(dr["WorkLocationTypeName"]),
                            WorkArrangementID = IntOrZero(dr["WorkArrangementID"]),
                            WorkArrangementName = Str(dr["WorkArrangementName"]),
                            HybridSchedule = Str(dr["HybridSchedule"]),
                            TerritoryRegionAssignment = Str(dr["TerritoryRegionAssignment"]),
                            ClientSiteAccess = Str(dr["ClientSiteAccess"]),
                            IsActive = Convert.ToBoolean(dr["IsActive"])
                        });
                    }
                }
            }
        }

        private static int IntOrZero(object v)
            => v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);

        private static string Str(object v)
            => v == null || v == DBNull.Value ? "" : v.ToString() ?? "";
    }
}
