<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.WorkerLocationSetupPage" %>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<input type="hidden" name="workerLocationID" value="<%= Input.WorkerLocationID %>" />

<div class="card">
  <div class="card-header">
    <h2><%= Input.WorkerLocationID > 0 ? "Edit Worker Location" : "Add Worker Location" %></h2>
  </div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group">
        <label>Employee <span class="required">*</span></label>
        <select name="employeeID" class="form-control">
          <option value="">-- Select Employee --</option>
          <% foreach (var emp in Employees) { %>
          <option value="<%= emp.Id %>" <%= Input.EmployeeID == emp.Id ? "selected" : "" %>><%= Server.HtmlEncode(emp.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Primary Location <span class="required">*</span></label>
        <select name="primaryLocationID" class="form-control">
          <option value="">-- Select Location --</option>
          <% foreach (var loc in Locations) { %>
          <option value="<%= loc.Id %>" <%= Input.PrimaryLocationID == loc.Id ? "selected" : "" %>><%= Server.HtmlEncode(loc.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Secondary Location</label>
        <select name="secondaryLocationID" class="form-control">
          <option value="">-- Select Location --</option>
          <% foreach (var loc in Locations) { %>
          <option value="<%= loc.Id %>" <%= Input.SecondaryLocationID == loc.Id ? "selected" : "" %>><%= Server.HtmlEncode(loc.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Work Location Type</label>
        <select name="workLocationTypeID" class="form-control">
          <option value="">-- Select Type --</option>
          <% foreach (var wlt in WorkLocationTypes) { %>
          <option value="<%= wlt.Id %>" <%= Input.WorkLocationTypeID == wlt.Id ? "selected" : "" %>><%= Server.HtmlEncode(wlt.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Work Arrangement</label>
        <select name="workArrangementID" class="form-control">
          <option value="">-- Select Arrangement --</option>
          <% foreach (var wa in WorkArrangements) { %>
          <option value="<%= wa.Id %>" <%= Input.WorkArrangementID == wa.Id ? "selected" : "" %>><%= Server.HtmlEncode(wa.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Hybrid Schedule</label>
        <input type="text" name="hybridSchedule" class="form-control" value="<%= Server.HtmlEncode(Input.HybridSchedule) %>" maxlength="200"
               placeholder="e.g. Mon-Wed office, Thu-Fri remote" />
      </div>
      <div class="form-group">
        <label>Territory / Region Assignment</label>
        <input type="text" name="territoryRegionAssignment" class="form-control" value="<%= Server.HtmlEncode(Input.TerritoryRegionAssignment) %>" maxlength="200"
               placeholder="e.g. North Region" />
      </div>
      <div class="form-group">
        <label>Client Site Access</label>
        <input type="text" name="clientSiteAccess" class="form-control" value="<%= Server.HtmlEncode(Input.ClientSiteAccess) %>" maxlength="200"
               placeholder="e.g. Client A, Client B" />
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
      <%= Input.WorkerLocationID > 0 ? "Update" : "Save" %>
    </button>
    <a href="<%= ResolveUrl("~/WorkerLocationSetup.aspx") %>" class="btn btn-secondary">Clear</a>
  </div>
</div>

<div class="card mt-4">
  <div class="card-header space-between">
    <h2>Worker Location List</h2>
    <input type="text" id="txtSearch" class="form-control" style="width:220px" placeholder="Search…" onkeyup="searchTable(this.value)" />
  </div>
  <div class="card-body table-responsive">
    <table class="data-table" id="dataTable">
      <thead class="grid-header">
        <tr>
          <th>Employee</th>
          <th>Primary Location</th>
          <th>Secondary Location</th>
          <th>Location Type</th>
          <th>Arrangement</th>
          <th>Hybrid Schedule</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <% if (Records.Count == 0) { %>
        <tr class="empty-row"><td colspan="8">No records found.</td></tr>
        <% } else {
             foreach (var item in Records) { %>
        <tr>
          <td><%= Server.HtmlEncode(item.EmployeeCode) %> — <%= Server.HtmlEncode(item.EmployeeName) %></td>
          <td><%= Server.HtmlEncode(item.PrimaryLocationName) %></td>
          <td><%= Server.HtmlEncode(item.SecondaryLocationName) %></td>
          <td><%= Server.HtmlEncode(item.WorkLocationTypeName) %></td>
          <td><%= Server.HtmlEncode(item.WorkArrangementName) %></td>
          <td><%= Server.HtmlEncode(item.HybridSchedule) %></td>
          <td><span class="badge <%= item.IsActive ? "badge-success" : "badge-danger" %>"><%= item.IsActive ? "Active" : "Inactive" %></span></td>
          <td class="actions-col">
            <a class="btn-icon btn-edit" href="<%= ResolveUrl("~/WorkerLocationSetup.aspx") %>?editId=<%= item.WorkerLocationID %>">Edit</a>
            <button type="submit" class="btn-icon btn-delete"
                    onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= item.WorkerLocationID %>';return confirm('Deactivate this worker location?');">X</button>
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
