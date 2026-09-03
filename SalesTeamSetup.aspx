<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.SalesTeamSetupPage" %>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<input type="hidden" name="salesTeamID" value="<%= Input.SalesTeamID %>" />

<div class="card">
  <div class="card-header">
    <h2><%= Input.SalesTeamID > 0 ? "Edit Sales Team" : "Add Sales Team" %></h2>
  </div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group">
        <label>Code <span class="required">*</span></label>
        <input type="text" name="salesTeamCode" class="form-control" value="<%= Server.HtmlEncode(Input.SalesTeamCode) %>" maxlength="20" placeholder="e.g. ST-001" />
      </div>
      <div class="form-group">
        <label>Name <span class="required">*</span></label>
        <input type="text" name="salesTeamName" class="form-control" value="<%= Server.HtmlEncode(Input.SalesTeamName) %>" maxlength="150" placeholder="Sales team name" />
      </div>
      <div class="form-group">
        <label>Division</label>
        <select name="divisionID" class="form-control">
          <option value="">-- Select Division --</option>
          <% foreach (var div in Divisions) { %>
          <option value="<%= div.Id %>" <%= Input.DivisionID == div.Id ? "selected" : "" %>><%= Server.HtmlEncode(div.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Alias</label>
        <input type="text" name="aliasName" class="form-control" value="<%= Server.HtmlEncode(Input.AliasName) %>" maxlength="100" placeholder="Short name / abbreviation" />
      </div>
      <div class="form-group">
        <label>Status</label>
        <label class="checkbox-label">
          <input type="checkbox" name="isActive" value="true" <%= Input.IsActive ? "checked" : "" %> /> Active
        </label>
      </div>
      <div class="form-group full-width">
        <label>Description</label>
        <textarea name="description" class="form-control" rows="3" maxlength="500" placeholder="Brief description..."><%= Server.HtmlEncode(Input.Description) %></textarea>
      </div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">
      <%= Input.SalesTeamID > 0 ? "Update" : "Save" %>
    </button>
    <a href="<%= ResolveUrl("~/SalesTeamSetup.aspx") %>" class="btn btn-secondary">Clear</a>
  </div>
</div>

<div class="card mt-4">
  <div class="card-header space-between">
    <h2>Sales Team List</h2>
    <input type="text" id="txtSearch" class="form-control" style="width:220px" placeholder="Search…" onkeyup="searchTable(this.value)" />
  </div>
  <div class="card-body table-responsive">
    <table class="data-table" id="dataTable">
      <thead class="grid-header">
        <tr>
          <th>Code</th>
          <th>Name</th>
          <th>Division</th>
          <th>Alias</th>
          <th>Description</th>
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
          <td><code style="font-size:.75rem;background:#f1f5f9;padding:.1rem .4rem;border-radius:4px;"><%= Server.HtmlEncode(item.SalesTeamCode) %></code></td>
          <td><%= Server.HtmlEncode(item.SalesTeamName) %></td>
          <td><%= Server.HtmlEncode(item.DivisionName) %></td>
          <td><%= Server.HtmlEncode(item.AliasName) %></td>
          <td><%= Server.HtmlEncode(item.Description) %></td>
          <td><span class="badge <%= item.IsActive ? "badge-success" : "badge-danger" %>"><%= item.IsActive ? "Active" : "Inactive" %></span></td>
          <td class="actions-col">
            <a class="btn-icon btn-edit" href="<%= ResolveUrl("~/SalesTeamSetup.aspx") %>?editId=<%= item.SalesTeamID %>">Edit</a>
            <button type="submit" class="btn-icon btn-delete"
                    onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= item.SalesTeamID %>';return confirm('Remove this sales team?');">X</button>
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
