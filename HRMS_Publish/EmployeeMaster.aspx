<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="EmployeeMaster.aspx.cs" Inherits="HRMS.EmployeeMasterPage" ValidateRequest="false" %>
<asp:Content ID="Head" ContentPlaceHolderID="head" runat="server">
<style>
.emp-list-header{display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:.75rem;}
.emp-list-header .right-controls{display:flex;align-items:center;gap:.6rem;}
.stat-chips{display:flex;gap:.5rem;margin-bottom:.75rem;flex-wrap:wrap;}
.stat-chip{display:inline-flex;align-items:center;gap:.3rem;padding:4px 12px;border-radius:20px;font-size:.78rem;font-weight:600;}
.chip-total{background:rgba(46,49,146,.12);color:var(--gb-blue);}
.chip-active{background:#dcfce7;color:#166534;}
.chip-inactive{background:rgba(227,30,36,.12);color:var(--gb-red-dark);}
.form-breadcrumb{display:flex;align-items:center;gap:.75rem;margin-bottom:1rem;padding:.5rem .75rem;background:#f9fafb;border:1px solid #e5e7eb;border-radius:6px;}
.form-breadcrumb .crumb-sep{color:var(--text-muted);}
.form-breadcrumb .crumb-current{font-weight:600;color:var(--gb-blue);}
.profile-locked{background:#f3f4f6 !important;cursor:not-allowed;}
#certificateTable input[type="file"],#documentTable input[type="file"]{font-size:.75rem;max-width:130px;}
.excel-import-row{display:flex;flex-wrap:wrap;align-items:flex-end;gap:.75rem;}
.excel-file-group{margin:0;min-width:240px;flex:1;}
</style>
</asp:Content>

<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
<% if (!string.IsNullOrEmpty(AlertMessage)) { %><div class="alert alert-<%= AlertType %>" id="alertBox"><%= Server.HtmlEncode(AlertMessage) %></div><% } %>
<div class="alert alert-error" id="clientNotice" style="display:none;"></div>
<input type="hidden" name="__handler" id="__handler" value="Save" />
<input type="hidden" name="deleteId" id="deleteId" value="0" />

<% if (!ShowForm) { %>
<div class="stat-chips">
    <span class="stat-chip chip-total">Total: <%= Employees.Count %></span>
    <span class="stat-chip chip-active">Active: <%= ActiveEmployeeCount %></span>
    <span class="stat-chip chip-inactive">Inactive: <%= InactiveEmployeeCount %></span>
</div>

<% if (HasFullEmployeeAccess) { %>
<div class="card excel-import-card" style="margin-bottom:1rem;">
    <div class="card-body">
        <div class="excel-import-row">
            <div class="form-group excel-file-group">
                <label>Import from Excel</label>
                <input type="file" name="excelFile" class="form-control" accept=".xlsx,.xls" />
            </div>
            <button type="submit" class="btn btn-primary" onclick="document.getElementById('__handler').value='ImportExcel';">Import Excel</button>
            <button type="submit" class="btn btn-secondary" onclick="document.getElementById('__handler').value='ExportExcel';">Export Excel</button>
        </div>
        <small style="color:#6b7280;display:block;margin-top:.5rem;font-size:.78rem;">Export downloads current data as a template. Re-import to add or update records.</small>
    </div>
</div>
<% } %>

<div class="card">
    <div class="card-header emp-list-header">
        <h2>Employee List</h2>
        <div class="right-controls">
            <input type="text" id="txtSearch" class="form-control" style="width:220px" placeholder="Search name / code / dept…" onkeyup="searchTable(this.value)" />
            <% if (HasFullEmployeeAccess) { %>
            <a href="/EmployeeMaster.aspx?newEmployee=1" class="btn btn-primary">+ New Employee</a>
            <% } %>
        </div>
    </div>
    <div class="card-body table-responsive">
        <table class="data-table" id="empTable">
            <thead class="grid-header">
                <tr>
                    <th>#</th><th>Emp Code</th><th>Full Name</th><th>Legal Entity</th><th>Department</th>
                    <th>Employment Type</th><th>Employment Status</th><th>Designation</th>
                    <th>Mobile</th><th>Email</th><th>Joined</th><th>Status</th><th>Actions</th>
                </tr>
            </thead>
            <tbody>
            <% if (Employees.Count == 0) { %>
                <tr><td colspan="13" class="empty-cell" style="text-align:center;padding:2rem;color:#6b7280;">
                    No employees found.
                    <% if (HasFullEmployeeAccess) { %><br /><a href="/EmployeeMaster.aspx?newEmployee=1" class="btn btn-primary" style="margin-top:.75rem;">+ Add First Employee</a><% } %>
                </td></tr>
            <% } else {
                int rowNum = 0;
                foreach (var emp in Employees) {
                    rowNum++;
            %>
                <tr class="<%= rowNum % 2 == 0 ? "grid-alt-row" : "grid-row" %>">
                    <td><%= rowNum %></td>
                    <td><strong><%= Server.HtmlEncode(emp.EmployeeCode) %></strong></td>
                    <td><%= Server.HtmlEncode(emp.FullName) %></td>
                    <td><%= Server.HtmlEncode(emp.LegalEntityName) %></td>
                    <td><%= Server.HtmlEncode(emp.DepartmentName) %></td>
                    <td><% if (!string.IsNullOrEmpty(emp.EmploymentType)) { %><span class="badge badge-warning"><%= Server.HtmlEncode(emp.EmploymentType) %></span><% } %></td>
                    <td><% if (!string.IsNullOrEmpty(emp.EmploymentStatus)) { %><span class="badge badge-info"><%= Server.HtmlEncode(emp.EmploymentStatus) %></span><% } %></td>
                    <td><%= Server.HtmlEncode(emp.Designation) %></td>
                    <td><%= Server.HtmlEncode(emp.Phone) %></td>
                    <td><%= Server.HtmlEncode(emp.Email) %></td>
                    <td><%= emp.DateOfJoining.HasValue ? emp.DateOfJoining.Value.ToString("dd MMM yyyy") : "" %></td>
                    <td><span class="badge <%= emp.Status == "Active" ? "badge-success" : "badge-danger" %>"><%= Server.HtmlEncode(emp.Status) %></span></td>
                    <td class="actions-col" style="white-space:nowrap;">
                        <a href="/EmployeeCard.aspx?id=<%= emp.EmployeeID %>" class="btn btn-secondary" style="padding:3px 12px;font-size:.8rem;" title="View ID Card">Card</a>
                        <a href="/EmployeeMaster.aspx?editId=<%= emp.EmployeeID %>" class="btn btn-secondary" style="padding:3px 12px;font-size:.8rem;"><%= HasFullEmployeeAccess ? "Edit" : "View" %></a>
                        <% if (HasFullEmployeeAccess) { %>
                        <button type="submit" class="btn btn-danger" style="padding:3px 10px;font-size:.8rem;"
                            onclick="document.getElementById('__handler').value='Delete';document.getElementById('deleteId').value='<%= emp.EmployeeID %>';return confirm('Delete this employee? This cannot be undone.');">Delete</button>
                        <% } %>
                    </td>
                </tr>
            <% } } %>
            </tbody>
        </table>
    </div>
    <div class="card-footer"><span class="record-count">Total Records: <%= Employees.Count %></span></div>
</div>
<% } else { %>

<div class="form-breadcrumb">
    <% if (HasFullEmployeeAccess) { %>
    <a href="/EmployeeMaster.aspx" class="btn btn-secondary" style="padding:4px 14px;">&#8592; Back to List</a>
    <span class="crumb-sep">/</span>
    <% } %>
    <span class="crumb-current"><%= IsProfileOnlyMode ? "My Profile" : (EditMode ? "Edit Employee" : "New Employee") %></span>
    <% if (EditMode && !string.IsNullOrEmpty(Input.EmployeeCode)) { %>
    <span class="crumb-sep"></span>
    <span style="color:#4b5563;font-size:.9rem;"><%= Server.HtmlEncode(Input.EmployeeCode) %> &nbsp; <%= Server.HtmlEncode(Input.FirstName) %> <%= Server.HtmlEncode(Input.LastName) %></span>
    <% } %>
</div>

<div class="card">
    <div class="card-header">
        <h2><%= IsProfileOnlyMode ? "My Profile" : (EditMode ? "Edit Employee Details" : "Add New Employee") %></h2>
    </div>
    <% if (IsProfileOnlyMode) { %>
    <div class="card-body" style="padding-bottom:0;">
        <div class="alert alert-info" style="margin:0;">
            You are viewing your own employee profile. Personal and contact details can be updated; employment and organization fields are managed by HR.
        </div>
    </div>
    <% } %>

    <div class="card-body" id="empForm">
        <input type="hidden" name="EmployeeID" value="<%= Input.EmployeeID %>" />
        <input type="hidden" name="EditMode" value="<%= EditMode ? "true" : "false" %>" />
        <input type="hidden" name="PhotoPath" id="hdnPhotoPath" value="<%= Server.HtmlEncode(Input.PhotoPath) %>" />
        <input type="hidden" name="ContactsJson" id="ContactsJson" />
        <input type="hidden" name="AddressesJson" id="AddressesJson" />
        <input type="hidden" name="FamilyMembersJson" id="FamilyMembersJson" />
        <input type="hidden" name="BanksJson" id="BanksJson" />
        <input type="hidden" name="EducationJson" id="EducationJson" />
        <input type="hidden" name="CertificatesJson" id="CertificatesJson" />
        <input type="hidden" name="DocumentsJson" id="DocumentsJson" />

        <div class="form-grid">
            <div class="form-group full-width"><h3 class="form-section-title">Primary Identifier</h3></div>

            <div class="form-group full-width emp-profile-photo-group">
                <label>Profile Picture</label>
                <div class="emp-profile-photo">
                    <div class="emp-profile-photo__preview" id="profilePhotoPreview">
                        <% if (!string.IsNullOrWhiteSpace(Input.PhotoPath)) { %>
                        <img src="<%= Server.HtmlEncode(Input.PhotoPath) %>" alt="Profile photo" id="imgProfilePhoto" />
                        <% } else {
                            var initials = "";
                            if (!string.IsNullOrEmpty(Input.FirstName)) initials += char.ToUpperInvariant(Input.FirstName.Trim()[0]);
                            if (!string.IsNullOrEmpty(Input.LastName)) initials += char.ToUpperInvariant(Input.LastName.Trim()[0]);
                        %>
                        <span class="emp-profile-photo__initials" id="profilePhotoInitials"><%= Server.HtmlEncode(initials) %></span>
                        <img src="" alt="Profile photo" id="imgProfilePhoto" style="display:none;" />
                        <% } %>
                    </div>
                    <div class="emp-profile-photo__controls">
                        <input type="file" name="ProfilePhoto" id="fileProfilePhoto" class="form-control" accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp" />
                        <small class="text-muted">JPG, PNG, or WEBP. Max 5 MB.</small>
                        <% if (!string.IsNullOrWhiteSpace(Input.PhotoPath)) { %>
                        <label class="emp-profile-photo__remove"><input type="checkbox" name="RemovePhoto" id="chkRemovePhoto" value="true" /> Remove current photo</label>
                        <% } %>
                        <% if (EditMode && Input.EmployeeID > 0) { %>
                        <a href="/EmployeeCard.aspx?id=<%= Input.EmployeeID %>" class="btn btn-secondary" style="margin-top:.5rem;align-self:flex-start;">Preview ID Card</a>
                        <% } %>
                    </div>
                </div>
            </div>

            <div class="form-group">
                <label>Employee ID <span class="required">*</span></label>
                <input type="text" name="EmployeeCode" id="txtEmpCode" class="form-control" value="<%= Server.HtmlEncode(Input.EmployeeCode) %>" maxlength="20" />
            </div>
            <div class="form-group">
                <label>Employee Status <span class="required">*</span></label>
                <select name="Status" id="ddlStatus" class="form-control">
                    <option value="Active" <%= Input.Status == "Active" ? "selected" : "" %>>Active</option>
                    <option value="Inactive" <%= Input.Status == "Inactive" ? "selected" : "" %>>Inactive</option>
                </select>
            </div>
            <div class="form-group">
                <label>First Name <span class="required">*</span></label>
                <input type="text" name="FirstName" id="txtFirstName" class="form-control" value="<%= Server.HtmlEncode(Input.FirstName) %>" maxlength="100" />
            </div>
            <div class="form-group">
                <label>Last Name <span class="required">*</span></label>
                <input type="text" name="LastName" id="txtLastName" class="form-control" value="<%= Server.HtmlEncode(Input.LastName) %>" maxlength="100" />
            </div>
            <div class="form-group">
                <label>Father's / Husband's Name</label>
                <input type="text" name="FathersHusbandsName" id="txtFatherHusbandName" class="form-control" value="<%= Server.HtmlEncode(Input.FathersHusbandsName) %>" maxlength="150" />
            </div>
            <div class="form-group">
                <label>Display Name</label>
                <input type="text" name="DisplayName" id="txtDisplayName" class="form-control" value="<%= Server.HtmlEncode(Input.DisplayName) %>" maxlength="200" />
            </div>
            <div class="form-group">
                <label>National ID Number</label>
                <input type="text" name="NationalIDNumber" id="txtNationalID" class="form-control" value="<%= Server.HtmlEncode(Input.NationalIDNumber) %>" maxlength="50" />
            </div>
            <div class="form-group">
                <label>Date of Birth</label>
                <input type="date" name="DateOfBirth" id="txtDOB" class="form-control" value="<%= Input.DateOfBirth %>" onchange="updateAgeField();" />
            </div>
            <div class="form-group">
                <label>Age</label>
                <input type="text" id="txtAge" class="form-control" readonly value="<%= Server.HtmlEncode(Input.AgeDisplay) %>" style="background:#f9fafb;" />
            </div>
            <div class="form-group">
                <label>Gender <span class="required">*</span></label>
                <select name="GenderID" id="ddlGender" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in Genders) { %>
                    <option value="<%= item.Id %>" <%= Input.GenderID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Language</label>
                <select name="LanguageID" id="ddlLanguage" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in Languages) { %>
                    <option value="<%= item.Id %>" <%= Input.LanguageID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>

            <div class="form-group full-width"><h3 class="form-section-title">Demographic Information</h3></div>
            <div class="form-group">
                <label>Nationality</label>
                <select name="NationalityID" id="ddlNationality" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in Nationalities) { %>
                    <option value="<%= item.Id %>" <%= Input.NationalityID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Domicile</label>
                <input type="text" name="Domicile" id="txtDomicile" class="form-control" value="<%= Server.HtmlEncode(Input.Domicile) %>" maxlength="150" />
            </div>
            <div class="form-group">
                <label>Religion</label>
                <select name="ReligionID" id="ddlReligion" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in Religions) { %>
                    <option value="<%= item.Id %>" <%= Input.ReligionID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Marital Status</label>
                <select name="MaritalStatus" id="ddlMaritalStatus" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var ms in new[] { "Single","Married","Divorced","Widowed","Separated" }) { %>
                    <option value="<%= ms %>" <%= Input.MaritalStatus == ms ? "selected" : "" %>><%= ms %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Blood Group</label>
                <select name="BloodGroupID" id="ddlBloodGroup" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in BloodGroups) { %>
                    <option value="<%= item.Id %>" <%= Input.BloodGroupID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>

            <div class="form-group full-width"><h3 class="form-section-title">Organization Assignment</h3></div>
            <div class="form-group">
                <label>Legal Entity</label>
                <select name="LegalEntityID" id="ddlLegalEntity" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in LegalEntities) { %>
                    <option value="<%= item.Id %>" <%= Input.LegalEntityID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Business Unit</label>
                <select name="BusinessUnitID" id="ddlBusinessUnit" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in BusinessUnits) { %>
                    <option value="<%= item.Id %>" <%= Input.BusinessUnitID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Division</label>
                <select name="DivisionID" id="ddlDivision" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in Divisions) { %>
                    <option value="<%= item.Id %>" <%= Input.DivisionID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Department <span class="required">*</span></label>
                <select name="DepartmentID" id="ddlDepartment" class="form-control">
                    <option value="">-- Select Department --</option>
                    <% foreach (var dept in Departments) { %>
                    <option value="<%= dept.DepartmentID %>" <%= Input.DepartmentID == dept.DepartmentID ? "selected" : "" %>><%= Server.HtmlEncode(dept.DepartmentName) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Designation <span class="required">*</span></label>
                <input type="text" name="Designation" id="txtDesignation" class="form-control" value="<%= Server.HtmlEncode(Input.Designation) %>" maxlength="100" />
            </div>
            <div class="form-group">
                <label>Section / Team</label>
                <select name="SalesTeamID" id="ddlSalesTeam" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in SalesTeams) { %>
                    <option value="<%= item.Id %>" <%= Input.SalesTeamID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Cost Center</label>
                <select name="CostCenterID" id="ddlCostCenter" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in CostCenters) { %>
                    <option value="<%= item.Id %>" <%= Input.CostCenterID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Region</label>
                <select name="RegionID" id="ddlRegion" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in Regions) { %>
                    <option value="<%= item.Id %>" <%= Input.RegionID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Worker Location</label>
                <select name="WorkerLocationID" id="ddlWorkerLocation" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in WorkerLocations) { %>
                    <option value="<%= item.Id %>" <%= Input.WorkerLocationID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Location</label>
                <select name="LocationID" id="ddlLocation" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in Locations) { %>
                    <option value="<%= item.Id %>" <%= Input.LocationID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Extension</label>
                <select name="ExtensionID" id="ddlExtension" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in Extensions) { %>
                    <option value="<%= item.Id %>" <%= Input.ExtensionID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Employment Status</label>
                <select name="EmploymentStatusID" id="ddlEmploymentStatus" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in EmploymentStatuses) { %>
                    <option value="<%= item.Id %>" <%= Input.EmploymentStatusID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Employment Type</label>
                <select name="EmploymentTypeID" id="ddlEmploymentType" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in EmploymentTypes) { %>
                    <option value="<%= item.Id %>" <%= Input.EmploymentTypeID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Job</label>
                <select name="JobID" id="ddlJob" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in Jobs) { %>
                    <option value="<%= item.Id %>" <%= Input.JobID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>City</label>
                <select name="CityID" id="ddlCity" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in Cities) { %>
                    <option value="<%= item.Id %>" <%= Input.CityID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Province</label>
                <select name="ProvinceID" id="ddlProvince" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in Provinces) { %>
                    <option value="<%= item.Id %>" <%= Input.ProvinceID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Sales Group</label>
                <select name="SalesGroupID" id="ddlSalesGroup" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in SalesGroups) { %>
                    <option value="<%= item.Id %>" <%= Input.SalesGroupID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Grade</label>
                <select name="GradeID" id="ddlGrade" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in Grades) { %>
                    <option value="<%= item.Id %>" <%= Input.GradeID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Basic Salary <span class="required">*</span></label>
                <input type="number" name="BasicSalary" id="txtSalary" class="form-control" value="<%= Server.HtmlEncode(Input.BasicSalary) %>" min="0" step="0.01" />
            </div>

            <div class="form-group full-width"><h3 class="form-section-title">Additional Employment</h3></div>
            <div class="form-group">
                <label>User</label>
                <select name="UserID" id="ddlUser" class="form-control">
                    <option value="">-- Select User --</option>
                    <% foreach (var user in Users) { %>
                    <option value="<%= user.Id %>" <%= Input.UserID == user.Id ? "selected" : "" %>><%= Server.HtmlEncode(user.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Temporary Responsible</label>
                <select name="TemporaryResponsibleEmployeeID" id="ddlTemporaryResponsible" class="form-control">
                    <option value="">-- Select Employee --</option>
                    <% foreach (var emp in EmployeeLookups) { %>
                    <option value="<%= emp.Id %>" <%= Input.TemporaryResponsibleEmployeeID == emp.Id ? "selected" : "" %>><%= Server.HtmlEncode(emp.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Permanent Responsible</label>
                <select name="PermanentResponsibleEmployeeID" id="ddlPermanentResponsible" class="form-control">
                    <option value="">-- Select Employee --</option>
                    <% foreach (var emp in EmployeeLookups) { %>
                    <option value="<%= emp.Id %>" <%= Input.PermanentResponsibleEmployeeID == emp.Id ? "selected" : "" %>><%= Server.HtmlEncode(emp.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Worker Category</label>
                <select name="WorkerCategoryID" id="ddlWorkerCategory" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in WorkerCategories) { %>
                    <option value="<%= item.Id %>" <%= Input.WorkerCategoryID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>
            <div class="form-group">
                <label>Workforce Segment</label>
                <select name="WorkforceSegmentID" id="ddlWorkforceSegment" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var item in WorkforceSegments) { %>
                    <option value="<%= item.Id %>" <%= Input.WorkforceSegmentID == item.Id ? "selected" : "" %>><%= Server.HtmlEncode(item.Name) %></option>
                    <% } %>
                </select>
            </div>

            <div class="form-group full-width"><h3 class="form-section-title">Worker Joining &amp; Tenure</h3></div>
            <div class="form-group">
                <label>Joining Date <span class="required">*</span></label>
                <input type="date" name="DateOfJoining" id="txtDOJ" class="form-control" value="<%= Input.DateOfJoining %>" onchange="updateTenureFields(); updateProbationEnd();" />
            </div>
            <div class="form-group">
                <label>Employment Start Date</label>
                <input type="date" name="EmploymentStartDate" id="txtEmploymentStart" class="form-control" value="<%= Input.EmploymentStartDate %>" onchange="updateTenureFields(); updateProbationEnd();" />
            </div>
            <div class="form-group">
                <label>Probation Period (Days)</label>
                <input type="number" name="ProbationPeriodDays" id="txtProbationDays" class="form-control" value="<%= Server.HtmlEncode(Input.ProbationPeriodDays) %>" min="0" step="1" onchange="updateProbationEnd();" oninput="updateProbationEnd();" />
            </div>
            <div class="form-group">
                <label>Probation End Date</label>
                <input type="date" name="ProbationEndDate" id="txtProbationEnd" class="form-control" value="<%= Input.ProbationEndDate %>" />
                <small style="color:#6b7280;font-size:.78rem;">Auto-calculated from start date + probation days</small>
            </div>
            <div class="form-group">
                <label>Confirmation Date</label>
                <input type="date" name="ConfirmationDate" id="txtConfirmationDate" class="form-control" value="<%= Input.ConfirmationDate %>" />
            </div>
            <div class="form-group">
                <label>Total Tenure</label>
                <input type="text" id="txtTotalTenure" class="form-control" readonly value="<%= Server.HtmlEncode(Input.TotalTenureDisplay) %>" style="background:#f9fafb;" />
            </div>
            <div class="form-group">
                <label>Current Role Tenure</label>
                <input type="text" id="txtCurrentRoleTenure" class="form-control" readonly value="<%= Server.HtmlEncode(Input.CurrentRoleTenureDisplay) %>" style="background:#f9fafb;" />
            </div>

            <div class="form-group full-width"><h3 class="form-section-title">Compensation &amp; Benefits</h3></div>
            <div class="form-group">
                <label>Benefit Entitlement</label>
                <select name="BenefitEntitlementID" id="ddlBenefitEntitlement" class="form-control">
                    <option value="">-- Select --</option>
                    <% foreach (var benefit in BenefitEntitlements) { %>
                    <option value="<%= benefit.Id %>" <%= Input.BenefitEntitlementID == benefit.Id ? "selected" : "" %>><%= Server.HtmlEncode(benefit.Name) %></option>
                    <% } %>
                </select>
            </div>

            <div class="form-group full-width mt-4">
                <div class="profile-tabs">
                    <div class="profile-tab-buttons">
                        <button type="button" class="profile-tab-btn active" data-tab-target="contactTab" onclick="switchProfileTab('contactTab', this)">Employee Contact</button>
                        <button type="button" class="profile-tab-btn" data-tab-target="addressTab" onclick="switchProfileTab('addressTab', this)">Address</button>
                        <button type="button" class="profile-tab-btn" data-tab-target="familyTab" onclick="switchProfileTab('familyTab', this)">Family Member</button>
                        <button type="button" class="profile-tab-btn" data-tab-target="educationTab" onclick="switchProfileTab('educationTab', this)">Education</button>
                        <button type="button" class="profile-tab-btn" data-tab-target="certificateTab" onclick="switchProfileTab('certificateTab', this)">Certificate</button>
                        <button type="button" class="profile-tab-btn" data-tab-target="documentTab" onclick="switchProfileTab('documentTab', this)">Documents</button>
                        <button type="button" class="profile-tab-btn" data-tab-target="bankTab" onclick="switchProfileTab('bankTab', this)">Bank Information</button>
                    </div>
                    <div class="profile-tab-panels">
                        <div class="profile-tab-panel active" id="contactTab">
                            <div class="card">
                                <div class="card-header space-between">
                                    <h2>Employee Contact (Multiple)</h2>
                                    <div>
                                        <button type="button" class="btn btn-secondary" onclick="addContactRow()">Add Contact</button>
                                        <button type="button" class="btn btn-primary" onclick="submitProfileSection('contacts')">Save Contact</button>
                                    </div>
                                </div>
                                <div class="card-body table-responsive">
                                    <table class="data-table" id="contactTable"><thead class="grid-header"><tr><th>Type</th><th>Name</th><th>Relationship</th><th>Value</th><th>Primary</th><th>Action</th></tr></thead><tbody></tbody></table>
                                </div>
                            </div>
                        </div>
                        <div class="profile-tab-panel" id="addressTab">
                            <div class="card">
                                <div class="card-header space-between">
                                    <h2>Address Information (Multiple)</h2>
                                    <div>
                                        <button type="button" class="btn btn-secondary" onclick="addAddressRow()">Add Address</button>
                                        <button type="button" class="btn btn-primary" onclick="submitProfileSection('addresses')">Save Address</button>
                                    </div>
                                </div>
                                <div class="card-body table-responsive">
                                    <table class="data-table" id="addressTable"><thead class="grid-header"><tr><th>Type</th><th>Address</th><th>City</th><th>Province/State</th><th>Postal Code</th><th>Primary</th><th>Action</th></tr></thead><tbody></tbody></table>
                                </div>
                            </div>
                        </div>
                        <div class="profile-tab-panel" id="familyTab">
                            <div class="card">
                                <div class="card-header space-between">
                                    <h2>Family Members (Multiple)</h2>
                                    <div>
                                        <button type="button" class="btn btn-secondary" onclick="addFamilyRow()">Add Member</button>
                                        <button type="button" class="btn btn-primary" onclick="submitProfileSection('family')">Save Family</button>
                                    </div>
                                </div>
                                <div class="card-body table-responsive">
                                    <table class="data-table" id="familyTable"><thead class="grid-header"><tr><th>Name</th><th>Relationship</th><th>Gender</th><th>Date of Birth</th><th>Contact Number</th><th>Dependent</th><th>Action</th></tr></thead><tbody></tbody></table>
                                </div>
                            </div>
                        </div>
                        <div class="profile-tab-panel" id="educationTab">
                            <div class="card">
                                <div class="card-header space-between">
                                    <h2>Education Details (Multiple)</h2>
                                    <div>
                                        <button type="button" class="btn btn-secondary" onclick="addEducationRow()">Add Education</button>
                                        <button type="button" class="btn btn-primary" onclick="submitProfileSection('education')">Save Education</button>
                                    </div>
                                </div>
                                <div class="card-body table-responsive">
                                    <table class="data-table" id="educationTable"><thead class="grid-header"><tr><th>Highest Qualification</th><th>Degree/Certificate</th><th>Specialization</th><th>Institution</th><th>Year of Passing</th><th>Grade/CGPA</th><th>Action</th></tr></thead><tbody></tbody></table>
                                </div>
                            </div>
                        </div>
                        <div class="profile-tab-panel" id="certificateTab">
                            <div class="card">
                                <div class="card-header space-between">
                                    <h2>Certificate Details (Multiple)</h2>
                                    <div>
                                        <button type="button" class="btn btn-secondary" onclick="addCertificateRow()">Add Certificate</button>
                                        <button type="button" class="btn btn-primary" onclick="submitProfileSection('certificates')">Save Certificate</button>
                                    </div>
                                </div>
                                <div class="card-body table-responsive">
                                    <table class="data-table" id="certificateTable"><thead class="grid-header"><tr><th>Certification Name</th><th>Certification Body</th><th>Certificate No</th><th>Issue Date</th><th>Expiry Date</th><th>Renewal Required</th><th>Certificate Copy</th><th>Action</th></tr></thead><tbody></tbody></table>
                                </div>
                            </div>
                        </div>
                        <div class="profile-tab-panel" id="documentTab">
                            <div class="card">
                                <div class="card-header space-between">
                                    <h2>Employee Documents (Multiple)</h2>
                                    <div>
                                        <button type="button" class="btn btn-secondary" onclick="addDocumentRow()">Add Document</button>
                                        <button type="button" class="btn btn-primary" onclick="submitProfileSection('documents')">Save Documents</button>
                                    </div>
                                </div>
                                <div class="card-body table-responsive">
                                    <table class="data-table" id="documentTable"><thead class="grid-header"><tr><th>Document Type</th><th>Document No</th><th>Issue Date</th><th>Expiry Date</th><th>Remarks</th><th>Upload / View</th><th>Verified</th><th>Action</th></tr></thead><tbody></tbody></table>
                                </div>
                            </div>
                        </div>
                        <div class="profile-tab-panel" id="bankTab">
                            <div class="card">
                                <div class="card-header space-between">
                                    <h2>Employee Bank Information (Multiple)</h2>
                                    <div>
                                        <button type="button" class="btn btn-secondary" onclick="addBankRow()">Add Bank</button>
                                        <button type="button" class="btn btn-primary" onclick="submitProfileSection('banks')">Save Bank</button>
                                    </div>
                                </div>
                                <div class="card-body table-responsive">
                                    <table class="data-table" id="bankTable"><thead class="grid-header"><tr><th>Bank</th><th>Bank Code</th><th>Location Name</th><th>Bank Group</th><th>IBAN No</th><th>Swift/BIC</th><th>Currency Code</th><th>Verification</th><th>Primary</th><th>Action</th></tr></thead><tbody></tbody></table>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="card-footer" style="display:flex;gap:.75rem;align-items:center;">
        <% if (CanEditCurrentRecord) { %>
        <button type="submit" class="btn btn-primary" onclick="return prepareAndSaveEmployee();">
            <%= IsProfileOnlyMode ? "Update My Profile" : (EditMode ? "Update Employee" : "Save Employee") %>
        </button>
        <% } %>
        <% if (HasFullEmployeeAccess) { %>
        <a href="/EmployeeMaster.aspx" class="btn btn-secondary">&#8592; Back to List</a>
        <% } %>
    </div>
</div>
<% } %>
</asp:Content>

<asp:Content ID="Scripts" ContentPlaceHolderID="scripts" runat="server">
<% if (ShowForm) { %>
<script id="initialContactsData" type="application/json"><%= ContactsJsonInit %></script>
<script id="initialAddressesData" type="application/json"><%= AddressesJsonInit %></script>
<script id="initialFamilyData" type="application/json"><%= FamilyJsonInit %></script>
<script id="initialEducationData" type="application/json"><%= EducationJsonInit %></script>
<script id="initialCertificatesData" type="application/json"><%= CertificatesJsonInit %></script>
<script id="initialDocumentsData" type="application/json"><%= DocumentsJsonInit %></script>
<script id="initialBanksData" type="application/json"><%= BanksJsonInit %></script>
<script id="bankLookupData" type="application/json"><%= BanksLookupJson %></script>
<script id="currencyLookupData" type="application/json"><%= CurrenciesLookupJson %></script>
<script id="bankGroupLookupData" type="application/json"><%= BankGroupsLookupJson %></script>
<script id="documentTypeLookupData" type="application/json"><%= DocumentTypesLookupJson %></script>
<script src="<%= ResolveUrl("~/js/field-validation.js") %>?v=1"></script>
<!-- app.js is loaded once from Site.Master (do not include again — duplicate loads re-seeded contact rows) -->
<script>
function formatTenureFromDate(dateStr) {
    if (!dateStr) return '';
    var start = new Date(dateStr + 'T00:00:00');
    var end = new Date(); end.setHours(0,0,0,0);
    if (isNaN(start.getTime()) || start > end) return '';
    var months = (end.getFullYear() - start.getFullYear()) * 12 + (end.getMonth() - start.getMonth());
    if (end.getDate() < start.getDate()) months--;
    if (months < 0) return '';
    var years = Math.floor(months / 12); months = months % 12;
    var parts = [];
    if (years > 0) parts.push(years + ' yr' + (years === 1 ? '' : 's'));
    if (months > 0) parts.push(months + ' mo' + (months === 1 ? '' : 's'));
    if (parts.length === 0) {
        var days = Math.round((end - start) / (1000 * 60 * 60 * 24));
        parts.push(days + ' day' + (days === 1 ? '' : 's'));
    }
    return parts.join(', ');
}
function updateAgeField() {
    var dob = document.getElementById('txtDOB') && document.getElementById('txtDOB').value;
    var ageEl = document.getElementById('txtAge');
    if (!ageEl) return;
    if (!dob) { ageEl.value = ''; return; }
    var birth = new Date(dob + 'T00:00:00');
    var today = new Date(); today.setHours(0,0,0,0);
    if (isNaN(birth.getTime()) || birth > today) { ageEl.value = ''; return; }
    var age = today.getFullYear() - birth.getFullYear();
    var m = today.getMonth() - birth.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) age--;
    ageEl.value = age >= 0 ? age + ' yr' + (age === 1 ? '' : 's') : '';
}
function updateTenureFields() {
    var joining = (document.getElementById('txtDOJ') && document.getElementById('txtDOJ').value) || '';
    var employmentStart = (document.getElementById('txtEmploymentStart') && document.getElementById('txtEmploymentStart').value) || '';
    var totalEl = document.getElementById('txtTotalTenure');
    var roleEl = document.getElementById('txtCurrentRoleTenure');
    if (totalEl) totalEl.value = formatTenureFromDate(joining);
    if (roleEl) roleEl.value = formatTenureFromDate(employmentStart || joining);
}
function updateProbationEnd() {
    var startEl = document.getElementById('txtEmploymentStart');
    var dojEl = document.getElementById('txtDOJ');
    var start = (startEl && startEl.value) || (dojEl && dojEl.value);
    var daysVal = document.getElementById('txtProbationDays') && document.getElementById('txtProbationDays').value;
    var endEl = document.getElementById('txtProbationEnd');
    if (!start || !daysVal || !endEl) return;
    var days = parseInt(daysVal, 10);
    if (isNaN(days) || days <= 0) return;
    var d = new Date(start + 'T00:00:00');
    d.setDate(d.getDate() + days);
    endEl.value = d.toISOString().split('T')[0];
}
function prepareAndSaveEmployee() {
    document.getElementById('__handler').value = 'Save';
    if (typeof prepareEmployeePayload === 'function') {
        if (!prepareEmployeePayload()) return false;
    }
    return true;
}
updateAgeField();
updateTenureFields();
<% if (IsProfileOnlyMode) { %>
(function lockProfileHrFields() {
    var hrFields = [
        'EmployeeCode','Status','LegalEntityID','BusinessUnitID','DivisionID','DepartmentID',
        'WorkforceSegmentID','SalesTeamID','CostCenterID','RegionID','LocationID','JobID',
        'WorkerLocationID','CityID','ProvinceID','SalesGroupID','GradeID','ExtensionID',
        'WorkerCategoryID','EmploymentTypeID','EmploymentStatusID','BenefitEntitlementID',
        'UserID','TemporaryResponsibleEmployeeID','PermanentResponsibleEmployeeID',
        'Designation','DateOfJoining','EmploymentStartDate','ProbationPeriodDays',
        'ProbationEndDate','ConfirmationDate','BasicSalary'
    ];
    hrFields.forEach(function(name) {
        document.querySelectorAll('[name="' + name + '"]').forEach(function(el) {
            el.classList.add('profile-locked');
            if (el.tagName === 'SELECT') el.disabled = true;
            else el.readOnly = true;
        });
    });
})();
<% } %>
(function initProfilePhotoPreview() {
    var fileInput = document.getElementById('fileProfilePhoto');
    var img = document.getElementById('imgProfilePhoto');
    var initials = document.getElementById('profilePhotoInitials');
    var remove = document.getElementById('chkRemovePhoto');
    if (!fileInput) return;
    function getInitials() {
        var f = (document.getElementById('txtFirstName') && document.getElementById('txtFirstName').value || '').trim();
        var l = (document.getElementById('txtLastName') && document.getElementById('txtLastName').value || '').trim();
        return ((f.charAt(0) || '') + (l.charAt(0) || '')).toUpperCase() || '?';
    }
    function showInitials() {
        if (img) { img.style.display = 'none'; img.removeAttribute('src'); }
        if (initials) { initials.style.display = 'flex'; initials.textContent = getInitials(); }
    }
    fileInput.addEventListener('change', function () {
        if (remove) remove.checked = false;
        var file = fileInput.files && fileInput.files[0];
        if (!file) return;
        if (file.size > 5 * 1024 * 1024) { alert('Profile photo must be 5 MB or smaller.'); fileInput.value = ''; return; }
        var reader = new FileReader();
        reader.onload = function (e) {
            if (!img) return;
            img.src = e.target.result; img.style.display = 'block';
            if (initials) initials.style.display = 'none';
        };
        reader.readAsDataURL(file);
    });
    if (remove) remove.addEventListener('change', function () { if (remove.checked) { fileInput.value = ''; showInitials(); } });
})();
</script>
<% } else { %>
<script>
function searchTable(q) {
    q = (q || '').toLowerCase();
    document.querySelectorAll('#empTable tbody tr').forEach(function(row) {
        row.style.display = row.innerText.toLowerCase().indexOf(q) >= 0 ? '' : 'none';
    });
}
</script>
<% } %>
</asp:Content>
