<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PositionMaster.aspx.cs" Inherits="HRMS.PositionMasterPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header space-between"><h2>Positions</h2>
    <a href="/PositionMaster.aspx?newPosition=1" class="btn btn-primary">+ New Position</a></div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>No</th><th>Description</th><th>Job</th><th>Dept</th><th>Title</th><th>Type</th><th>Duration</th><th>Status</th><th></th></tr></thead>
      <tbody>
      <% foreach (var p in Positions) { %>
      <tr>
        <td><%= Server.HtmlEncode(p.PositionNo) %></td>
        <td><%= Server.HtmlEncode(p.Description) %></td>
        <td><%= Server.HtmlEncode(p.JobTitle) %></td>
        <td><%= Server.HtmlEncode(p.DepartmentName) %></td>
        <td><%= Server.HtmlEncode(p.TitleName) %></td>
        <td><%= Server.HtmlEncode(p.PositionTypeName) %></td>
        <td><%= Server.HtmlEncode(p.PositionDuration) %></td>
        <td><%= p.IsActive ? "Active" : "Inactive" %></td>
        <td>
          <a href="/PositionMaster.aspx?editId=<%= p.PositionID %>">Edit</a>
          <% if (p.IsActive) { %>
          <button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= p.PositionID %>';return confirm('Deactivate?');">X</button>
          <% } %>
        </td>
      </tr>
      <% } %>
      <% if (Positions.Count == 0) { %><tr><td colspan="9">No positions found.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
<% } else { %>
<div class="form-breadcrumb" style="margin-bottom:1rem;">
  <a href="/PositionMaster.aspx" class="btn btn-secondary">&#8592; Back</a>
  <span><%= EditMode ? "Edit Position" : "New Position" %> <%= Server.HtmlEncode(Input.PositionNo) %></span>
</div>
<input type="hidden" name="positionID" value="<%= Input.PositionID %>" />
<div class="card">
  <div class="card-header"><h2><%= EditMode ? "Edit Position" : "Add Position" %></h2></div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group"><label>Position No *</label>
        <input type="text" name="positionNo" class="form-control" value="<%= Server.HtmlEncode(Input.PositionNo) %>" /></div>
      <div class="form-group"><label>Description</label>
        <input type="text" name="description" class="form-control" value="<%= Server.HtmlEncode(Input.Description) %>" /></div>
      <div class="form-group"><label>Email (Employee)</label>
        <select name="emailEmployeeID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var e in EmailLookups) { %><option value="<%= e.EmployeeID %>" <%= Input.EmailEmployeeID==e.EmployeeID?"selected":"" %>><%= Server.HtmlEncode(e.Label) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Job</label>
        <select name="jobID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var j in Jobs) { %><option value="<%= j.Id %>" <%= Input.JobID==j.Id?"selected":"" %>><%= Server.HtmlEncode(j.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Department</label>
        <select name="departmentID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var d in Departments) { %><option value="<%= d.Id %>" <%= Input.DepartmentID==d.Id?"selected":"" %>><%= Server.HtmlEncode(d.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Reports To</label>
        <select name="reportsToPositionID" class="form-control"><option value="0">-- None --</option>
        <% foreach (var r in ReportToPositions) { %><option value="<%= r.Id %>" <%= Input.ReportsToPositionID==r.Id?"selected":"" %>><%= Server.HtmlEncode(r.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Title</label>
        <select name="titleID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var t in Titles) { %><option value="<%= t.Id %>" <%= Input.TitleID==t.Id?"selected":"" %>><%= Server.HtmlEncode(t.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Position Type</label>
        <select name="positionTypeID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var t in PositionTypes) { %><option value="<%= t.Id %>" <%= Input.PositionTypeID==t.Id?"selected":"" %>><%= Server.HtmlEncode(t.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Duration</label>
        <select name="positionDuration" class="form-control"><option value="">--</option>
        <% foreach (var d in PositionDurations) { %><option value="<%= d %>" <%= Input.PositionDuration==d?"selected":"" %>><%= d %></option><% } %>
        </select></div>
      <div class="form-group"><label>Start Date</label>
        <input type="date" name="positionStartDate" class="form-control" value="<%= Server.HtmlEncode(Input.PositionStartDate) %>" /></div>
      <div class="form-group"><label>End Date</label>
        <input type="date" name="positionEndDate" class="form-control" value="<%= Server.HtmlEncode(Input.PositionEndDate) %>" /></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="isActive" value="true" <%= Input.IsActive?"checked":"" %> /> Active</label></div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">Save Position</button>
    <a href="/PositionMaster.aspx" class="btn btn-secondary">Cancel</a>
  </div>
</div>
<% } %>
</asp:Content>
