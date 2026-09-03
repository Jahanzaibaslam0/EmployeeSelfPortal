<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="DepartmentSetup.aspx.cs" Inherits="HRMS.DepartmentSetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" /><input type="hidden" name="deleteId" id="deleteId" value="0" />
<div class="card"><div class="card-header"><h2><%= Input.Id > 0 ? "Edit Department" : "Add Department" %></h2></div>
<div class="card-body"><input type="hidden" name="itemId" value="<%= Input.Id %>" />
<div class="form-grid">
<div class="form-group"><label>Division</label>
<select name="divisionID" class="form-control"><option value="0">-- None --</option>
<% foreach (var d in Divisions) { %><option value="<%= d.Id %>" <%= Input.DivisionID==d.Id?"selected":"" %>><%= Server.HtmlEncode(d.Name) %></option><% } %>
</select></div>
<div class="form-group"><label>Name *</label><input type="text" name="itemName" class="form-control" value="<%= Server.HtmlEncode(Input.Name) %>" /></div>
<div class="form-group"><label>Alias</label><input type="text" name="aliasName" class="form-control" value="<%= Server.HtmlEncode(Input.AliasName) %>" /></div>
<div class="form-group"><label class="checkbox-label"><input type="checkbox" name="isActive" value="true" <%= Input.IsActive?"checked":"" %> /> Active</label></div>
</div></div>
<div class="card-footer"><button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">Save</button>
<a href="/DepartmentSetup.aspx" class="btn btn-secondary">Clear</a></div></div>
<div class="card mt-4"><div class="card-header"><h2>Departments</h2></div>
<div class="card-body table-responsive"><table class="data-table">
<thead><tr><th>Division</th><th>Name</th><th>Alias</th><th>Status</th><th></th></tr></thead><tbody>
<% foreach (var r in Records) { %><tr>
<td><%= Server.HtmlEncode(r.DivisionName) %></td><td><%= Server.HtmlEncode(r.Name) %></td>
<td><%= Server.HtmlEncode(r.AliasName) %></td><td><%= r.IsActive?"Active":"Inactive" %></td>
<td><a href="/DepartmentSetup.aspx?editId=<%= r.Id %>">Edit</a>
<button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= r.Id %>';return confirm('Remove?');">X</button></td>
</tr><% } %>
</tbody></table></div></div>
</asp:Content>
