<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="UserSetup.aspx.cs" Inherits="HRMS.UserSetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<div class="card">
    <div class="card-header"><h2><%= Input.UserID > 0 ? "Edit User" : "Add User" %></h2></div>
    <div class="card-body">
        <input type="hidden" name="userId" value="<%= Input.UserID %>" />
        <div class="form-grid">
            <div class="form-group"><label>User Code</label><input type="text" name="userCode" class="form-control" value="<%= Server.HtmlEncode(Input.UserCode) %>" /></div>
            <div class="form-group"><label>Username *</label><input type="text" name="username" class="form-control" value="<%= Server.HtmlEncode(Input.Username) %>" /></div>
            <div class="form-group"><label>Full Name *</label><input type="text" name="fullName" class="form-control" value="<%= Server.HtmlEncode(Input.FullName) %>" /></div>
            <div class="form-group"><label>Email</label><input type="text" name="email" class="form-control" value="<%= Server.HtmlEncode(Input.Email) %>" /></div>
            <div class="form-group"><label>Password <%= Input.UserID > 0 ? "(leave blank to keep; user must change it at next sign-in)" : "*" %></label><input type="password" name="newPassword" class="form-control" /></div>
            <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="isActive" value="true" <%= Input.IsActive ? "checked" : "" %> /> Active</label></div>
            <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="isAdmin" value="true" <%= Input.IsAdmin ? "checked" : "" %> /> Admin</label></div>
            <% if (Input.UserID > 0) { %>
            <div class="form-group"><label>Security</label>
                <div>
                    <% if (Input.TwoFactorEnabled) { %><span class="badge badge-success">2FA on</span><% } else { %><span class="badge badge-info">2FA off</span><% } %>
                    <% if (Input.IsLocked) { %><span class="badge badge-danger">Locked</span><% } %>
                </div>
            </div>
            <% } %>
        </div>
        <p class="text-muted" style="margin-top:.5rem;font-size:.85rem;"><%= Server.HtmlEncode(HRMS.Services.PasswordHelper.PolicySummary) %></p>
    </div>
    <div class="card-footer">
        <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">Save</button>
        <a href="/UserSetup.aspx" class="btn btn-secondary">Clear</a>
        <% if (Input.UserID > 0 && Input.TwoFactorEnabled) { %>
        <button type="submit" class="btn btn-secondary" onclick="if(!confirm('Disable 2FA for this user?')) return false; document.getElementById('__handler').value='Disable2FA';">Disable 2FA</button>
        <% } %>
        <% if (Input.UserID > 0 && Input.IsLocked) { %>
        <button type="submit" class="btn btn-secondary" onclick="document.getElementById('__handler').value='Unlock';">Clear lockout</button>
        <% } %>
    </div>
</div>
<div class="card mt-4"><div class="card-header"><h2>Users</h2></div>
<div class="card-body table-responsive"><table class="data-table">
<thead><tr><th>Code</th><th>Username</th><th>Name</th><th>Email</th><th>Admin</th><th>Status</th><th></th></tr></thead>
<tbody>
<% foreach (var u in Users) { %>
<tr>
<td><%= Server.HtmlEncode(u.UserCode) %></td><td><%= Server.HtmlEncode(u.Username) %></td>
<td><%= Server.HtmlEncode(u.FullName) %></td><td><%= Server.HtmlEncode(u.Email) %></td>
<td><%= u.IsAdmin ? "Yes" : "No" %></td><td><%= u.IsActive ? "Active" : "Inactive" %></td>
<td><a href="/UserSetup.aspx?editId=<%= u.UserID %>">Edit</a>
<button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= u.UserID %>';return confirm('Deactivate?');">X</button></td>
</tr>
<% } %>
</tbody></table></div></div>
</asp:Content>
