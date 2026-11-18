<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ContactHubList.ascx.cs" Inherits="HRMS.Controls.ContactHubList" %>
<%@ Import Namespace="HRMS.Services" %>
<div class="card">
  <div class="card-header space-between">
    <h2><%= H(PageHeading) %></h2>
    <div>
      <% if (CanWrite) { %><a href="<%= NewContactUrl() %>" class="btn btn-primary">+ New Contact</a><% } %>
    </div>
  </div>
  <div class="card-body">
    <p class="hub-chip-caption"><%= Result.Facets.All %> records you may see. Choose a type, status, or search.</p>
    <div class="hub-chip-row" role="tablist" aria-label="Contact filters">
      <a class="hub-chip <%= ChipActive("All") ? "is-active" : "" %>" href="<%= ChipUrl("All") %>">All <span><%= Result.Facets.All %></span></a>
      <% if (!LockTypeFilter) { %>
      <a class="hub-chip <%= ChipActive("Customers") ? "is-active" : "" %>" href="<%= ChipUrl("Customers") %>">Customers <span><%= Result.Facets.Customers %></span></a>
      <a class="hub-chip <%= ChipActive("Vendors") ? "is-active" : "" %>" href="<%= ChipUrl("Vendors") %>">Vendors <span><%= Result.Facets.Vendors %></span></a>
      <a class="hub-chip <%= ChipActive("Employees") ? "is-active" : "" %>" href="<%= ChipUrl("Employees") %>">Employees <span><%= Result.Facets.Employees %></span></a>
      <a class="hub-chip <%= ChipActive("ContactPersons") ? "is-active" : "" %>" href="<%= ChipUrl("ContactPersons") %>">Contact persons <span><%= Result.Facets.ContactPersons %></span></a>
      <a class="hub-chip <%= ChipActive("NoBusinessLink") ? "is-active" : "" %>" href="<%= ChipUrl("NoBusinessLink") %>">No business link <span><%= Result.Facets.NoBusinessLink %></span></a>
      <% } %>
      <a class="hub-chip hub-chip-warn <%= ChipActive("ToVerify") ? "is-active" : "" %>" href="<%= ChipUrl("ToVerify") %>">To verify <span><%= Result.Facets.ToVerify %></span></a>
      <a class="hub-chip hub-chip-warn <%= ChipActive("Restricted") ? "is-active" : "" %>" href="<%= ChipUrl("Restricted") %>">Restricted <span><%= Result.Facets.Restricted %></span></a>
      <a class="hub-chip <%= ChipActive("Quarantined") ? "is-active" : "" %>" href="<%= ChipUrl("Quarantined") %>">Quarantined <span><%= Result.Facets.Quarantined %></span></a>
    </div>
    <div class="hub-filter-bar">
      <div class="form-grid hub-filter-grid">
        <% if (!LockTypeFilter) { %>
        <div class="form-group">
          <label>Contact Type</label>
          <select id="hubCtype" class="form-control">
            <option value="All" <%= Filter.ContactType=="All"?"selected":"" %>>All</option>
            <option value="Customers" <%= Filter.ContactType=="Customers"||Filter.ContactType=="Customer"?"selected":"" %>>Customers</option>
            <option value="Vendors" <%= Filter.ContactType=="Vendors"||Filter.ContactType=="Vendor"?"selected":"" %>>Vendors</option>
            <option value="Employees" <%= Filter.ContactType=="Employees"||Filter.ContactType=="Employee"?"selected":"" %>>Employees</option>
            <option value="NoBusinessLink" <%= ContactHubService.IsNoBusinessLink(Filter.ContactType)?"selected":"" %>>No business link</option>
          </select>
        </div>
        <% } %>
        <div class="form-group">
          <label>Verification Status</label>
          <select id="hubVerify" class="form-control">
            <option value="All" <%= Filter.Verification=="All"?"selected":"" %>>All</option>
            <option value="ToVerify" <%= Filter.Verification=="ToVerify"?"selected":"" %>>Pending</option>
          </select>
        </div>
        <div class="form-group">
          <label>Approval / Access</label>
          <select id="hubAccess" class="form-control">
            <option value="All" <%= Filter.AccessStatus=="All"?"selected":"" %>>All</option>
            <option value="Restricted" <%= Filter.AccessStatus=="Restricted"?"selected":"" %>>Restricted</option>
            <option value="Quarantined" <%= Filter.AccessStatus=="Quarantined"?"selected":"" %>>Quarantined</option>
          </select>
        </div>
        <div class="form-group">
          <label>Record Type</label>
          <select id="hubRtype" class="form-control">
            <option value="All" <%= Filter.RecordType=="All"?"selected":"" %>>All</option>
            <option value="People" <%= Filter.RecordType=="People"?"selected":"" %>>People</option>
            <option value="Organisation" <%= Filter.RecordType=="Organisation"?"selected":"" %>>Organisation</option>
          </select>
        </div>
        <div class="form-group">
          <label>Position</label>
          <select id="hubPosition" class="form-control">
            <option value="">All</option>
            <% foreach (var p in Result.Positions) { %>
            <option value="<%= H(p) %>" <%= Filter.Position==p?"selected":"" %>><%= H(p) %></option>
            <% } %>
          </select>
        </div>
        <div class="form-group">
          <label>Branch</label>
          <select id="hubBranch" class="form-control">
            <option value="">All</option>
            <% foreach (var b in Result.Branches) { %>
            <option value="<%= H(b) %>" <%= Filter.Branch==b?"selected":"" %>><%= H(b) %></option>
            <% } %>
          </select>
        </div>
        <div class="form-group full-width">
          <label>Search</label>
          <input type="text" id="hubQ" class="form-control" value="<%= H(Filter.Search) %>" placeholder="Name, code, account, employee number, phone, email..." />
        </div>
      </div>
      <div class="hub-filter-actions">
        <button type="button" class="btn btn-primary" onclick="applyHubFilters('<%= ListPath %>')">Apply filters</button>
        <a class="btn btn-secondary" href="<%= ListPath %>">Reset</a>
        <span class="text-muted"><%= Result.Total %> record<%= Result.Total==1?"":"s" %></span>
      </div>
    </div>

    <div class="table-responsive">
      <table class="data-table">
        <thead>
          <tr>
            <th><a href="<%= SortUrl("code") %>">Code</a></th>
            <th><a href="<%= SortUrl("name") %>">Name</a></th>
            <th>QR</th>
            <th><a href="<%= SortUrl("type") %>">Type</a></th>
            <th><a href="<%= SortUrl("account") %>">Account / Employee</a></th>
            <th><a href="<%= SortUrl("phone") %>">Phone</a></th>
            <th>Email</th>
            <th>Position</th>
            <th>Branch</th>
            <th><a href="<%= SortUrl("status") %>">Status</a></th>
            <th>Workflow</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
        <% foreach (var c in Result.Items) { %>
          <tr>
            <td><%= H(c.ContactCode) %></td>
            <td><%= H(c.Name) %></td>
            <td class="hub-qr-cell">
              <a href="<%= ContactHubService.QrViewUrl(c.ContactID, HubView) %>" title="View QR code">
                <img class="hub-qr-thumb" alt="QR for <%= H(c.Name) %>" width="48" height="48"
                     src="<%= ContactHubService.QrPngUrl(c.ContactID, ContactHubService.QrStamp(c), 3, HubView) %>" />
              </a>
            </td>
            <td><%= H(c.ContactType) %></td>
            <td><%= H(string.IsNullOrEmpty(c.LinkedMaster) ? c.EmployeeNumber : c.LinkedMaster) %></td>
            <td><%= H(string.IsNullOrEmpty(c.Mobile) ? c.Phone : c.Mobile) %></td>
            <td><%= H(c.Email) %></td>
            <td><%= H(c.Position) %></td>
            <td><%= H(c.Branch) %></td>
            <td>
              <%= H(c.ContactStatus) %>
              <span class="<%= ContactVerification.BadgeClass(c.VerificationStatus) %>"><%= H(ContactVerification.Display(c.VerificationStatus)) %></span>
              <% if (c.AccessStatus=="Restricted" || c.AccessStatus=="Quarantined") { %><span class="badge badge-rejected"><%= H(c.AccessStatus) %></span><% } %>
            </td>
            <td><span class="<%= ContactWorkflowStatuses.BadgeClass(c.WorkflowStatus) %>"><%= H(ContactWorkflowStatuses.Display(c.WorkflowStatus)) %></span></td>
            <td class="hub-row-actions">
              <a href="/ContactMaster.aspx?editId=<%= c.ContactID %>">Open</a>
              <% if (CanWrite && HubView == "All" && c.ContactStatus == "Active") { %>
              <button type="submit" onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= c.ContactID %>';return confirm('Deactivate contact?');">X</button>
              <% } %>
              <button type="button" class="btn btn-secondary" style="padding:2px 8px;font-size:.75rem;"
                      data-vcf="<%= ExportUrl(c.ContactID) %>"
                      data-qr="<%= ContactHubService.QrPngUrl(c.ContactID, ContactHubService.QrStamp(c), 6, HubView) %>"
                      data-view="<%= ContactHubService.QrViewUrl(c.ContactID, HubView) %>"
                      data-download="<%= ContactHubService.QrDownloadUrl(c.ContactID, ContactHubService.QrStamp(c), HubView) %>"
                      data-text="<%= B64(ContactHubService.ToPlainText(c)) %>"
                      data-name="<%= H(c.Name) %>"
                      onclick="openContactCard(this)">Card</button>
            </td>
          </tr>
        <% } %>
        <% if (Result.Items.Count == 0) { %><tr><td colspan="12">No contacts match the selected filters.</td></tr><% } %>
        </tbody>
      </table>
    </div>

    <% if (Result.PageCount > 1) { %>
    <div class="hub-pager">
      <% if (Result.Page > 1) { %><a href="<%= PageUrl(Result.Page-1) %>">&laquo; Prev</a><% } %>
      <span>Page <%= Result.Page %> of <%= Result.PageCount %></span>
      <% if (Result.Page < Result.PageCount) { %><a href="<%= PageUrl(Result.Page+1) %>">Next &raquo;</a><% } %>
    </div>
    <% } %>
  </div>
</div>

<div id="contactCardModal" class="notif-popup-overlay" style="display:none;" role="dialog" aria-modal="true">
  <div class="notif-popup-card" style="max-width:420px;">
    <div class="notif-popup-header">
      <h3>Contact card</h3>
      <button type="button" class="notif-popup-close" onclick="closeContactCard()" aria-label="Close">&times;</button>
    </div>
    <div class="notif-popup-body" style="text-align:center;">
      <a id="contactQrViewLink" href="#">
        <img id="contactQrImg" class="hub-qr-thumb" alt="Contact QR code" width="180" height="180" style="width:180px;height:180px;" />
      </a>
      <p class="text-muted" style="font-size:.8rem;margin:.75rem 0;">Scan with a phone to save this contact. The QR code always uses the latest saved name, mobile, email, company, and address.</p>
      <pre id="contactCardText" style="text-align:left;white-space:pre-wrap;font-size:.8rem;background:#f8fafc;padding:.75rem;border-radius:8px;max-height:160px;overflow:auto;"></pre>
    </div>
    <div class="notif-popup-footer">
      <a id="contactQrDownloadLink" class="btn btn-primary" href="#">Download QR</a>
      <a id="contactVcfLink" class="btn btn-secondary" href="#">Download vCard</a>
      <button type="button" class="btn btn-secondary" id="btnShareContactCard">Share</button>
      <button type="button" class="btn btn-secondary" onclick="copyContactText()">Copy as text</button>
      <button type="button" class="btn btn-secondary" onclick="closeContactCard()">Close</button>
    </div>
  </div>
</div>
<script>
function applyHubFilters(path) {
  var p = new URLSearchParams();
  var ctype = document.getElementById('hubCtype');
  if (ctype && ctype.value && ctype.value !== 'All') p.set('ctype', ctype.value);
  var v = document.getElementById('hubVerify');
  if (v && v.value !== 'All') p.set('verify', v.value);
  var a = document.getElementById('hubAccess');
  if (a && a.value !== 'All') p.set('access', a.value);
  var r = document.getElementById('hubRtype');
  if (r && r.value !== 'All') p.set('rtype', r.value);
  var pos = document.getElementById('hubPosition');
  if (pos && pos.value) p.set('position', pos.value);
  var br = document.getElementById('hubBranch');
  if (br && br.value) p.set('branch', br.value);
  var q = document.getElementById('hubQ');
  if (q && q.value.trim()) p.set('q', q.value.trim());
  p.set('page', '1');
  window.location = path + '?' + p.toString();
}
function decodeB64(val) {
  try { return decodeURIComponent(escape(atob(val || ''))); } catch (e) { return ''; }
}
function openContactCard(btn) {
  var modal = document.getElementById('contactCardModal');
  var text = decodeB64(btn.getAttribute('data-text'));
  var qr = btn.getAttribute('data-qr') || '';
  var view = btn.getAttribute('data-view') || qr;
  document.getElementById('contactCardText').textContent = text;
  document.getElementById('contactVcfLink').href = btn.getAttribute('data-vcf') || '#';
  document.getElementById('contactQrDownloadLink').href = btn.getAttribute('data-download') || '#';
  document.getElementById('contactQrViewLink').href = view;
  var img = document.getElementById('contactQrImg');
  if (img) { img.src = qr; img.alt = 'QR for ' + (btn.getAttribute('data-name') || 'contact'); }
  modal.setAttribute('data-share-text', text);
  modal.setAttribute('data-share-name', btn.getAttribute('data-name') || 'Contact');
  modal.setAttribute('data-share-url', view);
  modal.style.display = 'flex';
}
function closeContactCard() {
  var el = document.getElementById('contactCardModal');
  if (el) el.style.display = 'none';
}
function copyContactText() {
  var t = document.getElementById('contactCardText');
  var val = t ? t.textContent : '';
  if (navigator.clipboard && navigator.clipboard.writeText) {
    navigator.clipboard.writeText(val);
  } else {
    var ta = document.createElement('textarea');
    ta.value = val; document.body.appendChild(ta); ta.select(); document.execCommand('copy'); document.body.removeChild(ta);
  }
}
var shareBtn = document.getElementById('btnShareContactCard');
if (shareBtn) shareBtn.addEventListener('click', function () {
  var modal = document.getElementById('contactCardModal');
  var title = modal.getAttribute('data-share-name') || 'Contact';
  var text = modal.getAttribute('data-share-text') || '';
  var url = modal.getAttribute('data-share-url') || window.location.href;
  if (navigator.share) {
    navigator.share({ title: title, text: text, url: url }).catch(function () {});
    return;
  }
  if (navigator.clipboard && navigator.clipboard.writeText) {
    navigator.clipboard.writeText(text + '\n' + url);
    alert('Contact details copied. You can paste them to share.');
    return;
  }
  copyContactText();
});
</script>
