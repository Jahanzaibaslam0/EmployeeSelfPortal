<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.ExtensionSetupPage" %>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<input type="hidden" name="extensionID" value="<%= Input.ExtensionID %>" />

<div class="card">
  <div class="card-header">
    <h2><%= Input.ExtensionID > 0 ? "Edit Extension" : "Add Extension" %></h2>
  </div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group">
        <label>Code <span class="required">*</span></label>
        <input type="text" name="extensionCode" class="form-control"
               value="<%= Server.HtmlEncode(Input.ExtensionCode) %>"
               <%= Input.ExtensionID == 0 ? "readonly" : "" %>
               style="<%= Input.ExtensionID == 0 ? "background:#f8fafc;color:var(--text-muted);cursor:not-allowed;" : "" %>" />
      </div>
      <div class="form-group">
        <label>Name <span class="required">*</span></label>
        <input type="text" name="extensionName" class="form-control"
               value="<%= Server.HtmlEncode(Input.ExtensionName) %>" maxlength="150"
               placeholder="Extension name" />
      </div>
      <div class="form-group">
        <label>Alias</label>
        <input type="text" name="aliasName" class="form-control"
               value="<%= Server.HtmlEncode(Input.AliasName) %>" maxlength="100"
               placeholder="Short name / abbreviation" />
      </div>
      <div class="form-group">
        <label>Department</label>
        <select name="departmentID" class="form-control">
          <option value="">-- Select Department --</option>
          <% foreach (var dept in Departments) { %>
          <option value="<%= dept.Id %>" <%= Input.DepartmentID == dept.Id ? "selected" : "" %>><%= Server.HtmlEncode(dept.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Location</label>
        <select name="locationID" class="form-control">
          <option value="">-- Select Location --</option>
          <% foreach (var loc in Locations) { %>
          <option value="<%= loc.Id %>" <%= Input.LocationID == loc.Id ? "selected" : "" %>><%= Server.HtmlEncode(loc.Name) %></option>
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
      <%= Input.ExtensionID > 0 ? "Update" : "Save" %>
    </button>
    <a href="<%= ResolveUrl("~/ExtensionSetup.aspx") %>" class="btn btn-secondary">Clear</a>
  </div>
</div>

<div class="card mt-4">
  <div class="card-header space-between">
    <h2>Extension List</h2>
    <input type="text" id="txtSearch" class="form-control" style="width:220px"
           placeholder="Search…" onkeyup="searchTable(this.value)" />
  </div>
  <div class="card-body table-responsive">
    <table class="data-table" id="dataTable">
      <thead class="grid-header">
        <tr>
          <th>Code</th>
          <th>Name</th>
          <th>Alias</th>
          <th>Department</th>
          <th>Location</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <% if (Records.Count == 0) { %>
        <tr class="empty-row"><td colspan="7">No records found.</td></tr>
        <% } else {
             foreach (var item in Records) { %>
        <tr>
          <td><code style="font-size:.75rem;background:#f1f5f9;padding:.1rem .4rem;border-radius:4px;"><%= Server.HtmlEncode(item.ExtensionCode) %></code></td>
          <td><%= Server.HtmlEncode(item.ExtensionName) %></td>
          <td><%= Server.HtmlEncode(item.AliasName) %></td>
          <td><%= Server.HtmlEncode(item.DepartmentName) %></td>
          <td><%= Server.HtmlEncode(item.LocationName) %></td>
          <td><span class="badge <%= item.IsActive ? "badge-success" : "badge-danger" %>"><%= item.IsActive ? "Active" : "Inactive" %></span></td>
          <td class="actions-col">
            <a class="btn-icon btn-edit" href="<%= ResolveUrl("~/ExtensionSetup.aspx") %>?editId=<%= item.ExtensionID %>">Edit</a>
            <button type="submit" class="btn-icon btn-delete"
                    onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= item.ExtensionID %>';return confirm('Remove this extension?');">X</button>
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
