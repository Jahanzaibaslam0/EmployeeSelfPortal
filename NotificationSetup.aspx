<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.NotificationSetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<div class="card">
  <div class="card-header">
    <h2><%= Input.NotificationID > 0 ? "Edit Notification" : "Add Notification" %></h2>
  </div>
  <div class="card-body">
    <input type="hidden" name="notificationID" value="<%= Input.NotificationID %>" />
    <div class="form-grid">
      <div class="form-group">
        <label>ID</label>
        <input type="text" class="form-control"
               value="<%= Input.NotificationID > 0 ? Input.NotificationID.ToString() : "Auto" %>" readonly />
      </div>
      <div class="form-group">
        <label>Name <span class="required">*</span></label>
        <input type="text" name="notificationName" class="form-control"
               value="<%= Server.HtmlEncode(Input.Name) %>" maxlength="150" />
      </div>
      <div class="form-group">
        <label>Department</label>
        <select name="departmentID" class="form-control">
          <option value="0">All Departments</option>
          <% foreach (var dept in Departments) { %>
          <option value="<%= dept.Id %>" <%= Input.DepartmentID == dept.Id ? "selected" : "" %>><%= Server.HtmlEncode(dept.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Start Date <span class="required">*</span></label>
        <input type="date" name="startDate" class="form-control" value="<%= Input.StartDate.ToString("yyyy-MM-dd") %>" />
      </div>
      <div class="form-group">
        <label>Valid Till Date <span class="required">*</span></label>
        <input type="date" name="validTillDate" class="form-control" value="<%= Input.ValidTillDate.ToString("yyyy-MM-dd") %>" />
      </div>
      <div class="form-group">
        <label>Activate Status</label>
        <label class="checkbox-label">
          <input type="checkbox" name="isActive" value="true" <%= Input.IsActive ? "checked" : "" %> /> Active
        </label>
      </div>
      <div class="form-group full-width">
        <label>Description</label>
        <textarea name="description" class="form-control" rows="4" maxlength="2000"><%= Server.HtmlEncode(Input.Description) %></textarea>
      </div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">
      <%= Input.NotificationID > 0 ? "Update Notification" : "Save Notification" %>
    </button>
    <a href="/NotificationSetup.aspx" class="btn btn-secondary">Clear</a>
    <a href="/Notifications.aspx" class="btn btn-secondary">View Notifications</a>
  </div>
</div>

<div class="card mt-4">
  <div class="card-header"><h2>Notification List</h2></div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead class="grid-header">
        <tr>
          <th>ID</th>
          <th>Name</th>
          <th>Department</th>
          <th>Start</th>
          <th>Valid Till</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
      <% if (Records.Count == 0) { %>
        <tr class="empty-row"><td colspan="7">No notifications found.</td></tr>
      <% } else {
           foreach (var item in Records) { %>
        <tr>
          <td><%= item.NotificationID %></td>
          <td><%= Server.HtmlEncode(item.Name) %></td>
          <td><%= Server.HtmlEncode(item.DepartmentID > 0 ? item.DepartmentName : "All Departments") %></td>
          <td><%= item.StartDate.ToString("dd-MMM-yyyy") %></td>
          <td><%= item.ValidTillDate.ToString("dd-MMM-yyyy") %></td>
          <td>
            <span class="badge <%= item.IsActive ? "badge-success" : "badge-danger" %>">
              <%= item.IsActive ? "Active" : "Inactive" %>
            </span>
          </td>
          <td class="actions-col">
            <a class="btn-icon btn-edit" href="/NotificationSetup.aspx?editId=<%= item.NotificationID %>" title="Edit">Edit</a>
            <button type="submit" class="btn-icon btn-delete" title="Remove"
                    onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= item.NotificationID %>';">X</button>
          </td>
        </tr>
      <% } } %>
      </tbody>
    </table>
  </div>
</div>
</asp:Content>
