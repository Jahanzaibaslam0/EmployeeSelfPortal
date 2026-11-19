<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.DesignationSetupPage" %>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %>
<div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div>
<% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<input type="hidden" name="designationID" value="<%= Input.DesignationID %>" />

<div class="card">
  <div class="card-header">
    <h2><%= Input.DesignationID > 0 ? "Edit Designation" : "Add Designation" %></h2>
  </div>
  <div class="card-body">
    <div class="form-grid">
      <div class="form-group">
        <label>Title <span class="required">*</span></label>
        <select name="titleID" class="form-control">
          <option value="">-- Select Title --</option>
          <% foreach (var item in Titles) { %>
          <option value="<%= item.Id %>" <%= Input.TitleID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Grade <span class="required">*</span></label>
        <select name="gradeID" class="form-control">
          <option value="">-- Select Grade --</option>
          <% foreach (var item in Grades) { %>
          <option value="<%= item.Id %>" <%= Input.GradeID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Employee Category <span class="required">*</span></label>
        <select name="employeeCategoryID" class="form-control">
          <option value="">-- Select Employee Category --</option>
          <% foreach (var item in EmployeeCategories) { %>
          <option value="<%= item.Id %>" <%= Input.EmployeeCategoryID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Probation Days</label>
        <input type="text" name="probationDays" id="txtProbationDays" class="form-control inputmode-numeric"
               value="<%= Server.HtmlEncode(Input.ProbationDays) %>" maxlength="5" inputmode="numeric" autocomplete="off"
               placeholder="e.g. 90" />
        <span class="field-error" id="errProbationDays"></span>
      </div>
      <div class="form-group">
        <label>Privilege Code</label>
        <select name="privilegeCode" class="form-control">
          <% foreach (var item in PrivilegeCodeOptions) { %>
          <option value="<%= Server.HtmlEncode(item) %>" <%= Input.PrivilegeCode == item ? "selected" : "" %>><%= Server.HtmlEncode(item) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Privilege Tier</label>
        <select name="privilegeTier" class="form-control">
          <% foreach (var item in PrivilegeTierOptions) { %>
          <option value="<%= Server.HtmlEncode(item) %>" <%= Input.PrivilegeTier == item ? "selected" : "" %>><%= Server.HtmlEncode(item) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Position Tenure</label>
        <select name="positionTenure" class="form-control">
          <option value="">-- Select --</option>
          <% foreach (var item in PositionTenureOptions) { %>
          <option value="<%= Server.HtmlEncode(item) %>" <%= Input.PositionTenure == item ? "selected" : "" %>><%= Server.HtmlEncode(item) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Status</label>
        <label class="checkbox-label">
          <input type="checkbox" name="isActive" value="true" <%= Input.IsActive ? "checked" : "" %> /> Active
        </label>
      </div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="return validateDesignationForm();">
      <%= Input.DesignationID > 0 ? "Update" : "Save" %>
    </button>
    <a href="<%= ResolveUrl("~/DesignationSetup.aspx") %>" class="btn btn-secondary">Cancel</a>
  </div>
</div>

<div class="card mt-4">
  <div class="card-header space-between">
    <h2>Designation List</h2>
    <input type="text" id="txtSearch" class="form-control" style="width:220px" placeholder="Search&hellip;" onkeyup="searchTable(this.value)" />
  </div>
  <div class="card-body table-responsive">
    <table class="data-table" id="dataTable">
      <thead class="grid-header">
        <tr>
          <th>Title</th>
          <th>Grade</th>
          <th>Employee Category</th>
          <th>Probation Days</th>
          <th>Privilege Code</th>
          <th>Privilege Tier</th>
          <th>Position Tenure</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <% if (Records.Count == 0) { %>
        <tr class="empty-row"><td colspan="9">No records found.</td></tr>
        <% } else {
             foreach (var item in Records) { %>
        <tr>
          <td><%= Server.HtmlEncode(item.TitleName) %></td>
          <td><%= Server.HtmlEncode(item.GradeName) %></td>
          <td><%= Server.HtmlEncode(item.EmployeeCategoryName) %></td>
          <td><%= Server.HtmlEncode(item.ProbationDays) %></td>
          <td><%= Server.HtmlEncode(item.PrivilegeCode) %></td>
          <td><%= Server.HtmlEncode(item.PrivilegeTier) %></td>
          <td><%= Server.HtmlEncode(item.PositionTenure) %></td>
          <td><span class="badge <%= item.IsActive ? "badge-success" : "badge-danger" %>"><%= item.IsActive ? "Active" : "Inactive" %></span></td>
          <td class="actions-col">
            <a class="btn-icon btn-edit" href="<%= ResolveUrl("~/DesignationSetup.aspx") %>?editId=<%= item.DesignationID %>">Edit</a>
            <button type="submit" class="btn-icon btn-delete"
                    onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= item.DesignationID %>';return confirm('Deactivate this designation?');">X</button>
          </td>
        </tr>
        <% } } %>
      </tbody>
    </table>
  </div>
</div>
</asp:Content>

<asp:Content ID="Scripts" ContentPlaceHolderID="scripts" runat="server">
<script>
function validateDesignationForm() {
  document.getElementById('__handler').value = 'Save';
  var el = document.getElementById('txtProbationDays');
  var err = document.getElementById('errProbationDays');
  var value = el ? (el.value || '').trim() : '';
  if (err) err.textContent = '';
  if (el) el.classList.remove('field-invalid');
  if (value && !/^\d+$/.test(value)) {
    if (err) err.textContent = 'Probation Days must be a whole number. Decimal and non-numeric values are not allowed.';
    if (el) {
      el.classList.add('field-invalid');
      el.focus();
    }
    return false;
  }
  return true;
}
function searchTable(q) {
  q = (q || '').toLowerCase();
  document.querySelectorAll('#dataTable tbody tr').forEach(function (r) {
    r.style.display = r.innerText.toLowerCase().includes(q) ? '' : 'none';
  });
}
</script>
</asp:Content>
