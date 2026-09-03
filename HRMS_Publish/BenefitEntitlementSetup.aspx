<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Inherits="HRMS.BenefitEntitlementSetupPage" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="SaveEntitlement" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<input type="hidden" name="detailID" id="detailID" value="0" />
<input type="hidden" name="benefitEntitlementID" id="benefitEntitlementID" value="<%= ManageEntitlementID %>" />

<div class="card">
  <div class="card-header">
    <h2><%= EntitlementInput.Id > 0 ? "Edit Benefit Entitlement" : "Add Benefit Entitlement" %></h2>
  </div>
  <div class="card-body">
    <input type="hidden" name="itemId" value="<%= EntitlementInput.Id %>" />
    <input type="hidden" name="manageId" value="<%= ManageEntitlementID %>" />
    <div class="form-grid">
      <div class="form-group">
        <label>Entitlement Name <span class="required">*</span></label>
        <input type="text" name="itemName" class="form-control"
               value="<%= Server.HtmlEncode(EntitlementInput.Name) %>" maxlength="150"
               placeholder="e.g. Management Grade Benefits" />
      </div>
      <div class="form-group">
        <label>Alias</label>
        <input type="text" name="aliasName" class="form-control"
               value="<%= Server.HtmlEncode(EntitlementInput.AliasName) %>" maxlength="100"
               placeholder="Short name / abbreviation" />
      </div>
      <div class="form-group">
        <label>Status</label>
        <label class="checkbox-label">
          <input type="checkbox" name="isActive" value="true" <%= EntitlementInput.IsActive ? "checked" : "" %> /> Active
        </label>
      </div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='SaveEntitlement';">
      <%= EntitlementInput.Id > 0 ? "Update" : "Save" %>
    </button>
    <a href="/BenefitEntitlementSetup.aspx" class="btn btn-secondary">Clear</a>
  </div>
</div>

<div class="card mt-4">
  <div class="card-header space-between">
    <h2>Benefit Entitlement List</h2>
    <input type="text" id="txtSearch" class="form-control" style="width:220px"
           placeholder="Search…" onkeyup="searchBenefitEntitlementTable('entitlementTable', this.value)" />
  </div>
  <div class="card-body table-responsive">
    <table class="data-table" id="entitlementTable">
      <thead class="grid-header">
        <tr>
          <th>Name</th>
          <th>Alias</th>
          <th>Benefits</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
      <% if (Entitlements.Count == 0) { %>
        <tr class="empty-row"><td colspan="5">No records found.</td></tr>
      <% } else {
           foreach (var item in Entitlements) {
             var benefitCount = EntitlementBenefitCounts.ContainsKey(item.Id) ? EntitlementBenefitCounts[item.Id] : 0; %>
        <tr>
          <td><%= Server.HtmlEncode(item.Name) %></td>
          <td><%= Server.HtmlEncode(item.AliasName) %></td>
          <td><span class="badge badge-success"><%= benefitCount %></span></td>
          <td>
            <span class="badge <%= item.IsActive ? "badge-success" : "badge-danger" %>">
              <%= item.IsActive ? "Active" : "Inactive" %>
            </span>
          </td>
          <td class="actions-col">
            <a class="btn-icon btn-edit" href="/BenefitEntitlementSetup.aspx?editId=<%= item.Id %>">Edit</a>
            <a class="btn-icon btn-edit" href="/BenefitEntitlementSetup.aspx?manageId=<%= item.Id %>">Manage</a>
            <button type="submit" class="btn-icon btn-delete"
                    onclick="document.getElementById('__handler').value='DeleteEntitlement';document.getElementById('deleteId').value='<%= item.Id %>';return confirm('Remove this benefit entitlement?');">X</button>
          </td>
        </tr>
      <% } } %>
      </tbody>
    </table>
  </div>
</div>

<% if (ManageEntitlementID > 0) { %>
<div class="card mt-4">
  <div class="card-header space-between">
    <h2>Manage Benefits — <%= Server.HtmlEncode(ManageEntitlementName) %></h2>
    <a href="/BenefitEntitlementSetup.aspx" class="btn btn-secondary">Close</a>
  </div>
  <div class="card-body">
    <div class="form-grid" style="align-items:end;margin-bottom:1.5rem;">
      <div class="form-group" style="flex:1;">
        <label>Add Benefit</label>
        <select name="benefitID" id="benefitID" class="form-control">
          <option value="">-- Select Benefit --</option>
          <% foreach (var ben in AvailableBenefits) { %>
          <option value="<%= ben.BenefitID %>"><%= Server.HtmlEncode(ben.BenefitCode + " — " + ben.BenefitName + " (" + ben.BenefitType + ")") %></option>
          <% } %>
        </select>
      </div>
      <div class="form-group">
        <button type="submit" class="btn btn-primary"
                onclick="document.getElementById('__handler').value='LinkBenefit';document.getElementById('benefitEntitlementID').value='<%= ManageEntitlementID %>';">
          Link Benefit
        </button>
      </div>
    </div>

    <div class="table-responsive">
      <table class="data-table" id="linkedTable">
        <thead class="grid-header">
          <tr>
            <th>Code</th>
            <th>Name</th>
            <th>Type</th>
            <th>Description</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
        <% if (LinkedBenefits.Count == 0) { %>
          <tr class="empty-row"><td colspan="5">No benefits linked yet.</td></tr>
        <% } else {
             foreach (var ben in LinkedBenefits) { %>
          <tr>
            <td><code style="font-size:.75rem;background:#f1f5f9;padding:.1rem .4rem;border-radius:4px;"><%= Server.HtmlEncode(ben.BenefitCode) %></code></td>
            <td><%= Server.HtmlEncode(ben.BenefitName) %></td>
            <td><%= Server.HtmlEncode(ben.BenefitType) %></td>
            <td><%= Server.HtmlEncode(ben.Description) %></td>
            <td class="actions-col">
              <button type="submit" class="btn-icon btn-delete"
                      onclick="document.getElementById('__handler').value='UnlinkBenefit';document.getElementById('detailID').value='<%= ben.DetailID %>';document.getElementById('benefitEntitlementID').value='<%= ManageEntitlementID %>';return confirm('Remove this benefit from the entitlement?');">X</button>
            </td>
          </tr>
        <% } } %>
        </tbody>
      </table>
    </div>
  </div>
</div>
<% } %>
</asp:Content>

<asp:Content ID="Scripts" ContentPlaceHolderID="scripts" runat="server">
<script>
function searchBenefitEntitlementTable(tableId, q) {
    q = (q || '').toLowerCase();
    document.querySelectorAll('#' + tableId + ' tbody tr').forEach(function (r) {
        r.style.display = r.innerText.toLowerCase().includes(q) ? '' : 'none';
    });
}
</script>
</asp:Content>
