<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.SoftwareLinkSetupPage" %>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<input type="hidden" name="softwareLinkID" value="<%= Input.SoftwareLinkID %>" />

<div class="card">
  <div class="card-header">
    <h2><%= Input.SoftwareLinkID > 0 ? "Edit Software Link" : "Add Software Link" %></h2>
  </div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group">
        <label>Software Name <span class="required">*</span></label>
        <input type="text" name="softwareName" class="form-control"
               value="<%= Server.HtmlEncode(Input.SoftwareName) %>" maxlength="100" placeholder="e.g. Microsoft Power BI" />
      </div>
      <div class="form-group">
        <label>Software URL <span class="required">*</span></label>
        <input type="url" name="softwareUrl" class="form-control"
               value="<%= Server.HtmlEncode(Input.SoftwareUrl) %>" maxlength="500" placeholder="https://..." />
      </div>
      <div class="form-group">
        <label>Category</label>
        <input type="text" name="category" class="form-control"
               value="<%= Server.HtmlEncode(Input.Category) %>" maxlength="50" placeholder="e.g. Analytics, ERP, Communication" />
      </div>
      <div class="form-group">
        <label>Sort Order</label>
        <input type="number" name="sortOrder" class="form-control"
               value="<%= Input.SortOrder <= 0 ? 1 : Input.SortOrder %>" min="1" />
      </div>
      <div class="form-group">
        <label>Status</label>
        <label class="checkbox-label">
          <input type="checkbox" name="isActive" value="true" <%= Input.IsActive ? "checked" : "" %> />
          Active
        </label>
      </div>
      <div class="form-group full-width">
        <label>Description</label>
        <textarea name="description" class="form-control" maxlength="250" rows="2"
                  placeholder="Brief description shown on Quick Links page"><%= Server.HtmlEncode(Input.Description) %></textarea>
      </div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary"><%= Input.SoftwareLinkID > 0 ? "Update Software Link" : "Save Software Link" %></button>
    <a href="<%= ResolveUrl("~/SoftwareLinkSetup.aspx") %>" class="btn btn-secondary">Clear</a>
    <a href="<%= ResolveUrl("~/QuickLinks.aspx") %>" class="btn btn-secondary">View Quick Links</a>
  </div>
</div>

<div class="card mt-4">
  <div class="card-header space-between">
    <h2>Software Link List</h2>
    <input type="text" id="txtSearch" class="form-control" style="width:220px" placeholder="Search&hellip;" onkeyup="searchTable(this.value)" />
  </div>
  <div class="card-body table-responsive">
    <table class="data-table" id="dataTable">
      <thead class="grid-header">
        <tr>
          <th>Order</th>
          <th>Software</th>
          <th>URL</th>
          <th>Category</th>
          <th>Description</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <% if (Records.Count == 0) { %>
        <tr class="empty-row"><td colspan="7">No records found.</td></tr>
        <% } else { foreach (var item in Records) { %>
        <tr>
          <td><%= item.SortOrder %></td>
          <td><%= Server.HtmlEncode(item.SoftwareName) %></td>
          <td>
            <% if (!string.IsNullOrWhiteSpace(item.SoftwareUrl)) { %>
            <a href="<%= Server.HtmlEncode(item.SoftwareUrl) %>" target="_blank" rel="noopener noreferrer"><%= Server.HtmlEncode(item.SoftwareUrl) %></a>
            <% } %>
          </td>
          <td><%= Server.HtmlEncode(item.Category) %></td>
          <td><%= Server.HtmlEncode(item.Description) %></td>
          <td><span class="badge <%= item.IsActive ? "badge-success" : "badge-danger" %>"><%= item.IsActive ? "Active" : "Inactive" %></span></td>
          <td class="actions-col">
            <a class="btn-icon btn-edit" href="<%= ResolveUrl("~/SoftwareLinkSetup.aspx") %>?editId=<%= item.SoftwareLinkID %>">Edit</a>
            <button type="submit" class="btn-icon btn-delete"
                    onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= item.SoftwareLinkID %>';return confirm('Deactivate this software link?');">X</button>
          </td>
        </tr>
        <% } } %>
      </tbody>
    </table>
  </div>
</div>
</asp:Content>
