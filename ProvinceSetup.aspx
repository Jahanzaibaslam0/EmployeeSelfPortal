<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ProvinceSetup.aspx.cs" Inherits="HRMS.ProvinceSetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<div class="card">
    <div class="card-header"><h2><%= Input.ProvinceID > 0 ? "Edit Province" : "Add Province" %></h2></div>
    <div class="card-body">
        <input type="hidden" name="__handler" id="__handler" value="Save" />
        <input type="hidden" name="provinceID" value="<%= Input.ProvinceID %>" />
        <input type="hidden" name="deleteId" id="deleteId" value="0" />
        <div class="form-grid">
            <div class="form-group"><label>Code *</label>
                <input type="text" name="provinceCode" class="form-control" value="<%= Server.HtmlEncode(Input.ProvinceCode) %>" maxlength="20" /></div>
            <div class="form-group"><label>Name *</label>
                <input type="text" name="provinceName" class="form-control" value="<%= Server.HtmlEncode(Input.ProvinceName) %>" maxlength="150" /></div>
            <div class="form-group"><label>Alias</label>
                <input type="text" name="aliasName" class="form-control" value="<%= Server.HtmlEncode(Input.AliasName) %>" maxlength="100" /></div>
            <div class="form-group"><label class="checkbox-label">
                <input type="checkbox" name="isActive" value="true" <%= Input.IsActive ? "checked" : "" %> /> Active</label></div>
        </div>
    </div>
    <div class="card-footer">
        <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';"><%= Input.ProvinceID > 0 ? "Update" : "Save" %></button>
        <a href="/ProvinceSetup.aspx" class="btn btn-secondary">Clear</a>
    </div>
</div>
<div class="card mt-4">
    <div class="card-header"><h2>Province List</h2></div>
    <div class="card-body table-responsive">
        <table class="data-table">
            <thead><tr><th>Code</th><th>Name</th><th>Alias</th><th>Status</th><th>Actions</th></tr></thead>
            <tbody>
            <% if (Records.Count == 0) { %><tr><td colspan="5">No records found.</td></tr>
            <% } else { foreach (var item in Records) { %>
                <tr>
                    <td><%= Server.HtmlEncode(item.ProvinceCode) %></td>
                    <td><%= Server.HtmlEncode(item.ProvinceName) %></td>
                    <td><%= Server.HtmlEncode(item.AliasName) %></td>
                    <td><%= item.IsActive ? "Active" : "Inactive" %></td>
                    <td>
                        <a class="btn-icon btn-edit" href="/ProvinceSetup.aspx?editId=<%= item.ProvinceID %>">Edit</a>
                        <button type="submit" class="btn-icon btn-delete" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= item.ProvinceID %>';return confirm('Remove?');">X</button>
                    </td>
                </tr>
            <% } } %>
            </tbody>
        </table>
    </div>
</div>
</asp:Content>
