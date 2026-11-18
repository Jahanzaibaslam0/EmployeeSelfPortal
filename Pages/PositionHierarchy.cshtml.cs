using HRMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace HRMS.Pages;

public class PositionHierarchyFlat
{
    public int PositionID { get; set; }
    public string PositionNo { get; set; } = "";
    public string Description { get; set; } = "";
    public int ReportsToPositionID { get; set; }
    public string ReportsToPositionNo { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string TitleName { get; set; } = "";
    public string PositionTypeName { get; set; } = "";
    public string PositionDuration { get; set; } = "";
    public string AssignedWorkers { get; set; } = "";
    public bool IsActive { get; set; }
}

public class PositionHierarchyNode
{
    public PositionHierarchyFlat Data { get; set; } = new();
    public List<PositionHierarchyNode> Children { get; set; } = new();
    public int Depth { get; set; }
}

public class PositionHierarchyModel : PageModel
{
    private readonly string _conn;

    public string PageTitle => "Position Hierarchy";
    public List<PositionHierarchyNode> RootNodes { get; set; } = new();
    public List<PositionHierarchyFlat> OrphanNodes { get; set; } = new();
    public List<PositionHierarchyFlat> AllPositions { get; set; } = new();
    public bool ActiveOnly { get; set; } = true;
    public int TotalPositions { get; set; }
    public int RootCount { get; set; }
    public int MaxDepth { get; set; }

    public PositionHierarchyModel(IConfiguration config)
    {
        _conn = config.GetConnectionString("HRMSConnection")!;
    }

    public void OnGet(bool? activeOnly)
    {
        ActiveOnly = activeOnly ?? true;
        AllPositions = LoadPositions(ActiveOnly);
        TotalPositions = AllPositions.Count;
        BuildHierarchy(AllPositions);
    }

    private List<PositionHierarchyFlat> LoadPositions(bool activeOnly)
    {
        var list = new List<PositionHierarchyFlat>();
        using var conn = new SqlConnection(_conn);
        using var cmd = new SqlCommand(@"
            SELECT p.PositionID, p.PositionNo, ISNULL(p.Description, '') AS Description,
                   ISNULL(p.ReportsToPositionID, 0) AS ReportsToPositionID,
                   ISNULL(rp.PositionNo, '') AS ReportsToPositionNo,
                   ISNULL(j.JobTitle, '') AS JobTitle,
                   ISNULL(d.DepartmentName, '') AS DepartmentName,
                   ISNULL(t.DesignationLevelName, '') AS TitleName,
                   ISNULL(et.EmploymentTypeName, '') AS PositionTypeName,
                   ISNULL(p.PositionDuration, '') AS PositionDuration,
                   p.IsActive,
                   STUFF((
                       SELECT ', ' + e.EmployeeCode + ' – ' + e.FirstName + ' ' + e.LastName
                       FROM tblPositionWorkerAssignment a
                       INNER JOIN tblEmployee e ON e.EmployeeID = a.EmployeeID
                       WHERE a.PositionID = p.PositionID
                       ORDER BY a.SortOrder, a.PositionWorkerAssignmentID
                       FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS AssignedWorkers
            FROM tblPosition p
            LEFT JOIN tblPosition rp ON rp.PositionID = p.ReportsToPositionID
            LEFT JOIN tblJob j ON j.JobID = p.JobID
            LEFT JOIN tblDepartment d ON d.DepartmentID = p.DepartmentID
            LEFT JOIN tblDesignationLevel t ON t.DesignationLevelID = p.TitleID
            LEFT JOIN tblEmploymentType et ON et.EmploymentTypeID = p.PositionTypeID
            WHERE (@ActiveOnly = 0 OR p.IsActive = 1)
            ORDER BY p.PositionNo;", conn);
        cmd.Parameters.AddWithValue("@ActiveOnly", activeOnly ? 1 : 0);
        conn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            list.Add(new PositionHierarchyFlat
            {
                PositionID = Convert.ToInt32(dr["PositionID"]),
                PositionNo = dr["PositionNo"].ToString() ?? "",
                Description = dr["Description"].ToString() ?? "",
                ReportsToPositionID = Convert.ToInt32(dr["ReportsToPositionID"]),
                ReportsToPositionNo = dr["ReportsToPositionNo"].ToString() ?? "",
                JobTitle = dr["JobTitle"].ToString() ?? "",
                DepartmentName = dr["DepartmentName"].ToString() ?? "",
                TitleName = dr["TitleName"].ToString() ?? "",
                PositionTypeName = dr["PositionTypeName"].ToString() ?? "",
                PositionDuration = dr["PositionDuration"].ToString() ?? "",
                AssignedWorkers = dr["AssignedWorkers"] == DBNull.Value ? "" : dr["AssignedWorkers"].ToString() ?? "",
                IsActive = Convert.ToBoolean(dr["IsActive"])
            });
        }
        return list;
    }

    private void BuildHierarchy(List<PositionHierarchyFlat> flat)
    {
        RootNodes.Clear();
        OrphanNodes.Clear();
        MaxDepth = 0;

        if (flat.Count == 0) return;

        var lookup = flat.ToDictionary(p => p.PositionID);
        var nodes = flat.ToDictionary(
            p => p.PositionID,
            p => new PositionHierarchyNode { Data = p });

        bool IsRoot(PositionHierarchyFlat p)
        {
            if (p.ReportsToPositionID <= 0) return true;
            if (p.ReportsToPositionID == p.PositionID) return true;
            if (!lookup.ContainsKey(p.ReportsToPositionID)) return true;
            return false;
        }

        foreach (var p in flat)
        {
            var node = nodes[p.PositionID];
            if (IsRoot(p))
            {
                RootNodes.Add(node);
                continue;
            }

            nodes[p.ReportsToPositionID].Children.Add(node);
        }

        RootNodes = RootNodes
            .OrderBy(n => n.Data.PositionNo)
            .ToList();

        foreach (var root in RootNodes)
            SortChildren(root);

        var visited = new HashSet<int>();
        foreach (var root in RootNodes)
            MarkVisited(root, visited, 0);

        foreach (var p in flat.Where(p => !visited.Contains(p.PositionID)))
            OrphanNodes.Add(p);

        OrphanNodes = OrphanNodes.OrderBy(p => p.PositionNo).ToList();
        RootCount = RootNodes.Count + OrphanNodes.Count;
    }

    private void SortChildren(PositionHierarchyNode node)
    {
        node.Children = node.Children.OrderBy(c => c.Data.PositionNo).ToList();
        foreach (var child in node.Children)
            SortChildren(child);
    }

    private void MarkVisited(PositionHierarchyNode node, HashSet<int> visited, int depth)
    {
        if (visited.Contains(node.Data.PositionID)) return;
        visited.Add(node.Data.PositionID);
        node.Depth = depth;
        if (depth > MaxDepth) MaxDepth = depth;
        foreach (var child in node.Children)
            MarkVisited(child, visited, depth + 1);
    }
}
