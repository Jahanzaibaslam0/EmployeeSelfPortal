<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="TaskMaster.aspx.cs" Inherits="HRMS.TaskMasterPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<input type="hidden" name="statusId" id="statusId" value="0" />

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header space-between"><h2>Task List</h2>
    <a href="/TaskMaster.aspx?newTask=1" class="btn btn-primary">+ New Task</a></div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>Code</th><th>Title</th><th>Priority</th><th>Status</th><th>Due</th><th>Assigned</th><th>Ref</th><th></th></tr></thead>
      <tbody>
      <% foreach (var t in Tasks) { %>
      <tr>
        <td><%= Server.HtmlEncode(t.TaskCode) %></td>
        <td><%= Server.HtmlEncode(t.Title) %></td>
        <td><%= Server.HtmlEncode(t.Priority) %></td>
        <td><%= Server.HtmlEncode(t.TaskStatus) %></td>
        <td><%= t.DueDate.HasValue ? t.DueDate.Value.ToString("dd MMM yyyy") : "" %></td>
        <td><%= Server.HtmlEncode(t.AssignedToName) %></td>
        <td><%= Server.HtmlEncode(t.ReferenceType) %>: <%= Server.HtmlEncode(t.ReferenceDisplay) %></td>
        <td>
          <a href="/TaskMaster.aspx?editId=<%= t.TaskID %>">Edit</a>
          <button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= t.TaskID %>';return confirm('Remove this task?');">X</button>
        </td>
      </tr>
      <% } %>
      <% if (Tasks.Count == 0) { %><tr><td colspan="8">No tasks found.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
<% } else { %>
<div class="form-breadcrumb" style="margin-bottom:1rem;">
  <a href="/TaskMaster.aspx" class="btn btn-secondary">&#8592; Back</a>
  <span><%= EditMode ? "Edit Task" : "New Task" %> <%= Server.HtmlEncode(Input.TaskCode) %></span>
</div>
<input type="hidden" name="TaskID" value="<%= Input.TaskID %>" />
<input type="hidden" name="EditMode" value="<%= EditMode ? "true" : "false" %>" />
<div class="card">
  <div class="card-header"><h2><%= EditMode ? "Edit Task" : "Add Task" %></h2></div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group"><label>Code</label><input type="text" class="form-control" value="<%= Server.HtmlEncode(Input.TaskCode) %>" readonly /></div>
      <div class="form-group"><label>Title *</label><input type="text" name="Title" class="form-control" value="<%= Server.HtmlEncode(Input.Title) %>" <%= IsStatusLocked?"readonly":"" %> /></div>
      <div class="form-group" style="grid-column:1/-1;"><label>Description</label>
        <textarea name="Description" class="form-control" rows="3" <%= IsStatusLocked?"readonly":"" %>><%= Server.HtmlEncode(Input.Description) %></textarea></div>
      <div class="form-group"><label>Priority</label>
        <select name="Priority" class="form-control" <%= IsStatusLocked?"disabled":"" %>>
        <% foreach (var p in PriorityOptions) { %><option value="<%= p %>" <%= Input.Priority==p?"selected":"" %>><%= p %></option><% } %>
        </select><% if (IsStatusLocked) { %><input type="hidden" name="Priority" value="<%= Server.HtmlEncode(Input.Priority) %>" /><% } %></div>
      <div class="form-group"><label>Status</label>
        <select name="TaskStatus" class="form-control" <%= IsStatusLocked?"disabled":"" %>>
        <% foreach (var s in StatusOptions) { %><option value="<%= s %>" <%= Input.TaskStatus==s?"selected":"" %>><%= s %></option><% } %>
        </select><% if (IsStatusLocked) { %><input type="hidden" name="TaskStatus" value="<%= Server.HtmlEncode(Input.TaskStatus) %>" /><% } %></div>
      <div class="form-group"><label>Due Date</label><input type="date" name="DueDate" class="form-control" value="<%= Server.HtmlEncode(Input.DueDate) %>" <%= IsStatusLocked?"readonly":"" %> /></div>
      <div class="form-group"><label>Assigned To</label>
        <select name="AssignedToEmployeeID" class="form-control" <%= IsStatusLocked?"disabled":"" %>><option value="0">-- None --</option>
        <% foreach (var e in Employees) { %><option value="<%= e.Id %>" <%= Input.AssignedToEmployeeID==e.Id?"selected":"" %>><%= Server.HtmlEncode(e.Name) %></option><% } %>
        </select><% if (IsStatusLocked) { %><input type="hidden" name="AssignedToEmployeeID" value="<%= Input.AssignedToEmployeeID %>" /><% } %></div>
      <div class="form-group"><label>Reference Type</label>
        <select name="ReferenceType" class="form-control" <%= IsStatusLocked?"disabled":"" %>>
        <% foreach (var r in ReferenceTypeOptions) { %><option value="<%= r %>" <%= Input.ReferenceType==r?"selected":"" %>><%= r %></option><% } %>
        </select><% if (IsStatusLocked) { %><input type="hidden" name="ReferenceType" value="<%= Server.HtmlEncode(Input.ReferenceType) %>" /><% } %></div>
      <div class="form-group"><label>Ref Employee</label>
        <select name="ReferenceEmployeeID" class="form-control"><option value="0">--</option>
        <% foreach (var e in Employees) { %><option value="<%= e.Id %>" <%= Input.ReferenceEmployeeID==e.Id?"selected":"" %>><%= Server.HtmlEncode(e.Name) %></option><% } %></select></div>
      <div class="form-group"><label>Ref Customer</label>
        <select name="ReferenceCustomerID" class="form-control"><option value="0">--</option>
        <% foreach (var c in Customers) { %><option value="<%= c.Id %>" <%= Input.ReferenceCustomerID==c.Id?"selected":"" %>><%= Server.HtmlEncode(c.Name) %></option><% } %></select></div>
      <div class="form-group"><label>Ref Vendor</label>
        <select name="ReferenceVendorID" class="form-control"><option value="0">--</option>
        <% foreach (var v in Vendors) { %><option value="<%= v.Id %>" <%= Input.ReferenceVendorID==v.Id?"selected":"" %>><%= Server.HtmlEncode(v.Name) %></option><% } %></select></div>
      <div class="form-group"><label>Ref Sales Order</label>
        <select name="ReferenceSalesOrderID" class="form-control"><option value="0">--</option>
        <% foreach (var s in SalesOrders) { %><option value="<%= s.Id %>" <%= Input.ReferenceSalesOrderID==s.Id?"selected":"" %>><%= Server.HtmlEncode(s.Name) %></option><% } %></select></div>
      <div class="form-group"><label>Ref Purchase Order</label>
        <select name="ReferencePurchaseOrderID" class="form-control"><option value="0">--</option>
        <% foreach (var p in PurchaseOrders) { %><option value="<%= p.Id %>" <%= Input.ReferencePurchaseOrderID==p.Id?"selected":"" %>><%= Server.HtmlEncode(p.Name) %></option><% } %></select></div>
    </div>
  </div>
  <div class="card-footer">
    <% if (!IsStatusLocked) { %>
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">Save</button>
    <% } %>
    <% if (EditMode && Input.TaskStatus != "Completed" && Input.TaskStatus != "Cancelled") { %>
    <button type="submit" class="btn btn-secondary" onclick="document.getElementById('__handler').value='Complete';document.getElementById('statusId').value='<%= Input.TaskID %>';">Complete</button>
    <button type="submit" class="btn btn-secondary" onclick="document.getElementById('__handler').value='Cancel';document.getElementById('statusId').value='<%= Input.TaskID %>';">Cancel Task</button>
    <% } %>
    <% if (EditMode && IsStatusLocked) { %>
    <button type="submit" class="btn btn-secondary" onclick="document.getElementById('__handler').value='Reopen';document.getElementById('statusId').value='<%= Input.TaskID %>';">Reopen</button>
    <% } %>
    <a href="/TaskMaster.aspx" class="btn btn-secondary">Back</a>
  </div>
</div>
<% if (EditMode && History.Count > 0) { %>
<div class="card mt-4"><div class="card-header"><h2>History</h2></div>
<div class="card-body table-responsive"><table class="data-table">
<thead><tr><th>When</th><th>Action</th><th>From</th><th>To</th><th>By</th><th>Remarks</th></tr></thead>
<tbody>
<% foreach (var h in History) { %>
<tr>
  <td><%= h.CreatedOn.ToString("dd MMM yyyy HH:mm") %></td>
  <td><%= Server.HtmlEncode(h.ActionType) %></td>
  <td><%= Server.HtmlEncode(h.OldStatus) %></td>
  <td><%= Server.HtmlEncode(h.NewStatus) %></td>
  <td><%= Server.HtmlEncode(h.CreatedByName) %></td>
  <td><%= Server.HtmlEncode(h.Remarks) %></td>
</tr>
<% } %>
</tbody></table></div></div>
<% } %>
<% } %>
</asp:Content>
