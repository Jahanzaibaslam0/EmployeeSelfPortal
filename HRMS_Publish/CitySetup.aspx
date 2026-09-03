<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CitySetup.aspx.cs" Inherits="HRMS.CitySetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } %>
<div class="card">
    <div class="card-header"><h2><%= Input.CityID > 0 ? "Edit City" : "Add City" %></h2></div>
    <div class="card-body">
        <input type="hidden" name="__handler" value="Save" />
        <input type="hidden" name="cityID" value="<%= Input.CityID %>" />
        <div class="form-grid">
            <div class="form-group">
                <label>Code <span class="required">*</span></label>
                <input type="text" name="cityCode" class="form-control" value="<%= Server.HtmlEncode(Input.CityCode) %>" maxlength="20" />
            </div>
            <div class="form-group">
                <label>Name <span class="required">*</span></label>
                <input type="text" name="cityName" class="form-control" value="<%= Server.HtmlEncode(Input.CityName) %>" maxlength="150" />
            </div>
            <div class="form-group">
                <label>Alias</label>
                <input type="text" name="aliasName" class="form-control" value="<%= Server.HtmlEncode(Input.AliasName) %>" maxlength="100" />
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
        <button type="submit" class="btn btn-primary"><%= Input.CityID > 0 ? "Update" : "Save" %></button>
        <a href="/CitySetup.aspx" class="btn btn-secondary">Clear</a>
    </div>
</div>
<div class="card mt-4">
    <div class="card-header space-between">
        <h2>City List</h2>
        <input type="text" id="txtSearch" class="form-control" style="width:220px" placeholder="Search…" onkeyup="searchTable(this.value)" />
    </div>
    <div class="card-body table-responsive">
        <table class="data-table" id="dataTable">
            <thead class="grid-header"><tr><th>Code</th><th>Name</th><th>Alias</th><th>Status</th><th>Actions</th></tr></thead>
            <tbody>
            <% if (Records.Count == 0) { %>
                <tr class="empty-row"><td colspan="5">No records found.</td></tr>
            <% } else { foreach (var item in Records) { %>
                <tr>
                    <td><code><%= Server.HtmlEncode(item.CityCode) %></code></td>
                    <td><%= Server.HtmlEncode(item.CityName) %></td>
                    <td><%= Server.HtmlEncode(item.AliasName) %></td>
                    <td><span class="badge <%= item.IsActive ? "badge-success" : "badge-danger" %>"><%= item.IsActive ? "Active" : "Inactive" %></span></td>
                    <td class="actions-col">
                        <a class="btn-icon btn-edit" href="/CitySetup.aspx?editId=<%= item.CityID %>">Edit</a>
                        <button type="submit" class="btn-icon btn-delete" name="__handler" value="Delete"
                                onclick="this.form.deleteId.value='<%= item.CityID %>'; return confirm('Remove this city?');">X</button>
                    </td>
                </tr>
            <% } } %>
            </tbody>
        </table>
        <input type="hidden" name="deleteId" value="0" />
    </div>
</div>
<script>
function searchTable(q) {
    q = (q || '').toLowerCase();
    document.querySelectorAll('#dataTable tbody tr').forEach(function (r) {
        r.style.display = r.innerText.toLowerCase().includes(q) ? '' : 'none';
    });
}
</script>
</asp:Content>
