<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="WorkLocationTypeSetup.aspx.cs" Inherits="HRMS.WorkLocationTypeSetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<div class="card">
    <div class="card-header"><h2><%= Input.Id > 0 ? "Edit " + ItemLabel : "Add " + ItemLabel %></h2></div>
    <div class="card-body">
        <input type="hidden" name="__handler" id="__handler" value="Save" /><input type="hidden" name="itemId" value="<%= Input.Id %>" /><input type="hidden" name="deleteId" id="deleteId" value="0" />
        <div class="form-grid">
            <div class="form-group"><label>Code *</label><input type="text" name="itemCode" class="form-control" value="<%= Server.HtmlEncode(Input.Code) %>" /></div>
            <div class="form-group"><label>Name *</label><input type="text" name="itemName" class="form-control" value="<%= Server.HtmlEncode(Input.Name) %>" /></div>
            <div class="form-group"><label>Alias</label><input type="text" name="aliasName" class="form-control" value="<%= Server.HtmlEncode(Input.AliasName) %>" /></div>
            <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="isActive" value="true" <%= Input.IsActive ? "checked" : "" %> /> Active</label></div>
        </div>
    </div>
    <div class="card-footer"><button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">Save</button>
        <a href="/<%= PagePath %>.aspx" class="btn btn-secondary">Clear</a></div>
</div>
<div class="card mt-4"><div class="card-header"><h2>List</h2></div><div class="card-body table-responsive"><table class="data-table">
<thead><tr><th>Code</th><th>Name</th><th>Alias</th><th>Status</th><th></th></tr></thead><tbody>
<% foreach (var item in Records) { %><tr>
<td><%= Server.HtmlEncode(item.Code) %></td><td><%= Server.HtmlEncode(item.Name) %></td><td><%= Server.HtmlEncode(item.AliasName) %></td><td><%= item.IsActive ? "Active" : "Inactive" %></td>
<td><a href="/<%= PagePath %>.aspx?editId=<%= item.Id %>">Edit</a>
<button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= item.Id %>';return confirm('Remove?');">X</button></td></tr><% } %>
</tbody></table></div></div>
</asp:Content>
