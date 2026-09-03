<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.JobSetupPage" %>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<input type="hidden" name="jobID" value="<%= Input.JobID %>" />

<div class="card">
  <div class="card-header">
    <h2><%= Input.JobID > 0 ? "Edit Job" : "Add Job" %></h2>
  </div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group">
        <label>Job Title <span class="required">*</span></label>
        <input type="text" name="jobTitle" class="form-control" value="<%= Server.HtmlEncode(Input.JobTitle) %>" maxlength="200" />
      </div>
      <div class="form-group">
        <label>Job Code <span class="required">*</span></label>
        <input type="text" name="jobCode" class="form-control" value="<%= Server.HtmlEncode(Input.JobCode) %>" maxlength="50"
               <%= Input.JobID == 0 ? "readonly" : "" %>
               style="<%= Input.JobID == 0 ? "background:#f8fafc;color:var(--text-muted);cursor:not-allowed;" : "" %>" />
      </div>
      <div class="form-group">
        <label>Job Grade <span class="required">*</span></label>
        <select name="gradeID" class="form-control">
          <option value="">-- Select Grade --</option>
          <% foreach (var g in Grades) { %>
          <option value="<%= g.Id %>" <%= Input.GradeID == g.Id ? "selected" : "" %>><%= Server.HtmlEncode(g.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Job Level <span class="required">*</span></label>
        <select name="jobLevel" class="form-control">
          <option value="">-- Select Level --</option>
          <% foreach (var lvl in JobLevels) { %>
          <option value="<%= Server.HtmlEncode(lvl) %>" <%= Input.JobLevel == lvl ? "selected" : "" %>><%= Server.HtmlEncode(lvl) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Position Number <span class="required">*</span></label>
        <input type="text" name="positionNumber" class="form-control" value="<%= Server.HtmlEncode(Input.PositionNumber) %>" maxlength="50"
               <%= Input.JobID == 0 ? "readonly" : "" %>
               style="<%= Input.JobID == 0 ? "background:#f8fafc;color:var(--text-muted);cursor:not-allowed;" : "" %>" />
      </div>
      <div class="form-group">
        <label>Reports To (Supervisor) <span class="required">*</span></label>
        <select name="reportsToEmployeeID" class="form-control">
          <option value="">-- Select Employee --</option>
          <% foreach (var emp in Employees) { %>
          <option value="<%= emp.Id %>" <%= Input.ReportsToEmployeeID == emp.Id ? "selected" : "" %>><%= Server.HtmlEncode(emp.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Functional Manager</label>
        <select name="functionalManagerEmployeeID" class="form-control">
          <option value="">-- Select Employee --</option>
          <% foreach (var emp in Employees) { %>
          <option value="<%= emp.Id %>" <%= Input.FunctionalManagerEmployeeID == emp.Id ? "selected" : "" %>><%= Server.HtmlEncode(emp.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Dotted Line Manager</label>
        <select name="dottedLineManagerEmployeeID" class="form-control">
          <option value="">-- Select Employee --</option>
          <% foreach (var emp in Employees) { %>
          <option value="<%= emp.Id %>" <%= Input.DottedLineManagerEmployeeID == emp.Id ? "selected" : "" %>><%= Server.HtmlEncode(emp.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Backup Approver <span class="required">*</span></label>
        <select name="backupApproverEmployeeID" class="form-control">
          <option value="">-- Select Employee --</option>
          <% foreach (var emp in Employees) { %>
          <option value="<%= emp.Id %>" <%= Input.BackupApproverEmployeeID == emp.Id ? "selected" : "" %>><%= Server.HtmlEncode(emp.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Status</label>
        <label class="checkbox-label">
          <input type="checkbox" name="isActive" value="true" <%= Input.IsActive ? "checked" : "" %> /> Active
        </label>
      </div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">
      <%= Input.JobID > 0 ? "Update" : "Save" %>
    </button>
    <a href="<%= ResolveUrl("~/JobSetup.aspx") %>" class="btn btn-secondary">Clear</a>
  </div>
</div>

<div class="card mt-4">
  <div class="card-header space-between">
    <h2>Job List</h2>
    <input type="text" id="txtSearch" class="form-control" style="width:220px" placeholder="Search…" onkeyup="searchTable(this.value)" />
  </div>
  <div class="card-body table-responsive">
    <table class="data-table" id="dataTable">
      <thead class="grid-header">
        <tr>
          <th>Code</th>
          <th>Title</th>
          <th>Grade</th>
          <th>Level</th>
          <th>Position #</th>
          <th>Reports To</th>
          <th>Backup Approver</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <% if (Records.Count == 0) { %>
        <tr class="empty-row"><td colspan="9">No records found.</td></tr>
        <% } else {
             foreach (var item in Records) { %>
        <tr>
          <td><code style="font-size:.75rem;background:#f1f5f9;padding:.1rem .4rem;border-radius:4px;"><%= Server.HtmlEncode(item.JobCode) %></code></td>
          <td><%= Server.HtmlEncode(item.JobTitle) %></td>
          <td><%= Server.HtmlEncode(item.GradeName) %></td>
          <td><%= Server.HtmlEncode(item.JobLevel) %></td>
          <td><%= Server.HtmlEncode(item.PositionNumber) %></td>
          <td><%= Server.HtmlEncode(item.ReportsToName) %></td>
          <td><%= Server.HtmlEncode(item.BackupApproverName) %></td>
          <td><span class="badge <%= item.IsActive ? "badge-success" : "badge-danger" %>"><%= item.IsActive ? "Active" : "Inactive" %></span></td>
          <td class="actions-col">
            <a class="btn-icon btn-edit" href="<%= ResolveUrl("~/JobSetup.aspx") %>?editId=<%= item.JobID %>">Edit</a>
            <button type="submit" class="btn-icon btn-delete"
                    onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= item.JobID %>';return confirm('Deactivate this job?');">X</button>
          </td>
        </tr>
        <% } } %>
      </tbody>
    </table>
  </div>
</div>
</asp:Content>

<asp:Content ID="Scripts" ContentPlaceHolderID="scripts" runat="server">
<script>
function searchTable(q) {
  q = (q || '').toLowerCase();
  document.querySelectorAll('#dataTable tbody tr').forEach(function (r) {
    r.style.display = r.innerText.toLowerCase().includes(q) ? '' : 'none';
  });
}
</script>
</asp:Content>
