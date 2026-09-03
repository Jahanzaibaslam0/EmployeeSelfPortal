/* =========================================================
   HRMS – Employee Master  |  Client-side Logic
   ========================================================= */

'use strict';

/* ---- Live Clock ---- */
(function initClock() {
    function tick() {
        var el = document.getElementById('clock');
        if (!el) return;
        var now = new Date();
        var d   = now.toLocaleDateString('en-PK', { weekday:'short', year:'numeric', month:'short', day:'numeric' });
        var t   = now.toLocaleTimeString('en-PK');
        el.textContent = d + '  ' + t;
    }
    tick();
    setInterval(tick, 1000);
}());

/* ---- ASP.NET control ID helpers ---- */
function $id(partialId) {
    // ASP.NET may mangle control IDs; find the element whose id ends with the partial id.
    var els = document.querySelectorAll('[id$="' + partialId + '"]');
    return els.length ? els[0] : null;
}
function val(partialId) {
    var el = $id(partialId);
    return el ? el.value.trim() : '';
}
function clearError(id) {
    var el = document.getElementById(id);
    if (el) el.textContent = '';
}
function hideClientNotice() {
    var el = document.getElementById('clientNotice');
    if (!el) return;
    el.style.display = 'none';
    el.textContent = '';
}
function showClientNotice(msg) {
    var el = document.getElementById('clientNotice');
    if (!el) return;
    el.textContent = msg;
    el.style.display = 'block';
    window.scrollTo({ top: 0, behavior: 'smooth' });
}
function setError(id, msg) {
    var el = document.getElementById(id);
    if (el) el.textContent = msg;
}
function focusCtrl(partialId) {
    var el = $id(partialId);
    if (el) el.focus();
}

/* ---- Client-side Form Validation ---- */
function validateForm() {
    var valid = true;
    hideClientNotice();

    // Clear all previous errors
    ['errEmpCode','errFirstName','errLastName','errGender',
     'errDepartment','errDesignation','errDOJ','errSalary',
     'errContactRows','errAddressRows','errBankRows'].forEach(clearError);

    // Employee ID
    if (!val('txtEmpCode')) {
        setError('errEmpCode', 'Employee ID is required.');
        if (valid) { focusCtrl('txtEmpCode'); valid = false; }
    }

    // First Name
    if (!val('txtFirstName')) {
        setError('errFirstName', 'First Name is required.');
        if (valid) { focusCtrl('txtFirstName'); valid = false; }
    }

    // Last Name
    if (!val('txtLastName')) {
        setError('errLastName', 'Last Name is required.');
        if (valid) { focusCtrl('txtLastName'); valid = false; }
    }

    // Gender
    if (!val('ddlGender')) {
        setError('errGender', 'Please select Gender.');
        if (valid) { focusCtrl('ddlGender'); valid = false; }
    }

    // Department
    if (!val('ddlDepartment')) {
        setError('errDepartment', 'Please select a Department.');
        if (valid) { focusCtrl('ddlDepartment'); valid = false; }
    }

    // Designation
    if (!val('txtDesignation')) {
        setError('errDesignation', 'Designation is required.');
        if (valid) { focusCtrl('txtDesignation'); valid = false; }
    }

    // Joining Date
    if (!val('txtDOJ')) {
        setError('errDOJ', 'Joining Date is required.');
        if (valid) { focusCtrl('txtDOJ'); valid = false; }
    }

    // Salary
    var salary = val('txtSalary');
    if (!salary) {
        setError('errSalary', 'Basic Salary is required.');
        if (valid) { focusCtrl('txtSalary'); valid = false; }
    } else if (isNaN(salary) || parseFloat(salary) < 0) {
        setError('errSalary', 'Enter a valid positive salary.');
        if (valid) { focusCtrl('txtSalary'); valid = false; }
    }

    if (!valid) {
        showClientNotice('Please fix the highlighted errors before saving.');
    }

    return valid;
}

/* ---- Delete Confirmation ---- */
function confirmDelete() {
    return confirm('Are you sure you want to delete this employee record?\nThis action cannot be undone.');
}

/* ---- Client-side Grid Search / Filter ---- */
function searchTable(keyword) {
    var kw    = keyword.toLowerCase();
    var table = document.querySelector('.data-table');
    if (!table) return;

    var rows = table.querySelectorAll('tbody tr');
    var visible = 0;

    rows.forEach(function (row) {
        var text = row.textContent.toLowerCase();
        var show = text.indexOf(kw) !== -1;
        row.style.display = show ? '' : 'none';
        if (show) visible++;
    });

    // Update count label
    var lbl = document.querySelector('[id$="lblRecordCount"]');
    if (lbl) {
        lbl.textContent = keyword
            ? ('Showing ' + visible + ' of ' + rows.length + ' record(s)')
            : ('Total Records: ' + rows.length);
    }
}

/* ---- Auto-dismiss alert after 5 s ---- */
(function autoDismissAlert() {
    var alert = document.querySelector('.alert');
    if (!alert) return;
    setTimeout(function () {
        alert.style.transition = 'opacity .6s';
        alert.style.opacity    = '0';
        setTimeout(function () {
            alert.style.display = 'none';
        }, 600);
    }, 5000);
}());

document.addEventListener('DOMContentLoaded', function () {
    if (window.__hrmsProfileRowsRendered) return;
    window.__hrmsProfileRowsRendered = true;
    renderInitialRows();
    var firstTabBtn = document.querySelector('.profile-tab-btn[data-tab-target="contactTab"]');
    if (firstTabBtn) {
        switchProfileTab('contactTab', firstTabBtn);
    }
});

function readJsonScript(id) {
    var el = document.getElementById(id);
    if (!el || !el.textContent.trim()) return [];
    try {
        return JSON.parse(el.textContent);
    } catch (_e) {
        return [];
    }
}

function clearTableBody(selector) {
    var tbody = document.querySelector(selector);
    if (tbody) tbody.innerHTML = '';
}

function renderInitialRows() {
    // Only run on Employee Master profile grids
    if (!document.getElementById('contactTable')) return;

    var contacts = readJsonScript('initialContactsData');
    var addresses = readJsonScript('initialAddressesData');
    var family = readJsonScript('initialFamilyData');
    var education = readJsonScript('initialEducationData');
    var certificates = readJsonScript('initialCertificatesData');
    var documents = readJsonScript('initialDocumentsData');
    var banks = readJsonScript('initialBanksData');

    // Clear first so a second script load cannot stack duplicate rows
    clearTableBody('#contactTable tbody');
    clearTableBody('#addressTable tbody');
    clearTableBody('#familyTable tbody');
    clearTableBody('#educationTable tbody');
    clearTableBody('#certificateTable tbody');
    clearTableBody('#documentTable tbody');
    clearTableBody('#bankTable tbody');

    // Do NOT invent placeholder contact/address rows — user adds explicitly via Add buttons.
    contacts.forEach(function (c) { addContactRow(c); });
    document.querySelectorAll('#contactTable tbody tr').forEach(function (tr) {
        if (window.HrmsValidation) HrmsValidation.bindContactTypeHints(tr);
    });
    addresses.forEach(function (a) { addAddressRow(a); });
    family.forEach(function (f) { addFamilyRow(f); });
    education.forEach(function (e) { addEducationRow(e); });
    certificates.forEach(function (c) { addCertificateRow(c); });
    documents.forEach(function (d) { addDocumentRow(d); });
    banks.forEach(function (b) { addBankRow(b); });
}

function addContactRow(data) {
    data = data || {};
    var tbody = document.querySelector('#contactTable tbody');
    if (!tbody) return;

    var tr = document.createElement('tr');
    tr.setAttribute('data-record-id', data.contactID || data.ContactID || 0);
    tr.innerHTML = ''
        + '<td><select class="form-control contact-type">'
        + '  <option value="">-- Select --</option>'
        + '  <option value="PersonalEmail">Personal Email</option>'
        + '  <option value="OfficialEmail">Official Email</option>'
        + '  <option value="PersonalMobile">Personal Mobile</option>'
        + '  <option value="OfficialMobile">Official Mobile</option>'
        + '  <option value="WhatsApp">WhatsApp</option>'
        + '  <option value="Emergency">Emergency Contact</option>'
        + '  <option value="PowerBI ID">Power BI ID</option>'
        + '</select></td>'
        + '<td><input type="text" class="form-control contact-name" maxlength="100" /></td>'
        + '<td><input type="text" class="form-control contact-relationship" maxlength="50" /></td>'
        + '<td><input type="text" class="form-control contact-value" maxlength="255" /></td>'
        + '<td><input type="checkbox" class="contact-primary" /></td>'
        + '<td><button type="button" class="btn-icon btn-delete" onclick="removeRow(this)" title="Remove this contact">X</button></td>';

    tbody.appendChild(tr);
    tr.querySelector('.contact-type').value = data.contactType || data.ContactType || '';
    tr.querySelector('.contact-name').value = data.contactName || data.ContactName || '';
    tr.querySelector('.contact-relationship').value = data.relationship || data.Relationship || '';
    tr.querySelector('.contact-value').value = data.contactValue || data.ContactValue || '';
    tr.querySelector('.contact-primary').checked = !!(data.isPrimary || data.IsPrimary);
    if (window.HrmsValidation) HrmsValidation.bindContactTypeHints(tr);
}

function addAddressRow(data) {
    data = data || {};
    var tbody = document.querySelector('#addressTable tbody');
    if (!tbody) return;

    var tr = document.createElement('tr');
    tr.setAttribute('data-record-id', data.addressID || data.AddressID || 0);
    tr.innerHTML = ''
        + '<td><select class="form-control address-type">'
        + '  <option value="Current">Current</option>'
        + '  <option value="Permanent">Permanent</option>'
        + '  <option value="Temporary">Temporary</option>'
        + '  <option value="Other">Other</option>'
        + '</select></td>'
        + '<td><textarea class="form-control address-line" rows="2"></textarea></td>'
        + '<td><input type="text" class="form-control address-city" maxlength="100" /></td>'
        + '<td><input type="text" class="form-control address-province" maxlength="100" /></td>'
        + '<td><input type="text" class="form-control address-postal" maxlength="10" /></td>'
        + '<td><input type="checkbox" class="address-primary" /></td>'
        + '<td><button type="button" class="btn-icon btn-delete" onclick="removeRow(this)" title="Remove this address">X</button></td>';

    tbody.appendChild(tr);
    tr.querySelector('.address-type').value = data.addressType || data.AddressType || 'Current';
    tr.querySelector('.address-line').value = data.addressLine || data.AddressLine || '';
    tr.querySelector('.address-city').value = data.city || data.City || '';
    tr.querySelector('.address-province').value = data.provinceState || data.ProvinceState || '';
    tr.querySelector('.address-postal').value = data.postalCode || data.PostalCode || '';
    tr.querySelector('.address-primary').checked = !!(data.isPrimary || data.IsPrimary);
}

function addFamilyRow(data) {
    data = data || {};
    var tbody = document.querySelector('#familyTable tbody');
    if (!tbody) return;

    var tr = document.createElement('tr');
    tr.setAttribute('data-record-id', data.familyMemberID || data.FamilyMemberID || 0);
    tr.innerHTML = ''
        + '<td><input type="text" class="form-control family-name" maxlength="150" /></td>'
        + '<td><input type="text" class="form-control family-relationship" maxlength="50" /></td>'
        + '<td><select class="form-control family-gender"><option value="">--</option><option value="Male">Male</option><option value="Female">Female</option><option value="Other">Other</option></select></td>'
        + '<td><input type="date" class="form-control family-dob" /></td>'
        + '<td><input type="text" class="form-control family-contact" maxlength="20" data-validate="phone" data-label="Family contact number" placeholder="+92 300 1234567" /></td>'
        + '<td><input type="checkbox" class="family-dependent" /></td>'
        + '<td><button type="button" class="btn-icon btn-delete" onclick="removeRow(this)" title="Remove this member">X</button></td>';

    tbody.appendChild(tr);
    tr.querySelector('.family-name').value = data.memberName || data.MemberName || '';
    tr.querySelector('.family-relationship').value = data.relationship || data.Relationship || '';
    tr.querySelector('.family-gender').value = data.gender || data.Gender || '';
    tr.querySelector('.family-dob').value = data.dateOfBirth || data.DateOfBirth || '';
    tr.querySelector('.family-contact').value = data.contactNumber || data.ContactNumber || '';
    tr.querySelector('.family-dependent').checked = !!(data.isDependent || data.IsDependent);
    if (window.HrmsValidation) HrmsValidation.bindInput(tr.querySelector('.family-contact'));
}

function addEducationRow(data) {
    data = data || {};
    var tbody = document.querySelector('#educationTable tbody');
    if (!tbody) return;

    var tr = document.createElement('tr');
    tr.setAttribute('data-record-id', data.educationID || data.EducationID || 0);
    tr.innerHTML = ''
        + '<td><select class="form-control edu-qualification">'
        + '  <option value="">-- Select --</option>'
        + '  <option value="Matric / O-Level">Matric / O-Level</option>'
        + '  <option value="Intermediate / A-Level">Intermediate / A-Level</option>'
        + '  <option value="Diploma">Diploma</option>'
        + '  <option value="Certificate">Certificate</option>'
        + '  <option value="Bachelor">Bachelor</option>'
        + '  <option value="Master">Master</option>'
        + '  <option value="MPhil">MPhil</option>'
        + '  <option value="PhD">PhD</option>'
        + '  <option value="Other">Other</option>'
        + '</select></td>'
        + '<td><input type="text" class="form-control edu-degree" maxlength="150" /></td>'
        + '<td><input type="text" class="form-control edu-specialization" maxlength="150" /></td>'
        + '<td><input type="text" class="form-control edu-institution" maxlength="200" /></td>'
        + '<td><input type="number" class="form-control edu-year" min="1950" max="2100" /></td>'
        + '<td><input type="text" class="form-control edu-grade" maxlength="20" placeholder="e.g. 3.5 / A+" /></td>'
        + '<td><button type="button" class="btn-icon btn-delete" onclick="removeRow(this)" title="Remove this education">X</button></td>';

    tbody.appendChild(tr);
    tr.querySelector('.edu-qualification').value = data.highestQualification || data.HighestQualification || '';
    tr.querySelector('.edu-degree').value = data.degreeCertificate || data.DegreeCertificate || '';
    tr.querySelector('.edu-specialization').value = data.specialization || data.Specialization || '';
    tr.querySelector('.edu-institution').value = data.institution || data.Institution || '';
    tr.querySelector('.edu-year').value = data.yearOfPassing || data.YearOfPassing || '';
    tr.querySelector('.edu-grade').value = data.gradeCGPA || data.GradeCGPA || '';
}

function reindexCertificateRows() {
    document.querySelectorAll('#certificateTable tbody tr').forEach(function (tr, idx) {
        tr.setAttribute('data-row-index', idx);
        var fileInput = tr.querySelector('.cert-copy-file');
        if (fileInput) {
            fileInput.name = 'CertCopy_' + idx;
            fileInput.removeAttribute('form');
        }
    });
}

function addCertificateRow(data) {
    data = data || {};
    var tbody = document.querySelector('#certificateTable tbody');
    if (!tbody) return;

    var rowIndex = tbody.querySelectorAll('tr').length;
    var copyPath = data.certificateCopyPath || data.CertificateCopyPath || '';
    var copyLink = copyPath
        ? '<a class="cert-copy-link" href="' + escapeHtml(copyPath) + '" target="_blank">View</a><br/>'
        : '';

    var tr = document.createElement('tr');
    tr.setAttribute('data-row-index', rowIndex);
    tr.setAttribute('data-record-id', data.certificateID || data.CertificateID || 0);
    tr.innerHTML = ''
        + '<td><input type="text" class="form-control cert-name" maxlength="200" /></td>'
        + '<td><input type="text" class="form-control cert-body" maxlength="200" /></td>'
        + '<td><input type="text" class="form-control cert-number" maxlength="100" /></td>'
        + '<td><input type="date" class="form-control cert-issue-date" /></td>'
        + '<td><input type="date" class="form-control cert-expiry-date" /></td>'
        + '<td style="text-align:center;"><input type="checkbox" class="cert-renewal" /></td>'
        + '<td>' + copyLink
        + '<input type="file" class="cert-copy-file" name="CertCopy_' + rowIndex + '" accept=".pdf,.jpg,.jpeg,.png,.doc,.docx" />'
        + '<input type="hidden" class="cert-copy-path" /></td>'
        + '<td><button type="button" class="btn-icon btn-delete" onclick="removeCertificateRow(this)" title="Remove this certificate">X</button></td>';

    tbody.appendChild(tr);
    tr.querySelector('.cert-name').value = data.certificationName || data.CertificationName || '';
    tr.querySelector('.cert-body').value = data.certificationBody || data.CertificationBody || '';
    tr.querySelector('.cert-number').value = data.certificateNumber || data.CertificateNumber || '';
    tr.querySelector('.cert-issue-date').value = data.issueDate || data.IssueDate || '';
    tr.querySelector('.cert-expiry-date').value = data.expiryDate || data.ExpiryDate || '';
    tr.querySelector('.cert-renewal').checked = !!(data.renewalRequired || data.RenewalRequired);
    tr.querySelector('.cert-copy-path').value = copyPath;
    reindexCertificateRows();
}

function removeCertificateRow(btn) {
    var tr = btn.closest('tr');
    if (tr) tr.remove();
    reindexCertificateRows();
}

function reindexDocumentRows() {
    document.querySelectorAll('#documentTable tbody tr').forEach(function (tr, idx) {
        tr.setAttribute('data-row-index', idx);
        var fileInput = tr.querySelector('.doc-file');
        if (fileInput) {
            fileInput.name = 'DocFile_' + idx;
            fileInput.removeAttribute('form');
        }
    });
}

function addDocumentRow(data) {
    data = data || {};
    var tbody = document.querySelector('#documentTable tbody');
    if (!tbody) return;

    var rowIndex = tbody.querySelectorAll('tr').length;
    var docPath = data.documentPath || data.DocumentPath || '';
    var fileName = data.originalFileName || data.OriginalFileName || '';
    var viewLabel = fileName || 'View Document';
    var viewLink = docPath
        ? '<a class="doc-view-link" href="' + escapeHtml(docPath) + '" target="_blank" title="' + escapeHtml(viewLabel) + '">View</a><br/>'
        : '';

    var docTypes = readJsonScript('documentTypeLookupData');
    var typeOptions = '<option value="">-- Select --</option>';
    docTypes.forEach(function (dt) {
        var id = dt.id || dt.Id;
        var name = dt.name || dt.Name || '';
        typeOptions += '<option value="' + escapeHtml(id) + '">' + escapeHtml(name) + '</option>';
    });

    var tr = document.createElement('tr');
    tr.setAttribute('data-row-index', rowIndex);
    tr.setAttribute('data-record-id', data.employeeDocumentID || data.EmployeeDocumentID || 0);
    tr.innerHTML = ''
        + '<td><select class="form-control doc-type">' + typeOptions + '</select></td>'
        + '<td><input type="text" class="form-control doc-number" maxlength="100" /></td>'
        + '<td><input type="date" class="form-control doc-issue-date" /></td>'
        + '<td><input type="date" class="form-control doc-expiry-date" /></td>'
        + '<td><input type="text" class="form-control doc-remarks" maxlength="250" /></td>'
        + '<td>' + viewLink
        + '<input type="file" class="doc-file" name="DocFile_' + rowIndex + '" accept=".pdf,.jpg,.jpeg,.png,.doc,.docx" />'
        + '<input type="hidden" class="doc-path" />'
        + '<input type="hidden" class="doc-original-name" /></td>'
        + '<td><select class="form-control doc-verification">'
        + '  <option value="Pending">Pending</option>'
        + '  <option value="Verified">Verified</option>'
        + '  <option value="Rejected">Rejected</option>'
        + '</select></td>'
        + '<td><button type="button" class="btn-icon btn-delete" onclick="removeDocumentRow(this)" title="Remove this document">X</button></td>';

    tbody.appendChild(tr);
    tr.querySelector('.doc-type').value = data.documentTypeID || data.DocumentTypeID || '';
    tr.querySelector('.doc-number').value = data.documentNumber || data.DocumentNumber || '';
    tr.querySelector('.doc-issue-date').value = data.issueDate || data.IssueDate || '';
    tr.querySelector('.doc-expiry-date').value = data.expiryDate || data.ExpiryDate || '';
    tr.querySelector('.doc-remarks').value = data.remarks || data.Remarks || '';
    tr.querySelector('.doc-path').value = docPath;
    tr.querySelector('.doc-original-name').value = fileName;
    tr.querySelector('.doc-verification').value = data.verificationStatus || data.VerificationStatus || 'Pending';
    reindexDocumentRows();
}

function removeDocumentRow(btn) {
    var tr = btn.closest('tr');
    if (tr) tr.remove();
    reindexDocumentRows();
}

function escapeHtml(value) {
    return String(value || '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function addBankRow(data) {
    data = data || {};
    var tbody = document.querySelector('#bankTable tbody');
    if (!tbody) return;

    var bankOptions = readJsonScript('bankLookupData');
    var currencyOptions = readJsonScript('currencyLookupData');
    var bankGroupOptions = readJsonScript('bankGroupLookupData');
    var optionHtml = '<option value="">-- Select Bank --</option>';
    bankOptions.forEach(function (bank) {
        var id = bank.id || bank.Id;
        var name = bank.name || bank.Name || '';
        optionHtml += '<option value="' + escapeHtml(id) + '">' + escapeHtml(name) + '</option>';
    });

    var currencyOptionHtml = '<option value="">-- Select --</option>';
    currencyOptions.forEach(function (currency) {
        var code = currency.code || currency.Code || '';
        var name = currency.name || currency.Name || '';
        var label = code && name ? code + ' - ' + name : code || name;
        currencyOptionHtml += '<option value="' + escapeHtml(code) + '">' + escapeHtml(label) + '</option>';
    });

    var bankGroupOptionHtml = '<option value="">-- Select --</option>';
    bankGroupOptions.forEach(function (group) {
        var id = group.id || group.Id;
        var name = group.name || group.Name || '';
        bankGroupOptionHtml += '<option value="' + escapeHtml(id) + '">' + escapeHtml(name) + '</option>';
    });

    var tr = document.createElement('tr');
    tr.setAttribute('data-record-id', data.employeeBankID || data.EmployeeBankID || 0);
    tr.innerHTML = ''
        + '<td><select class="form-control bank-id">' + optionHtml + '</select></td>'
        + '<td><input type="text" class="form-control bank-code" maxlength="50" /></td>'
        + '<td><input type="text" class="form-control bank-location-name" maxlength="150" /></td>'
        + '<td><select class="form-control bank-group-id">' + bankGroupOptionHtml + '</select></td>'
        + '<td><input type="text" class="form-control bank-iban" maxlength="50" /></td>'
        + '<td><input type="text" class="form-control bank-swift" maxlength="50" /></td>'
        + '<td><select class="form-control bank-currency-code">' + currencyOptionHtml + '</select></td>'
        + '<td><select class="form-control bank-verification-status">'
        + '  <option value="Pending">Pending</option>'
        + '  <option value="Verified">Verified</option>'
        + '  <option value="Rejected">Rejected</option>'
        + '</select></td>'
        + '<td><input type="checkbox" class="bank-primary" /></td>'
        + '<td><button type="button" class="btn-icon btn-delete" onclick="removeRow(this)" title="Remove this bank">X</button></td>';

    tbody.appendChild(tr);
    tr.querySelector('.bank-id').value = data.bankID || data.BankID || '';
    tr.querySelector('.bank-code').value = data.bankCode || data.BankCode || data.branchCode || data.BranchCode || '';
    tr.querySelector('.bank-location-name').value = data.locationName || data.LocationName || data.branchName || data.BranchName || '';
    tr.querySelector('.bank-group-id').value = data.bankGroupID || data.BankGroupID || '';
    tr.querySelector('.bank-iban').value = data.iban || data.IBAN || '';
    tr.querySelector('.bank-swift').value = data.swiftBICCode || data.SwiftBICCode || '';
    tr.querySelector('.bank-currency-code').value = data.currencyCode || data.CurrencyCode || data.accountType || data.AccountType || '';
    tr.querySelector('.bank-verification-status').value = data.accountVerificationStatus || data.AccountVerificationStatus || 'Pending';
    tr.querySelector('.bank-primary').checked = !!(data.isPrimary || data.IsPrimary);
}

function removeRow(button) {
    var tr = button.closest('tr');
    if (tr) tr.remove();
}

function rowRecordId(tr) {
    return parseInt(tr.getAttribute('data-record-id') || '0', 10) || 0;
}

function readContactRows() {
    return Array.from(document.querySelectorAll('#contactTable tbody tr'))
        .map(function (tr) {
            return {
                ContactID: rowRecordId(tr),
                ContactType: tr.querySelector('.contact-type').value,
                ContactName: tr.querySelector('.contact-name').value.trim(),
                Relationship: tr.querySelector('.contact-relationship').value.trim(),
                ContactValue: tr.querySelector('.contact-value').value.trim(),
                IsPrimary: tr.querySelector('.contact-primary').checked
            };
        })
        .filter(function (c) { return c.ContactValue || c.ContactName; });
}

function readAddressRows() {
    return Array.from(document.querySelectorAll('#addressTable tbody tr'))
        .map(function (tr) {
            return {
                AddressID: rowRecordId(tr),
                AddressType: tr.querySelector('.address-type').value,
                AddressLine: tr.querySelector('.address-line').value.trim(),
                City: tr.querySelector('.address-city').value.trim(),
                ProvinceState: tr.querySelector('.address-province').value.trim(),
                PostalCode: tr.querySelector('.address-postal').value.trim(),
                IsPrimary: tr.querySelector('.address-primary').checked
            };
        })
        .filter(function (a) { return a.AddressLine; });
}

function readFamilyRows() {
    return Array.from(document.querySelectorAll('#familyTable tbody tr'))
        .map(function (tr) {
            return {
                FamilyMemberID: rowRecordId(tr),
                MemberName: tr.querySelector('.family-name').value.trim(),
                Relationship: tr.querySelector('.family-relationship').value.trim(),
                Gender: tr.querySelector('.family-gender').value,
                DateOfBirth: tr.querySelector('.family-dob').value,
                ContactNumber: tr.querySelector('.family-contact').value.trim(),
                IsDependent: tr.querySelector('.family-dependent').checked
            };
        })
        .filter(function (f) { return f.MemberName; });
}

function readEducationRows() {
    return Array.from(document.querySelectorAll('#educationTable tbody tr'))
        .map(function (tr) {
            return {
                EducationID: rowRecordId(tr),
                HighestQualification: tr.querySelector('.edu-qualification').value,
                DegreeCertificate: tr.querySelector('.edu-degree').value.trim(),
                Specialization: tr.querySelector('.edu-specialization').value.trim(),
                Institution: tr.querySelector('.edu-institution').value.trim(),
                YearOfPassing: tr.querySelector('.edu-year').value,
                GradeCGPA: tr.querySelector('.edu-grade').value.trim()
            };
        })
        .filter(function (e) {
            return e.HighestQualification || e.DegreeCertificate || e.Institution;
        });
}

function readCertificateRows() {
    return Array.from(document.querySelectorAll('#certificateTable tbody tr'))
        .map(function (tr) {
            return {
                CertificateID: rowRecordId(tr),
                CertificationName: tr.querySelector('.cert-name').value.trim(),
                CertificationBody: tr.querySelector('.cert-body').value.trim(),
                CertificateNumber: tr.querySelector('.cert-number').value.trim(),
                IssueDate: tr.querySelector('.cert-issue-date').value,
                ExpiryDate: tr.querySelector('.cert-expiry-date').value,
                RenewalRequired: tr.querySelector('.cert-renewal').checked,
                CertificateCopyPath: tr.querySelector('.cert-copy-path').value
            };
        });
}

function readDocumentRows() {
    return Array.from(document.querySelectorAll('#documentTable tbody tr'))
        .map(function (tr) {
            return {
                EmployeeDocumentID: rowRecordId(tr),
                DocumentTypeID: parseInt(tr.querySelector('.doc-type').value || '0', 10),
                DocumentNumber: tr.querySelector('.doc-number').value.trim(),
                IssueDate: tr.querySelector('.doc-issue-date').value,
                ExpiryDate: tr.querySelector('.doc-expiry-date').value,
                Remarks: tr.querySelector('.doc-remarks').value.trim(),
                DocumentPath: tr.querySelector('.doc-path').value,
                OriginalFileName: tr.querySelector('.doc-original-name').value,
                VerificationStatus: tr.querySelector('.doc-verification').value
            };
        });
}

function readBankRows() {
    return Array.from(document.querySelectorAll('#bankTable tbody tr'))
        .map(function (tr) {
            return {
                EmployeeBankID: rowRecordId(tr),
                BankID: parseInt(tr.querySelector('.bank-id').value || '0', 10),
                BankCode: tr.querySelector('.bank-code').value.trim(),
                LocationName: tr.querySelector('.bank-location-name').value.trim(),
                BankGroupID: parseInt(tr.querySelector('.bank-group-id').value || '0', 10),
                IBAN: tr.querySelector('.bank-iban').value.trim(),
                SwiftBICCode: tr.querySelector('.bank-swift').value.trim(),
                CurrencyCode: tr.querySelector('.bank-currency-code').value.trim(),
                AccountVerificationStatus: tr.querySelector('.bank-verification-status').value,
                IsPrimary: tr.querySelector('.bank-primary').checked
            };
        })
        .filter(function (b) { return b.BankID > 0; });
}

function hasDuplicateKeys(items, keyFn, message) {
    var seen = {};
    for (var i = 0; i < items.length; i++) {
        var key = (keyFn(items[i]) || '').toLowerCase();
        if (!key) continue;
        if (seen[key]) {
            if (typeof showClientNotice === 'function') showClientNotice(message);
            else alert(message);
            return true;
        }
        seen[key] = true;
    }
    return false;
}

function validateProfileSectionDuplicates(section) {
    if (section === 'contacts') {
        return !hasDuplicateKeys(readContactRows(), function (c) {
            return (c.ContactType || '') + '|' + (c.ContactValue || '') + '|' + (c.ContactName || '');
        }, 'Duplicate contact entry detected. Each contact type/value combination must be unique.');
    }
    if (section === 'addresses') {
        return !hasDuplicateKeys(readAddressRows(), function (a) {
            return (a.AddressType || '') + '|' + (a.AddressLine || '') + '|' + (a.City || '') + '|' + (a.PostalCode || '');
        }, 'Duplicate address entry detected. Remove the duplicate before saving.');
    }
    if (section === 'family') {
        return !hasDuplicateKeys(readFamilyRows(), function (f) {
            return (f.MemberName || '') + '|' + (f.Relationship || '') + '|' + (f.DateOfBirth || '');
        }, 'Duplicate family member entry detected. Remove the duplicate before saving.');
    }
    if (section === 'education') {
        return !hasDuplicateKeys(readEducationRows(), function (e) {
            return (e.HighestQualification || '') + '|' + (e.DegreeCertificate || '') + '|' + (e.Institution || '') + '|' + (e.YearOfPassing || '');
        }, 'Duplicate education entry detected. Remove the duplicate before saving.');
    }
    if (section === 'certificates') {
        var certs = readCertificateRows().filter(function (c) {
            return c.CertificationName || c.CertificateNumber || c.CertificationBody;
        });
        return !hasDuplicateKeys(certs, function (c) {
            return (c.CertificationName || '') + '|' + (c.CertificateNumber || '') + '|' + (c.CertificationBody || '');
        }, 'Duplicate certificate entry detected. Remove the duplicate before saving.');
    }
    if (section === 'documents') {
        var docs = readDocumentRows().filter(function (d) {
            return d.DocumentTypeID > 0 || d.DocumentNumber || d.DocumentPath || d.Remarks;
        });
        return !hasDuplicateKeys(docs, function (d) {
            return (d.DocumentTypeID || 0) + '|' + (d.DocumentNumber || '');
        }, 'Duplicate document entry detected. Each document type/number must be unique.');
    }
    if (section === 'banks') {
        return !hasDuplicateKeys(readBankRows(), function (b) {
            return (b.BankID || 0) + '|' + (b.IBAN || '') + '|' + (b.BankCode || '');
        }, 'Duplicate bank account entry detected. Each bank/IBAN combination must be unique.');
    }
    return true;
}

function prepareEmployeePayload() {
    if (!validateForm()) return false;

    if (window.HrmsValidation) {
        if (!HrmsValidation.validateEmployeeContactRows('#contactTable')) return false;
        if (!HrmsValidation.validateFamilyContactRows('#familyTable')) return false;
    }

    if (!validateProfileSectionDuplicates('contacts')) return false;
    if (!validateProfileSectionDuplicates('addresses')) return false;
    if (!validateProfileSectionDuplicates('family')) return false;
    if (!validateProfileSectionDuplicates('education')) return false;
    if (!validateProfileSectionDuplicates('banks')) return false;

    document.getElementById('ContactsJson').value = JSON.stringify(readContactRows());
    document.getElementById('AddressesJson').value = JSON.stringify(readAddressRows());
    document.getElementById('FamilyMembersJson').value = JSON.stringify(readFamilyRows());
    document.getElementById('EducationJson').value = JSON.stringify(readEducationRows());
    document.getElementById('BanksJson').value = JSON.stringify(readBankRows());
    if (document.getElementById('CertificatesJson'))
        document.getElementById('CertificatesJson').value = JSON.stringify(readCertificateRows());
    if (document.getElementById('DocumentsJson'))
        document.getElementById('DocumentsJson').value = JSON.stringify(readDocumentRows());
    hideClientNotice();
    return true;
}

function submitProfileSection(section) {
    hideClientNotice();

    var employeeIdEl = document.querySelector('input[name="EmployeeID"]');
    var employeeId = employeeIdEl ? employeeIdEl.value : '';
    var employeeCodeEl = document.getElementById('txtEmpCode');
    var employeeCode = employeeCodeEl ? employeeCodeEl.value : '';

    if ((!employeeId || employeeId === '0') && !employeeCode) {
        if (typeof showClientNotice === 'function') showClientNotice('Please save or select an employee before saving profile details.');
        else alert('Please save or select an employee before saving profile details.');
        return false;
    }

    if (section === 'contacts' && window.HrmsValidation && !HrmsValidation.validateEmployeeContactRows('#contactTable')) return false;
    if (section === 'family' && window.HrmsValidation && !HrmsValidation.validateFamilyContactRows('#familyTable')) return false;
    if (!validateProfileSectionDuplicates(section)) return false;

    var handler = 'Save';
    if (section === 'contacts') {
        var contactsEl = document.getElementById('ContactsJson');
        if (!contactsEl) {
            if (typeof showClientNotice === 'function') showClientNotice('Contact form is not ready. Please refresh the page.');
            else alert('Contact form is not ready. Please refresh the page.');
            return false;
        }
        contactsEl.value = JSON.stringify(readContactRows());
        handler = 'SaveContacts';
    } else if (section === 'addresses') {
        document.getElementById('AddressesJson').value = JSON.stringify(readAddressRows());
        handler = 'SaveAddresses';
    } else if (section === 'family') {
        document.getElementById('FamilyMembersJson').value = JSON.stringify(readFamilyRows());
        handler = 'SaveFamilyMembers';
    } else if (section === 'education') {
        document.getElementById('EducationJson').value = JSON.stringify(readEducationRows());
        handler = 'SaveEducation';
    } else if (section === 'certificates') {
        reindexCertificateRows();
        document.getElementById('CertificatesJson').value = JSON.stringify(readCertificateRows());
        handler = 'SaveCertificates';
    } else if (section === 'documents') {
        reindexDocumentRows();
        document.getElementById('DocumentsJson').value = JSON.stringify(readDocumentRows());
        handler = 'SaveDocuments';
    } else if (section === 'banks') {
        document.getElementById('BanksJson').value = JSON.stringify(readBankRows());
        handler = 'SaveBanks';
    }

    var handlerEl = document.getElementById('__handler');
    if (!handlerEl) {
        if (typeof showClientNotice === 'function') showClientNotice('Save handler is missing. Please refresh the page.');
        else alert('Save handler is missing. Please refresh the page.');
        return false;
    }
    handlerEl.value = handler;
    var form = document.getElementById('form1') || document.forms[0];
    if (!form) {
        if (typeof showClientNotice === 'function') showClientNotice('Form not found. Please refresh the page.');
        else alert('Form not found. Please refresh the page.');
        return false;
    }
    form.submit();
    return false;
}

function switchProfileTab(targetTabId, clickedButton) {
    var panels = document.querySelectorAll('.profile-tab-panel');
    var buttons = document.querySelectorAll('.profile-tab-btn');

    panels.forEach(function (panel) {
        panel.classList.toggle('active', panel.id === targetTabId);
    });

    buttons.forEach(function (btn) {
        btn.classList.toggle('active', btn === clickedButton);
    });
}
