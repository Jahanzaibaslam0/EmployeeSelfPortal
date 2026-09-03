<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ExpenseMaster.aspx.cs" Inherits="HRMS.ExpenseMasterPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<% if (!ShowForm) { %>
<div class="card">
  <div class="card-header space-between">
    <h2>Expense List</h2>
    <a href="/ExpenseMaster.aspx?newExpense=1" class="btn btn-primary">+ New Expense</a>
  </div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>Employee</th><th>Date</th><th>Location</th><th>Purpose</th><th>Lines</th><th>Total</th><th>Workflow</th><th>Doc</th><th></th></tr></thead>
      <tbody>
      <% foreach (var x in Expenses) { %>
      <tr>
        <td><%= Server.HtmlEncode(x.EmployeeCode) %> – <%= Server.HtmlEncode(x.EmployeeName) %></td>
        <td><%= x.ExpenseDate.HasValue ? x.ExpenseDate.Value.ToString("dd MMM yyyy") : "" %></td>
        <td><%= Server.HtmlEncode(x.LocationName) %></td>
        <td><%= Server.HtmlEncode(x.ExpensePurpose) %></td>
        <td><%= x.LineCount %></td>
        <td><%= x.TotalAmount.ToString("N2") %></td>
        <td><%= Server.HtmlEncode(x.WorkflowStatus) %></td>
        <td><%= Server.HtmlEncode(x.DocumentStatus) %></td>
        <td>
          <a href="/ExpenseMaster.aspx?editId=<%= x.ExpenseID %>">Edit</a>
          <button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= x.ExpenseID %>';return confirm('Delete this expense?');">X</button>
        </td>
      </tr>
      <% } %>
      <% if (Expenses.Count == 0) { %><tr><td colspan="9">No expenses found.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
<% } else { %>
<div class="form-breadcrumb" style="margin-bottom:1rem;">
  <a href="/ExpenseMaster.aspx" class="btn btn-secondary">&#8592; Back to List</a>
  <span><%= EditMode ? "Edit Expense" : "New Expense" %></span>
</div>
<input type="hidden" name="ExpenseID" value="<%= Input.ExpenseID %>" />
<input type="hidden" name="EditMode" value="<%= EditMode ? "true" : "false" %>" />
<input type="hidden" name="DetailsJson" id="DetailsJson" />
<div class="card">
  <div class="card-header"><h2><%= EditMode ? "Edit Expense" : "Add Expense" %></h2></div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group"><label>Employee *</label>
        <select name="EmployeeID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var e in Employees) { %><option value="<%= e.Id %>" <%= Input.EmployeeID==e.Id?"selected":"" %>><%= Server.HtmlEncode(e.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Expense Date</label>
        <input type="date" name="ExpenseDate" class="form-control" value="<%= Server.HtmlEncode(Input.ExpenseDate) %>" /></div>
      <div class="form-group"><label>Location</label>
        <select name="LocationID" class="form-control"><option value="0">-- Select --</option>
        <% foreach (var l in Locations) { %><option value="<%= l.Id %>" <%= Input.LocationID==l.Id?"selected":"" %>><%= Server.HtmlEncode(l.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Workflow Status</label>
        <select name="WorkflowStatus" class="form-control">
        <% foreach (var s in new[]{"Draft","Submitted","Approved","Rejected"}) { %><option value="<%= s %>" <%= Input.WorkflowStatus==s?"selected":"" %>><%= s %></option><% } %>
        </select></div>
      <div class="form-group"><label>Document Status</label>
        <select name="DocumentStatus" class="form-control">
        <% foreach (var s in new[]{"Pending","Complete","Missing"}) { %><option value="<%= s %>" <%= Input.DocumentStatus==s?"selected":"" %>><%= s %></option><% } %>
        </select></div>
      <div class="form-group" style="grid-column:1/-1;"><label>Expense Purpose</label>
        <textarea name="ExpensePurpose" class="form-control" rows="2"><%= Server.HtmlEncode(Input.ExpensePurpose) %></textarea></div>
      <div class="form-group"><label>Vehicle No</label>
        <input type="text" name="VehicleNo" class="form-control" value="<%= Server.HtmlEncode(Input.VehicleNo) %>" /></div>
      <div class="form-group"><label>Meter Reading</label>
        <input type="text" name="MeterReading" class="form-control" value="<%= Server.HtmlEncode(Input.MeterReading) %>" /></div>
    </div>
    <div class="card mt-4">
      <div class="card-header space-between"><h2>Expense Line Items</h2>
        <button type="button" class="btn btn-secondary" onclick="addExpenseDetailRow()">+ Add Line</button></div>
      <div class="card-body table-responsive">
        <table class="data-table" id="expenseDetailTable">
          <thead><tr>
            <th>Category</th><th>Description</th><th>Payment</th><th>Txn Date</th><th>Currency</th>
            <th>Txn Amt</th><th>Amount</th><th>Approval</th><th>Receipt ID</th><th>Doc</th><th></th>
          </tr></thead>
          <tbody></tbody>
        </table>
      </div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';return prepareExpensePayload();"><%= EditMode ? "Update Expense" : "Save Expense" %></button>
    <a href="/ExpenseMaster.aspx" class="btn btn-secondary">Cancel</a>
  </div>
</div>
<script type="application/json" id="expenseCategoryData"><%= CategoriesJson %></script>
<script type="application/json" id="initialExpenseDetailsData"><%= DetailsJsonInit %></script>
<script src="/js/expense.js?v=1"></script>
<% } %>
</asp:Content>
