<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.BenefitSetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<div class="card">
  <div class="card-header">
    <h2><%= Input.BenefitID > 0 ? "Edit Benefit" : "Add Benefit" %></h2>
  </div>
  <div class="card-body">
    <input type="hidden" name="benefitId" value="<%= Input.BenefitID %>" />
    <div class="form-grid">
      <div class="form-group">
        <label>Code</label>
        <input type="text" name="benefitCode" class="form-control"
               value="<%= Server.HtmlEncode(Input.BenefitCode) %>" readonly
               style="background:#f8fafc;color:var(--text-muted);cursor:not-allowed;" />
      </div>
      <div class="form-group">
        <label>Name <span class="required">*</span></label>
        <input type="text" name="benefitName" class="form-control"
               value="<%= Server.HtmlEncode(Input.BenefitName) %>" maxlength="150"
               placeholder="e.g. Medical Allowance" />
      </div>
      <div class="form-group">
        <label>Type</label>
        <select name="benefitType" class="form-control">
          <option value="">-- Select Type --</option>
          <% foreach (var bt in BenefitTypeOptions) { %>
          <option value="<%= Server.HtmlEncode(bt) %>" <%= Input.BenefitType == bt ? "selected" : "" %>><%= Server.HtmlEncode(bt) %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <label>Status</label>
        <label class="checkbox-label">
          <input type="checkbox" name="isActive" value="true" <%= Input.IsActive ? "checked" : "" %> /> Active
        </label>
      </div>
      <div class="form-group full-width">
        <label>Description</label>
        <textarea name="description" class="form-control" rows="3" maxlength="500"
                  placeholder="Brief description..."><%= Server.HtmlEncode(Input.Description) %></textarea>
      </div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save';">
      <%= Input.BenefitID > 0 ? "Update" : "Save" %>
    </button>
    <a href="/BenefitSetup.aspx" class="btn btn-secondary">Clear</a>
  </div>
</div>

<div class="card mt-4">
  <div class="card-header space-between">
    <h2>Benefit List</h2>
    <input type="text" id="txtSearch" class="form-control" style="width:220px"
           placeholder="Search…" onkeyup="searchBenefitTable(this.value)" />
  </div>
  <div class="card-body table-responsive">
    <table class="data-table" id="dataTable">
      <thead class="grid-header">
        <tr>
          <th>Code</th>
          <th>Name</th>
          <th>Type</th>
          <th>Description</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
      <% if (Records.Count == 0) { %>
        <tr class="empty-row"><td colspan="6">No records found.</td></tr>
      <% } else {
           foreach (var item in Records) { %>
        <tr>
          <td><code style="font-size:.75rem;background:#f1f5f9;padding:.1rem .4rem;border-radius:4px;"><%= Server.HtmlEncode(item.BenefitCode) %></code></td>
          <td><%= Server.HtmlEncode(item.BenefitName) %></td>
          <td><%= Server.HtmlEncode(item.BenefitType) %></td>
          <td><%= Server.HtmlEncode(item.Description) %></td>
          <td>
            <span class="badge <%= item.IsActive ? "badge-success" : "badge-danger" %>">
              <%= item.IsActive ? "Active" : "Inactive" %>
            </span>
          </td>
          <td class="actions-col">
            <a class="btn-icon btn-edit" href="/BenefitSetup.aspx?editId=<%= item.BenefitID %>">Edit</a>
            <button type="submit" class="btn-icon btn-delete"
                    onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= item.BenefitID %>';return confirm('Deactivate this benefit?');">X</button>
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
function searchBenefitTable(q) {
    q = (q || '').toLowerCase();
    document.querySelectorAll('#dataTable tbody tr').forEach(function (r) {
        r.style.display = r.innerText.toLowerCase().includes(q) ? '' : 'none';
    });
}
</script>
</asp:Content>
