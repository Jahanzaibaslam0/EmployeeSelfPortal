<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="TrainingMaster.aspx.cs" Inherits="HRMS.TrainingMasterPage" %>

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
    <h2>Training Master</h2>
    <a href="<%= ResolveUrl("~/TrainingMaster.aspx") %>?newRecord=1" class="btn btn-primary">+ New Training Record</a>
  </div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead class="grid-header">
        <tr>
          <th>Employee Code</th>
          <th>Employee Name</th>
          <th>Training Name</th>
          <th>Code</th>
          <th>Status</th>
          <th>Department</th>
          <th>Last Training</th>
          <th>Next Due</th>
          <th>Hours YTD</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
      <% if (Records.Count == 0) { %>
        <tr class="empty-row"><td colspan="10">No training records found.</td></tr>
      <% } else {
           foreach (var r in Records) { %>
        <tr>
          <td><%= Server.HtmlEncode(r.EmployeeCode) %></td>
          <td><%= Server.HtmlEncode(r.EmployeeName) %></td>
          <td><%= Server.HtmlEncode(r.TrainingName) %></td>
          <td><%= Server.HtmlEncode(r.TrainingCode) %></td>
          <td><%= Server.HtmlEncode(r.MandatoryTrainingStatus) %></td>
          <td><%= Server.HtmlEncode(r.TrainingDepartment) %></td>
          <td><%= r.LastTrainingDate.HasValue ? r.LastTrainingDate.Value.ToString("dd-MMM-yyyy") : "—" %></td>
          <td><%= r.NextTrainingDue.HasValue ? r.NextTrainingDue.Value.ToString("dd-MMM-yyyy") : "—" %></td>
          <td><%= r.TrainingHoursYTD.HasValue ? r.TrainingHoursYTD.Value.ToString("0.##") : "—" %></td>
          <td class="actions-col">
            <a class="btn-icon btn-edit" href="<%= ResolveUrl("~/TrainingMaster.aspx") %>?editId=<%= r.EmployeeTrainingID %>">Edit</a>
            <button type="submit" class="btn-icon btn-delete" title="Delete"
                    onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= r.EmployeeTrainingID %>';return confirm('Delete this training record?');">X</button>
          </td>
        </tr>
      <% } } %>
      </tbody>
    </table>
  </div>
</div>
<% } else { %>
<div class="form-breadcrumb">
  <a href="<%= ResolveUrl("~/TrainingMaster.aspx") %>" class="btn btn-secondary">&#8592; Back</a>
  <span><%= EditMode ? "Edit Training Record" : "New Training Record" %></span>
</div>
<input type="hidden" name="EmployeeTrainingID" value="<%= Input.EmployeeTrainingID %>" />
<input type="hidden" name="EditMode" value="<%= EditMode ? "true" : "false" %>" />

<div class="card">
  <div class="card-header">
    <h2><%= EditMode ? "Edit Training Record" : "Add Training Record" %></h2>
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
        <label>Training Name</label>
        <input type="text" name="TrainingName" class="form-control" value="<%= Server.HtmlEncode(Input.TrainingName) %>" maxlength="200" />
      </div>
      <div class="form-group">
        <label>Training Code</label>
        <input type="text" name="TrainingCode" class="form-control" value="<%= Server.HtmlEncode(Input.TrainingCode) %>" maxlength="50" />
      </div>
      <div class="form-group">
        <label>Mandatory Training Status</label>
        <select name="MandatoryTrainingStatus" class="form-control">
          <option value="">-- Select Status --</option>
          <% foreach (var s in MandatoryStatuses) { %>
          <option value="<%= Server.HtmlEncode(s) %>" <%= Input.MandatoryTrainingStatus == s ? "selected" : "" %>><%= Server.HtmlEncode(s) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Training Department</label>
        <select name="TrainingDepartment" class="form-control">
          <% foreach (var d in Departments) { %>
          <option value="<%= Server.HtmlEncode(d.Name) %>" <%= Input.TrainingDepartment == d.Name ? "selected" : "" %>><%= Server.HtmlEncode(d.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Last Training Date</label>
        <input type="date" name="LastTrainingDate" class="form-control" value="<%= Server.HtmlEncode(Input.LastTrainingDate) %>" />
      </div>
      <div class="form-group">
        <label>Next Training Due</label>
        <input type="date" name="NextTrainingDue" class="form-control" value="<%= Server.HtmlEncode(Input.NextTrainingDue) %>" />
      </div>
      <div class="form-group">
        <label>Safety Training Valid Till</label>
        <input type="date" name="SafetyTrainingValidTill" class="form-control" value="<%= Server.HtmlEncode(Input.SafetyTrainingValidTill) %>" />
      </div>
      <div class="form-group">
        <label>GMP Training Valid Till</label>
        <input type="date" name="GMPTrainingValidTill" class="form-control" value="<%= Server.HtmlEncode(Input.GMPTrainingValidTill) %>" />
      </div>
      <div class="form-group">
        <label>Training Hours YTD</label>
        <input type="text" name="TrainingHoursYTD" class="form-control" value="<%= Server.HtmlEncode(Input.TrainingHoursYTD) %>" maxlength="10" placeholder="e.g. 12.5" />
      </div>
      <div class="form-group">
        <label>Training Hours Required (Annual)</label>
        <input type="text" name="TrainingHoursRequiredAnnual" class="form-control" value="<%= Server.HtmlEncode(Input.TrainingHoursRequiredAnnual) %>" maxlength="10" placeholder="e.g. 40" />
      </div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">
      <%= EditMode ? "Update" : "Save" %>
    </button>
    <a href="<%= ResolveUrl("~/TrainingMaster.aspx") %>" class="btn btn-secondary">Cancel</a>
  </div>
</div>
<% } %>
</asp:Content>
