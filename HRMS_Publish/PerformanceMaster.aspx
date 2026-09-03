<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PerformanceMaster.aspx.cs" Inherits="HRMS.PerformanceMasterPage" %>

<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
.form-breadcrumb {
    display: flex;
    align-items: center;
    gap: .75rem;
    margin-bottom: 1rem;
    padding: .5rem .75rem;
    background: #f9fafb;
    border: 1px solid #e5e7eb;
    border-radius: 6px;
}
</style>
</asp:Content>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header space-between">
    <h2>Employee Performance</h2>
    <a href="/PerformanceMaster.aspx?newRecord=1" class="btn btn-primary">+ New Performance Record</a>
  </div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead class="grid-header">
        <tr>
          <th>Employee Code</th>
          <th>Employee Name</th>
          <th>Review Cycle</th>
          <th>Last Review</th>
          <th>Rating</th>
          <th>Score</th>
          <th>Next Review Due</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
      <% if (Records.Count == 0) { %>
        <tr class="empty-row"><td colspan="8">No performance records found.</td></tr>
      <% } else {
           foreach (var r in Records) { %>
        <tr>
          <td><%= Server.HtmlEncode(r.EmployeeCode) %></td>
          <td><%= Server.HtmlEncode(r.EmployeeName) %></td>
          <td><%= Server.HtmlEncode(r.PerformanceReviewCycle) %></td>
          <td><%= r.LastReviewDate.HasValue ? r.LastReviewDate.Value.ToString("dd-MMM-yyyy") : "—" %></td>
          <td><%= Server.HtmlEncode(r.LastReviewRating) %></td>
          <td><%= r.LastReviewScore.HasValue ? r.LastReviewScore.Value.ToString("0.##") : "—" %></td>
          <td><%= r.NextReviewDue.HasValue ? r.NextReviewDue.Value.ToString("dd-MMM-yyyy") : "—" %></td>
          <td class="actions-col">
            <a class="btn-icon btn-edit" href="/PerformanceMaster.aspx?editId=<%= r.PerformanceID %>">Edit</a>
            <button type="submit" class="btn-icon btn-delete" title="Delete"
                    onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= r.PerformanceID %>';return confirm('Delete this performance record?');">X</button>
          </td>
        </tr>
      <% } } %>
      </tbody>
    </table>
  </div>
</div>
<% } else { %>
<div class="form-breadcrumb">
  <a href="/PerformanceMaster.aspx" class="btn btn-secondary">&#8592; Back</a>
  <span><%= EditMode ? "Edit Performance Record" : "New Performance Record" %></span>
</div>
<input type="hidden" name="PerformanceID" value="<%= Input.PerformanceID %>" />
<input type="hidden" name="EditMode" value="<%= EditMode ? "true" : "false" %>" />

<div class="card">
  <div class="card-header">
    <h2><%= EditMode ? "Edit Performance Record" : "Add Performance Record" %></h2>
  </div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group">
        <label>Employee <span class="required">*</span></label>
        <select name="EmployeeID" class="form-control">
          <option value="0">-- Select Employee --</option>
          <% foreach (var e in Employees) { %>
          <option value="<%= e.Id %>" <%= Input.EmployeeID == e.Id ? "selected" : "" %>><%= Server.HtmlEncode(e.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Performance Review Cycle</label>
        <select name="PerformanceReviewCycle" class="form-control">
          <option value="">-- Select Cycle --</option>
          <% foreach (var c in ReviewCycleOptions) { %>
          <option value="<%= Server.HtmlEncode(c) %>" <%= Input.PerformanceReviewCycle == c ? "selected" : "" %>><%= Server.HtmlEncode(c) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Last Review Date</label>
        <input type="date" name="LastReviewDate" class="form-control" value="<%= Server.HtmlEncode(Input.LastReviewDate) %>" />
      </div>
      <div class="form-group">
        <label>Last Review Rating</label>
        <select name="LastReviewRating" class="form-control">
          <option value="">-- Select Rating --</option>
          <% foreach (var r in RatingOptions) { %>
          <option value="<%= Server.HtmlEncode(r) %>" <%= Input.LastReviewRating == r ? "selected" : "" %>><%= Server.HtmlEncode(r) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Last Review Score</label>
        <input type="text" name="LastReviewScore" class="form-control" value="<%= Server.HtmlEncode(Input.LastReviewScore) %>" maxlength="10" placeholder="e.g. 85.5" />
      </div>
      <div class="form-group">
        <label>Next Review Due</label>
        <input type="date" name="NextReviewDue" class="form-control" value="<%= Server.HtmlEncode(Input.NextReviewDue) %>" />
      </div>
      <div class="form-group">
        <label>Goal Achievement %</label>
        <input type="text" name="GoalAchievementPercent" class="form-control" value="<%= Server.HtmlEncode(Input.GoalAchievementPercent) %>" maxlength="10" placeholder="e.g. 90" />
      </div>
      <div class="form-group">
        <label>Career Path</label>
        <input type="text" name="CareerPath" class="form-control" value="<%= Server.HtmlEncode(Input.CareerPath) %>" maxlength="100" />
      </div>
      <div class="form-group">
        <label class="checkbox-label">
          <input type="checkbox" name="KPIsAssigned" value="true" <%= Input.KPIsAssigned ? "checked" : "" %> /> KPIs Assigned
        </label>
      </div>
      <div class="form-group">
        <label class="checkbox-label">
          <input type="checkbox" name="PerformanceImprovementPlan" value="true" <%= Input.PerformanceImprovementPlan ? "checked" : "" %> /> Performance Improvement Plan
        </label>
      </div>
      <div class="form-group">
        <label class="checkbox-label">
          <input type="checkbox" name="PromotionReady" value="true" <%= Input.PromotionReady ? "checked" : "" %> /> Promotion Ready
        </label>
      </div>
      <div class="form-group">
        <label class="checkbox-label">
          <input type="checkbox" name="SuccessionPool" value="true" <%= Input.SuccessionPool ? "checked" : "" %> /> Succession Pool
        </label>
      </div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">
      <%= EditMode ? "Update" : "Save" %>
    </button>
    <a href="/PerformanceMaster.aspx" class="btn btn-secondary">Cancel</a>
  </div>
</div>
<% } %>
</asp:Content>
