<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.MemorandumSetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<div class="card">
  <div class="card-header">
    <h2><%= Input.MemorandumID > 0 ? "Edit Memorandum" : "Add Memorandum" %></h2>
  </div>
  <div class="card-body">
    <input type="hidden" name="memorandumID" value="<%= Input.MemorandumID %>" />
    <div class="form-grid">
      <div class="form-group">
        <label>ID</label>
        <input type="text" class="form-control"
               value="<%= Input.MemorandumID > 0 ? Input.MemorandumID.ToString() : "Auto" %>" readonly />
      </div>
      <div class="form-group">
        <label>Name <span class="required">*</span></label>
        <input type="text" name="memorandumName" class="form-control"
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
      <div class="form-group full-width">
        <label>Attached Document</label>
        <input type="file" name="documentFile" class="form-control" accept=".pdf,.doc,.docx,.xls,.xlsx,.png,.jpg,.jpeg" />
        <small style="color:var(--text-muted);">PDF, Word, Excel, or image files</small>
        <% if (!string.IsNullOrWhiteSpace(Input.DocumentPath)) { %>
        <div style="margin-top:.5rem;">
          Current: <a href="<%= Server.HtmlEncode(Input.DocumentPath) %>" target="_blank" rel="noopener noreferrer"><%= Server.HtmlEncode(Input.OriginalFileName) %></a>
        </div>
        <% } %>
      </div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">
      <%= Input.MemorandumID > 0 ? "Update Memorandum" : "Save Memorandum" %>
    </button>
    <a href="/MemorandumSetup.aspx" class="btn btn-secondary">Clear</a>
    <a href="/Memorandums.aspx" class="btn btn-secondary">View Memorandums</a>
  </div>
</div>

<div class="card mt-4">
  <div class="card-header"><h2>Memorandum List</h2></div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead class="grid-header">
        <tr>
          <th>ID</th>
          <th>Name</th>
          <th>Department</th>
          <th>Start</th>
          <th>Valid Till</th>
          <th>Document</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
      <% if (Records.Count == 0) { %>
        <tr class="empty-row"><td colspan="8">No memorandums found.</td></tr>
      <% } else {
           foreach (var item in Records) { %>
        <tr>
          <td><%= item.MemorandumID %></td>
          <td><%= Server.HtmlEncode(item.Name) %></td>
          <td><%= Server.HtmlEncode(item.DepartmentID > 0 ? item.DepartmentName : "All Departments") %></td>
          <td><%= item.StartDate.ToString("dd-MMM-yyyy") %></td>
          <td><%= item.ValidTillDate.ToString("dd-MMM-yyyy") %></td>
          <td>
            <% if (!string.IsNullOrWhiteSpace(item.DocumentPath)) { %>
              <a href="<%= Server.HtmlEncode(item.DocumentPath) %>" target="_blank" rel="noopener noreferrer">View</a>
            <% } else { %>
              <span class="audit-meta">—</span>
            <% } %>
          </td>
          <td>
            <span class="badge <%= item.IsActive ? "badge-success" : "badge-danger" %>">
              <%= item.IsActive ? "Active" : "Inactive" %>
            </span>
          </td>
          <td class="actions-col">
            <a class="btn-icon btn-edit" href="/MemorandumSetup.aspx?editId=<%= item.MemorandumID %>" title="Edit">Edit</a>
            <button type="submit" class="btn-icon btn-delete" title="Remove"
                    onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= item.MemorandumID %>';">X</button>
          </td>
        </tr>
      <% } } %>
      </tbody>
    </table>
  </div>
</div>
</asp:Content>
