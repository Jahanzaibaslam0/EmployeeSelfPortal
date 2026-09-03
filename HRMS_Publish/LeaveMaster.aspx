<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="LeaveMaster.aspx.cs" Inherits="HRMS.LeaveMasterPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header space-between"><h2>Leave Applications</h2>
    <a href="/LeaveMaster.aspx?newLeave=1" class="btn btn-primary">+ New Leave</a></div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>Applied</th><th>Employee</th><th>Type</th><th>Category</th><th>From</th><th>To</th><th>Days</th><th></th></tr></thead>
      <tbody>
      <% foreach (var l in Leaves) { %>
      <tr>
        <td><%= l.ApplyingDate.ToString("dd MMM yyyy") %></td>
        <td><%= Server.HtmlEncode(l.EmployeeCode) %> – <%= Server.HtmlEncode(l.EmployeeName) %></td>
        <td><%= Server.HtmlEncode(l.LeaveType) %></td>
        <td><%= Server.HtmlEncode(l.LeaveCategoryName) %></td>
        <td><%= l.LeaveFromDate.ToString("dd MMM yyyy") %></td>
        <td><%= l.LeaveToDate.ToString("dd MMM yyyy") %></td>
        <td><%= l.NumberOfDays %></td>
        <td>
          <a href="/LeaveMaster.aspx?editId=<%= l.LeaveID %>">Edit</a>
          <button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= l.LeaveID %>';return confirm('Delete this leave?');">X</button>
        </td>
      </tr>
      <% } %>
      <% if (Leaves.Count == 0) { %><tr><td colspan="8">No leave applications found.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
<% } else { %>
<div class="form-breadcrumb" style="margin-bottom:1rem;">
  <a href="/LeaveMaster.aspx" class="btn btn-secondary">&#8592; Back</a>
  <span><%= EditMode ? "Edit Leave" : "New Leave" %></span>
</div>
<input type="hidden" name="LeaveID" value="<%= Input.LeaveID %>" />
<input type="hidden" name="EditMode" value="<%= EditMode ? "true" : "false" %>" />
<div class="card">
  <div class="card-header"><h2><%= EditMode ? "Edit Leave Application" : "New Leave Application" %></h2></div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group"><label>Applying Date</label>
        <input type="date" name="ApplyingDate" class="form-control" value="<%= Server.HtmlEncode(Input.ApplyingDate) %>" /></div>
      <div class="form-group"><label>Employee *</label>
        <select name="EmployeeID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var e in Employees) { %><option value="<%= e.Id %>" <%= Input.EmployeeID==e.Id?"selected":"" %>><%= Server.HtmlEncode(e.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Leave Type</label>
        <select name="LeaveType" class="form-control">
        <% foreach (var t in LeaveTypes) { %><option value="<%= t %>" <%= Input.LeaveType==t?"selected":"" %>><%= t %></option><% } %>
        </select></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="IsFutureUnplannedLeave" value="true" <%= Input.IsFutureUnplannedLeave?"checked":"" %> /> Future Unplanned</label></div>
      <div class="form-group"><label>Leave Category *</label>
        <select name="LeaveCategoryID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var c in LeaveCategories) { %><option value="<%= c.Id %>" <%= Input.LeaveCategoryID==c.Id?"selected":"" %>><%= Server.HtmlEncode(c.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>From Date *</label>
        <input type="date" name="LeaveFromDate" class="form-control" value="<%= Server.HtmlEncode(Input.LeaveFromDate) %>" /></div>
      <div class="form-group"><label>To Date *</label>
        <input type="date" name="LeaveToDate" class="form-control" value="<%= Server.HtmlEncode(Input.LeaveToDate) %>" /></div>
      <div class="form-group"><label>Days</label>
        <input type="text" class="form-control" value="<%= Input.NumberOfDays %>" readonly /></div>
      <div class="form-group" style="grid-column:1/-1;"><label>Reason</label>
        <textarea name="ReasonForLeave" class="form-control" rows="2"><%= Server.HtmlEncode(Input.ReasonForLeave) %></textarea></div>
      <div class="form-group"><label>Temp Responsible</label>
        <select name="TempResponsibleEmployeeID" class="form-control"><option value="0">-- None --</option>
        <% foreach (var e in Employees) { %><option value="<%= e.Id %>" <%= Input.TempResponsibleEmployeeID==e.Id?"selected":"" %>><%= Server.HtmlEncode(e.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Permanent Responsible</label>
        <select name="PermanentResponsibleEmployeeID" class="form-control"><option value="0">-- None --</option>
        <% foreach (var e in Employees) { %><option value="<%= e.Id %>" <%= Input.PermanentResponsibleEmployeeID==e.Id?"selected":"" %>><%= Server.HtmlEncode(e.Name) %></option><% } %>
        </select></div>
    </div>
    <% if (!string.IsNullOrEmpty(EmployeeInfo.EmployeeName)) { %>
    <div class="mt-4" style="padding:1rem;background:#f9fafb;border-radius:6px;">
      <strong><%= Server.HtmlEncode(EmployeeInfo.EmployeeName) %></strong>
      — <%= Server.HtmlEncode(EmployeeInfo.Department) %> / <%= Server.HtmlEncode(EmployeeInfo.Designation) %>
      — <%= Server.HtmlEncode(EmployeeInfo.Location) %>
    </div>
    <% } %>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">Save</button>
    <a href="/LeaveMaster.aspx" class="btn btn-secondary">Cancel</a>
  </div>
</div>
<% } %>
</asp:Content>
