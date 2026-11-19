<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ContactMaster.aspx.cs" Inherits="HRMS.ContactMasterPage" %>
<%@ Import Namespace="HRMS.Services" %>
<%@ Register Src="~/Controls/ContactHubList.ascx" TagPrefix="hrms" TagName="ContactHubList" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />
<input type="hidden" name="deleteAddressId" id="deleteAddressId" value="0" />

<% if (!ShowForm) { %>
<hrms:ContactHubList runat="server" HubView="All" ListPath="/ContactMaster.aspx" PageHeading="All Contacts" />
<% } else { %>
<div class="form-breadcrumb" style="margin-bottom:1rem;">
  <a href="/ContactMaster.aspx" class="btn btn-secondary">&#8592; Back</a>
  <span><%= EditMode ? "Edit Contact" : "New Contact" %> <%= Server.HtmlEncode(Input.ContactCode) %></span>
</div>
<input type="hidden" name="ContactID" value="<%= Input.ContactID %>" />
<input type="hidden" name="EditMode" value="<%= EditMode ? "true" : "false" %>" />
<div class="card">
  <div class="card-header"><h2><%= EditMode ? "Edit Contact" : "Add Contact" %>
    <% if (Input.ContactID > 0) { %>
    <span class="<%= ContactWorkflowStatuses.BadgeClass(Input.WorkflowStatus) %>"><%= Server.HtmlEncode(ContactWorkflowStatuses.Display(Input.WorkflowStatus)) %></span>
    <span class="<%= ContactVerification.BadgeClass(Input.VerificationStatus) %>"><%= Server.HtmlEncode(ContactVerification.Display(Input.VerificationStatus)) %></span>
    <% } %>
  </h2></div>
  <div class="card-body">
    <% if (PendingRequest != null) { %>
    <div class="alert alert-warning">
      Workflow <strong><%= Server.HtmlEncode(PendingRequest.RequestCode) %></strong> is in process
      (<%= Server.HtmlEncode(PendingRequest.ActionType) %>).
      <% if (PendingRequest.ActionType == "Update") { %>Proposed values are shown below and will not replace the live contact until approved.<% } %>
      <a href="/ContactChangeReview.aspx?id=<%= PendingRequest.RequestID %>">Open workflow</a>
    </div>
    <% } %>
    <div class="form-grid">
      <div class="form-group"><label>Contact Code</label><input type="text" class="form-control" value="<%= Server.HtmlEncode(Input.ContactCode) %>" readonly /></div>
      <div class="form-group"><label>Contact Type *</label>
        <select name="ContactType" id="selContactType" class="form-control" required onchange="onContactTypeChange();">
          <option value="">-- Select type --</option>
          <% foreach (var t in ContactTypes) { %><option value="<%= t %>" <%= Input.ContactType==t?"selected":"" %>><%= t %></option><% } %>
        </select></div>
      <div class="form-group" id="wrapCustomer">
        <label>Customer Master *</label>
        <input type="text" class="form-control" placeholder="Search customer..." oninput="filterLookup('selCustomer', this.value)" />
        <select name="CustomerID" id="selCustomer" class="form-control" style="margin-top:.35rem;">
          <option value="0">-- Select customer --</option>
          <% foreach (var c in Customers) { %><option value="<%= c.Id %>" <%= Input.CustomerID==c.Id?"selected":"" %>><%= Server.HtmlEncode(c.Name) %></option><% } %>
        </select>
      </div>
      <div class="form-group" id="wrapVendor">
        <label>Vendor Master *</label>
        <input type="text" class="form-control" placeholder="Search vendor..." oninput="filterLookup('selVendor', this.value)" />
        <select name="VendorID" id="selVendor" class="form-control" style="margin-top:.35rem;">
          <option value="0">-- Select vendor --</option>
          <% foreach (var v in Vendors) { %><option value="<%= v.Id %>" <%= Input.VendorID==v.Id?"selected":"" %>><%= Server.HtmlEncode(v.Name) %></option><% } %>
        </select>
      </div>
      <div class="form-group" id="wrapEmployee">
        <label>Employee Master *</label>
        <input type="text" class="form-control" placeholder="Search employee..." oninput="filterLookup('selEmployee', this.value)" />
        <select name="EmployeeID" id="selEmployee" class="form-control" style="margin-top:.35rem;">
          <option value="0">-- Select employee --</option>
          <% foreach (var e in Employees) { %><option value="<%= e.Id %>" <%= Input.EmployeeID==e.Id?"selected":"" %>><%= Server.HtmlEncode(e.Name) %></option><% } %>
        </select>
      </div>
      <div class="form-group" id="wrapGuest">
        <label>Guest Name / Description *</label>
        <input type="text" name="GuestName" id="txtGuestName" class="form-control" value="<%= Server.HtmlEncode(Input.GuestName) %>" maxlength="200" />
      </div>
      <div class="form-group"><label>Name *</label><input type="text" name="Name" id="txtContactName" class="form-control" value="<%= Server.HtmlEncode(Input.Name) %>" /></div>
      <div class="form-group"><label>Search Name</label><input type="text" name="SearchName" class="form-control" value="<%= Server.HtmlEncode(Input.SearchName) %>" /></div>
      <div class="form-group"><label>Contact For</label><input type="text" name="ContactFor" class="form-control" value="<%= Server.HtmlEncode(Input.ContactFor) %>" /></div>
      <div class="form-group"><label>Status</label>
        <select name="ContactStatus" class="form-control">
        <% foreach (var s in ContactStatuses) { %><option value="<%= s %>" <%= Input.ContactStatus==s?"selected":"" %>><%= s %></option><% } %>
        </select></div>
      <div class="form-group"><label>Record Type</label>
        <select name="RecordType" class="form-control">
          <option value="People" <%= Input.RecordType=="People"?"selected":"" %>>People</option>
          <option value="Organisation" <%= Input.RecordType=="Organisation"?"selected":"" %>>Organisation</option>
        </select></div>
      <div class="form-group"><label>Verified Status</label>
        <input type="text" class="form-control" value="<%= Server.HtmlEncode(ContactVerification.Display(Input.VerificationStatus)) %>" readonly disabled />
        <small class="text-muted">Updated only when the approval workflow completes.</small>
      </div>
      <div class="form-group"><label>Approval / Access</label>
        <select name="AccessStatus" class="form-control">
          <option value="Open" <%= Input.AccessStatus=="Open"?"selected":"" %>>Open</option>
          <option value="Restricted" <%= Input.AccessStatus=="Restricted"?"selected":"" %>>Restricted</option>
          <option value="Quarantined" <%= Input.AccessStatus=="Quarantined"?"selected":"" %>>Quarantined</option>
        </select></div>
      <div class="form-group"><label>Gender</label>
        <select name="GenderID" class="form-control"><option value="0">--</option>
        <% foreach (var g in Genders) { %><option value="<%= g.Id %>" <%= Input.GenderID==g.Id?"selected":"" %>><%= Server.HtmlEncode(g.Name) %></option><% } %>
        </select></div>
      <div class="form-group"><label>Marital Status</label>
        <select name="MaritalStatus" class="form-control"><option value="">--</option>
        <% foreach (var m in MaritalStatuses) { %><option value="<%= m %>" <%= Input.MaritalStatus==m?"selected":"" %>><%= m %></option><% } %>
        </select></div>
      <div class="form-group"><label>Professional Title</label><input type="text" name="ProfessionalTitle" class="form-control" value="<%= Server.HtmlEncode(Input.ProfessionalTitle) %>" /></div>
      <div class="form-group"><label>Department</label><input type="text" name="Department" class="form-control" value="<%= Server.HtmlEncode(Input.Department) %>" /></div>
      <div class="form-group"><label>Office Location</label><input type="text" name="OfficeLocation" class="form-control" value="<%= Server.HtmlEncode(Input.OfficeLocation) %>" /></div>
      <div class="form-group"><label>Available From</label><input type="time" name="AvailableFrom" class="form-control" value="<%= Server.HtmlEncode(Input.AvailableFrom) %>" /></div>
      <div class="form-group"><label>Available To</label><input type="time" name="AvailableTo" class="form-control" value="<%= Server.HtmlEncode(Input.AvailableTo) %>" /></div>
      <div class="form-group"><label>Reports To</label><input type="text" name="ReportToManagerName" class="form-control" value="<%= Server.HtmlEncode(Input.ReportToManagerName) %>" /></div>
      <div class="form-group"><label>Phone</label><input type="text" name="Phone" id="txtPhone" class="form-control" value="<%= Server.HtmlEncode(Input.Phone) %>" /></div>
      <div class="form-group"><label>Mobile</label><input type="text" name="Mobile" id="txtMobile" class="form-control" value="<%= Server.HtmlEncode(Input.Mobile) %>" /></div>
      <div class="form-group"><label>Email</label><input type="text" name="Email" id="txtEmail" class="form-control" value="<%= Server.HtmlEncode(Input.Email) %>" /></div>
      <div class="form-group"><label>WhatsApp</label><input type="text" name="Whatsapp" id="txtWhatsapp" class="form-control" value="<%= Server.HtmlEncode(Input.Whatsapp) %>" /></div>
      <div class="form-group"><label>URL</label><input type="text" name="URL" class="form-control" value="<%= Server.HtmlEncode(Input.URL) %>" /></div>
      <div class="form-group"><label>Fax</label><input type="text" name="Fax" id="txtFax" class="form-control" value="<%= Server.HtmlEncode(Input.Fax) %>" /></div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='Save'; return validateContactForm();"><%= EditMode ? "Submit for approval" : "Create and submit for approval" %></button>
    <a href="/ContactMaster.aspx" class="btn btn-secondary">Cancel</a>
  </div>
</div>
<% if (EditMode && Input.ContactID > 0) { %>
<div class="card" style="margin-top:1rem;">
  <div class="card-header space-between">
    <h2>QR Code</h2>
    <a class="btn btn-secondary" href="<%= ContactQrViewUrl %>">Open QR card</a>
  </div>
  <div class="card-body" style="display:flex;flex-wrap:wrap;gap:1.25rem;align-items:center;">
    <a href="<%= ContactQrViewUrl %>">
      <img class="hub-qr-thumb" alt="Contact QR code" width="140" height="140" style="width:140px;height:140px;"
           src="<%= ContactQrPngUrl %>" />
    </a>
    <div>
      <p style="margin:0 0 .5rem 0;">Unique QR code for this contact. Scan to read name, mobile, email, company, and address. The image is generated from the saved record, so it updates after contact changes are approved.</p>
      <div style="display:flex;flex-wrap:wrap;gap:.5rem;">
        <a class="btn btn-primary" href="<%= ContactQrViewUrl %>">View / Print / Share</a>
        <a class="btn btn-secondary" href="<%= ContactQrDownloadUrl %>">Download PNG</a>
        <a class="btn btn-secondary" href="/ContactExport.aspx?id=<%= Input.ContactID %>">Download vCard</a>
      </div>
    </div>
  </div>
</div>
<input type="hidden" name="deleteAddressId" id="deleteAddressId" value="0" />
<div class="card" style="margin-top:1rem;">
  <div class="card-header space-between">
    <h2>Contact Addresses</h2>
    <a href="/ContactLocationSetup.aspx" class="btn btn-secondary">Address Lookups</a>
  </div>
  <div class="card-body">
    <p style="color:#6b7280;margin:0 0 1rem 0;">Select Country, State, City, Postal Code, and Street from master setup. Optional address detail is for house / plot / floor. One primary, one billing, and one shipping address per contact. Address create, change, and delete start an approval workflow. Verified status is set only after approval.</p>
    <% if (PendingAddressRequest != null) { %>
    <div class="alert alert-warning">
      Address workflow <strong><%= Server.HtmlEncode(PendingAddressRequest.RequestCode) %></strong> is in process
      (<%= Server.HtmlEncode(PendingAddressRequest.ActionDisplay) %>).
      <% if (PendingAddressRequest.ActionType == "Update") { %>Proposed values are shown below and will not replace the live address until approved.<% } %>
      <a href="/ContactChangeReview.aspx?id=<%= PendingAddressRequest.RequestID %>">Open workflow</a>
    </div>
    <% } %>
    <input type="hidden" name="AddressID" value="<%= AddressInput.AddressID %>" />
    <div class="form-grid">
      <div class="form-group"><label>Country *</label>
        <select name="CountryID" id="selCountry" class="form-control" onchange="onCountryChange();">
          <option value="0">-- Select country --</option>
          <% foreach (var c in AddressCountries) { %>
          <option value="<%= c.Id %>" <%= AddressInput.CountryID==c.Id?"selected":"" %>><%= Server.HtmlEncode(c.Name) %></option>
          <% } %>
        </select></div>
      <div class="form-group"><label>State *</label>
        <select name="StateID" id="selState" class="form-control" onchange="onStateChange();">
          <option value="0">-- Select state --</option>
          <% foreach (var s in AddressStates) { %>
          <option value="<%= s.Id %>" <%= AddressInput.StateID==s.Id?"selected":"" %>><%= Server.HtmlEncode(s.Name) %></option>
          <% } %>
        </select></div>
      <div class="form-group"><label>City *</label>
        <select name="CityID" id="selCity" class="form-control" onchange="onCityChange();">
          <option value="0">-- Select city --</option>
          <% foreach (var city in AddressCities) { %>
          <option value="<%= city.Id %>" <%= AddressInput.CityID==city.Id?"selected":"" %>><%= Server.HtmlEncode(city.Name) %></option>
          <% } %>
        </select></div>
      <div class="form-group"><label>Postal Code *</label>
        <select name="PostalID" id="selPostal" class="form-control" onchange="onPostalChange();">
          <option value="0">-- Select postal code --</option>
          <% foreach (var p in AddressPostals) { %>
          <option value="<%= p.Id %>" <%= AddressInput.PostalID==p.Id?"selected":"" %>><%= Server.HtmlEncode(p.Name) %></option>
          <% } %>
        </select></div>
      <div class="form-group"><label>Street *</label>
        <select name="StreetID" id="selStreet" class="form-control">
          <option value="0">-- Select street --</option>
          <% foreach (var st in AddressStreets) { %>
          <option value="<%= st.Id %>" <%= AddressInput.StreetID==st.Id?"selected":"" %>><%= Server.HtmlEncode(st.Name) %></option>
          <% } %>
        </select></div>
      <div class="form-group"><label>Address Detail</label>
        <input type="text" name="AddressLine" class="form-control" value="<%= Server.HtmlEncode(AddressInput.AddressLine) %>" maxlength="500" placeholder="House / plot / floor (optional)" /></div>
      <div class="form-group"><label>Latitude</label>
        <input type="text" name="Latitude" class="form-control" value="<%= Server.HtmlEncode(AddressInput.LatitudeText) %>" placeholder="-90 to 90" /></div>
      <div class="form-group"><label>Longitude</label>
        <input type="text" name="Longitude" class="form-control" value="<%= Server.HtmlEncode(AddressInput.LongitudeText) %>" placeholder="-180 to 180" /></div>
      <div class="form-group"><label>Coordinate</label>
        <input type="text" class="form-control" value="<%= Server.HtmlEncode(AddressInput.Coordinate) %>" readonly /></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="IsPrimary" value="true" <%= AddressInput.IsPrimary?"checked":"" %> /> Primary</label></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="IsBilling" value="true" <%= AddressInput.IsBilling?"checked":"" %> /> Billing</label></div>
      <div class="form-group"><label class="checkbox-label"><input type="checkbox" name="IsShipping" value="true" <%= AddressInput.IsShipping?"checked":"" %> /> Shipping</label></div>
    </div>
  </div>
  <div class="card-footer">
    <button type="submit" class="btn btn-primary" onclick="return validateContactAddress();"><%= AddressInput.AddressID > 0 ? "Submit address for approval" : "Create address and submit for approval" %></button>
    <% if (AddressInput.AddressID > 0) { %>
    <a href="/ContactMaster.aspx?editId=<%= Input.ContactID %>" class="btn btn-secondary">Cancel Address Edit</a>
    <% } %>
  </div>
  <div class="card-body table-responsive">
    <table class="data-table">
      <thead><tr><th>Street</th><th>Postal</th><th>City</th><th>State</th><th>Country</th><th>Detail</th><th>Coordinate</th><th>Type</th><th>Workflow</th><th>Verified</th><th></th></tr></thead>
      <tbody>
      <% foreach (var a in Addresses) { %>
      <tr>
        <td><%= Server.HtmlEncode(a.StreetName) %></td>
        <td><%= Server.HtmlEncode(a.PostalCode) %></td>
        <td><%= Server.HtmlEncode(a.CityName) %></td>
        <td><%= Server.HtmlEncode(a.StateName) %></td>
        <td><%= Server.HtmlEncode(a.CountryName) %></td>
        <td><%= Server.HtmlEncode(a.AddressLine) %></td>
        <td><%= Server.HtmlEncode(a.Coordinate) %></td>
        <td><%= Server.HtmlEncode(a.TypeDisplay) %></td>
        <td><span class="<%= ContactWorkflowStatuses.BadgeClass(a.WorkflowStatus) %>"><%= Server.HtmlEncode(ContactWorkflowStatuses.Display(a.WorkflowStatus)) %></span></td>
        <td><span class="<%= ContactVerification.BadgeClass(a.VerificationStatus) %>"><%= Server.HtmlEncode(ContactVerification.Display(a.VerificationStatus)) %></span></td>
        <td>
          <a href="/ContactMaster.aspx?editId=<%= Input.ContactID %>&amp;addressId=<%= a.AddressID %>">Edit</a>
          <% var addrWf = RequestsForAddress(a.AddressID); if (addrWf.Count > 0) { %>
          <a href="#wf-<%= addrWf[0].RequestID %>">History (<%= addrWf.Count %>)</a>
          <% } %>
          <button type="submit" onclick="document.getElementById('__handler').value='DeleteAddress';document.getElementById('deleteAddressId').value='<%= a.AddressID %>';return confirm('Submit this address for delete approval?');">X</button>
        </td>
      </tr>
      <% } %>
      <% if (Addresses.Count == 0) { %><tr><td colspan="11">No addresses yet.</td></tr><% } %>
      </tbody>
    </table>
  </div>
</div>
<% } else { %>
<p style="margin-top:1rem;color:#6b7280;">Save the contact first to add one or more addresses.</p>
<% } %>

<% if (EditMode && Input.ContactID > 0) { %>
<div class="card" style="margin-top:1rem;" id="workflow-history">
  <div class="card-header"><h2>Workflow history</h2></div>
  <div class="card-body">
    <p class="text-muted">Every contact and address create, update, approve, and reject remains here after the workflow finishes.</p>
    <% if (WorkflowRequests.Count == 0) { %>
    <p>No workflow transactions have been recorded for this contact yet.</p>
    <% } foreach (var r in WorkflowRequests) { %>
    <div id="wf-<%= r.RequestID %>" class="card" style="margin:0 0 1rem 0;border:1px solid #e5e7eb;">
      <div class="card-body">
        <p>
          <strong><%= Server.HtmlEncode(r.RequestCode) %></strong>
          <span class="<%= r.BadgeClass %>"><%= Server.HtmlEncode(r.StatusDisplay) %></span>
          &middot; <%= Server.HtmlEncode(r.ActionDisplay) %>
          <% if (r.AddressID > 0) { %> &middot; Address #<%= r.AddressID %><% } %>
          &middot; <a href="/ContactChangeReview.aspx?id=<%= r.RequestID %>">Open request</a>
        </p>
        <p>
          Initiated <%= r.SubmittedOn.ToString("dd-MMM-yyyy HH:mm") %>
          by <%= Server.HtmlEncode(r.SubmittedByName) %>
          <% if (r.DecidedOn.HasValue) { %>
          &middot; <%= Server.HtmlEncode(r.StatusDisplay) %> <%= r.DecidedOn.Value.ToString("dd-MMM-yyyy HH:mm") %>
          by <%= Server.HtmlEncode(r.DecidedByName) %>
          <% } %>
        </p>
        <% if (!string.IsNullOrEmpty(r.DecisionComments)) { %>
        <p><strong>Approver comments:</strong> <%= Server.HtmlEncode(r.DecisionComments) %></p>
        <% } %>
        <div class="table-responsive">
          <table class="data-table">
            <thead><tr><th>Field</th><th>Previous value</th><th>Updated value</th></tr></thead>
            <tbody>
            <% if (r.Lines.Count == 0) { %><tr><td colspan="3">No field differences captured.</td></tr><% } %>
            <% foreach (var line in r.Lines) { %>
            <tr>
              <td><%= Server.HtmlEncode(line.FieldLabel) %></td>
              <td><%= Server.HtmlEncode(line.OldValue) %></td>
              <td><%= Server.HtmlEncode(line.NewValue) %></td>
            </tr>
            <% } %>
            </tbody>
          </table>
        </div>
        <ul style="margin-top:.75rem;">
        <% foreach (var h in r.History) { %>
          <li><%= h.ActionOn.ToString("dd-MMM-yyyy HH:mm") %> &mdash; <%= Server.HtmlEncode(h.StatusDisplay) %>
              by <%= Server.HtmlEncode(h.ActorName) %>
              <% if (!string.IsNullOrEmpty(h.Comments)) { %> &middot; <%= Server.HtmlEncode(h.Comments) %><% } %></li>
        <% } %>
        </ul>
      </div>
    </div>
    <% } %>
  </div>
</div>
<div class="card" style="margin-top:1rem;" id="audit-trail">
  <div class="card-header"><h2>Audit trail</h2></div>
  <div class="card-body table-responsive">
    <p class="text-muted">System audit log for this contact and its addresses, including workflow initiation and decisions.</p>
    <table class="data-table">
      <thead><tr><th>When</th><th>User</th><th>Action</th><th>Record</th><th>Details</th></tr></thead>
      <tbody>
      <% if (AuditTrail.Count == 0) { %><tr><td colspan="5">No audit entries found for this contact yet.</td></tr><% } %>
      <% foreach (var log in AuditTrail) { %>
      <tr>
        <td><%= log.ActionAt.ToString("dd-MMM-yyyy HH:mm") %></td>
        <td><%= Server.HtmlEncode(log.Username) %></td>
        <td><%= Server.HtmlEncode(log.ActionType) %></td>
        <td><%= Server.HtmlEncode(log.EntityType) %> <%= Server.HtmlEncode(log.EntityName) %></td>
        <td style="white-space:pre-wrap;max-width:40rem;"><%= Server.HtmlEncode(log.Details) %></td>
      </tr>
      <% } %>
      </tbody>
    </table>
  </div>
</div>
<% } %>

<script>
function filterLookup(selectId, q) {
  q = (q || '').toLowerCase();
  var sel = document.getElementById(selectId);
  if (!sel) return;
  Array.prototype.forEach.call(sel.options, function (o) {
    if (!o.value || o.value === '0') { o.hidden = false; return; }
    o.hidden = q && (o.textContent || '').toLowerCase().indexOf(q) < 0;
  });
}
function onContactTypeChange() {
  var type = (document.getElementById('selContactType') || {}).value || '';
  var show = function (id, visible) {
    var el = document.getElementById(id);
    if (el) el.style.display = visible ? '' : 'none';
  };
  show('wrapCustomer', type === 'Customer');
  show('wrapVendor', type === 'Vendor');
  show('wrapEmployee', type === 'Employee');
  show('wrapGuest', type === 'Guest/Others');
}
function validateContactForm() {
  var typeEl = document.getElementById('selContactType');
  var type = typeEl ? typeEl.value : '';
  if (!type) { alert('Contact type is required.'); if (typeEl) typeEl.focus(); return false; }
  if (type === 'Customer') {
    var c = document.getElementById('selCustomer');
    if (!c || c.value === '0') { alert('Select a customer from Customer Master.'); if (c) c.focus(); return false; }
  } else if (type === 'Vendor') {
    var v = document.getElementById('selVendor');
    if (!v || v.value === '0') { alert('Select a vendor from Vendor Master.'); if (v) v.focus(); return false; }
  } else if (type === 'Employee') {
    var e = document.getElementById('selEmployee');
    if (!e || e.value === '0') { alert('Select an employee from Employee Master.'); if (e) e.focus(); return false; }
  } else if (type === 'Guest/Others') {
    var g = document.getElementById('txtGuestName');
    if (!g || !(g.value || '').trim()) { alert('Guest Name / Description is required.'); if (g) g.focus(); return false; }
    var n = document.getElementById('txtContactName');
    if (n && !(n.value || '').trim()) n.value = (g.value || '').trim();
  }
  return validateContactChannels();
}
document.addEventListener('DOMContentLoaded', onContactTypeChange);
function digitsOnly(v) { return (v || '').replace(/\D/g, ''); }
function validateContactChannels() {
  var fields = [
    { label: 'Phone', el: document.getElementById('txtPhone'), phone: true },
    { label: 'Mobile', el: document.getElementById('txtMobile'), phone: true },
    { label: 'WhatsApp', el: document.getElementById('txtWhatsapp'), phone: true },
    { label: 'Fax', el: document.getElementById('txtFax'), phone: true },
    { label: 'Email', el: document.getElementById('txtEmail'), phone: false }
  ];
  var seen = {};
  for (var i = 0; i < fields.length; i++) {
    var f = fields[i];
    if (!f.el) continue;
    var raw = (f.el.value || '').trim();
    if (!raw) continue;
    var key = f.phone ? digitsOnly(raw) : raw.toLowerCase();
    if (!key) continue;
    if (seen[key]) {
      alert(f.label + ' cannot be the same as ' + seen[key] + '. Phone, Mobile, Fax, WhatsApp, and Email must be unique. Please recorrect the value.');
      f.el.focus();
      return false;
    }
    seen[key] = f.label;
  }
  return true;
}
function fillLookup(sel, items, placeholder) {
  if (!sel) return;
  var current = sel.value;
  sel.innerHTML = '';
  var blank = document.createElement('option');
  blank.value = '0';
  blank.textContent = placeholder;
  sel.appendChild(blank);
  (items || []).forEach(function (it) {
    var o = document.createElement('option');
    o.value = String(it.Id);
    o.textContent = it.Name;
    sel.appendChild(o);
  });
  sel.value = current && sel.querySelector('option[value="' + current + '"]') ? current : '0';
}
function loadLookup(kind, parentId, target, placeholder, thenClear) {
  if (!parentId || parentId === '0') {
    fillLookup(target, [], placeholder);
    thenClear();
    return;
  }
  fetch('/ContactMaster.aspx?geo=' + encodeURIComponent(kind) + '&parentId=' + encodeURIComponent(parentId), { credentials: 'same-origin' })
    .then(function (r) { return r.json(); })
    .then(function (data) {
      fillLookup(target, data, placeholder);
      thenClear();
    })
    .catch(function () { thenClear(); });
}
function onCountryChange() {
  loadLookup('states', document.getElementById('selCountry').value, document.getElementById('selState'), '-- Select state --', function () {
    fillLookup(document.getElementById('selCity'), [], '-- Select city --');
    fillLookup(document.getElementById('selPostal'), [], '-- Select postal code --');
    fillLookup(document.getElementById('selStreet'), [], '-- Select street --');
  });
}
function onStateChange() {
  loadLookup('cities', document.getElementById('selState').value, document.getElementById('selCity'), '-- Select city --', function () {
    fillLookup(document.getElementById('selPostal'), [], '-- Select postal code --');
    fillLookup(document.getElementById('selStreet'), [], '-- Select street --');
  });
}
function onCityChange() {
  loadLookup('postals', document.getElementById('selCity').value, document.getElementById('selPostal'), '-- Select postal code --', function () {
    fillLookup(document.getElementById('selStreet'), [], '-- Select street --');
  });
}
function onPostalChange() {
  loadLookup('streets', document.getElementById('selPostal').value, document.getElementById('selStreet'), '-- Select street --', function () {});
}
function validateContactAddress() {
  var h = document.getElementById('__handler');
  if (h) h.value = 'SaveAddress';
  var country = document.getElementById('selCountry');
  var state = document.getElementById('selState');
  var city = document.getElementById('selCity');
  var postal = document.getElementById('selPostal');
  var street = document.getElementById('selStreet');
  if (!country || country.value === '0') { alert('Select a country from Country Setup.'); if (country) country.focus(); return false; }
  if (!state || state.value === '0') { alert('Select a state from State Setup.'); if (state) state.focus(); return false; }
  if (!city || city.value === '0') { alert('Select a city from City Setup.'); if (city) city.focus(); return false; }
  if (!postal || postal.value === '0') { alert('Select a postal code from Postal Setup.'); if (postal) postal.focus(); return false; }
  if (!street || street.value === '0') { alert('Select a street from Street Setup.'); if (street) street.focus(); return false; }
  return true;
}
</script>
<% } %>
<% if (!string.IsNullOrEmpty(PopupMessage)) { %>
<script>alert('<%= System.Web.HttpUtility.JavaScriptStringEncode(PopupMessage) %>');</script>
<% } %>
</asp:Content>
