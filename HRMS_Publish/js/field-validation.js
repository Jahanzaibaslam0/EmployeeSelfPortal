/* HRMS – shared email & phone validation */
'use strict';

window.HrmsValidation = (function () {
    var EMAIL_RE = /^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$/;
    var PHONE_CHARS_RE = /^[\d\s\-\+\(\)\.]+$/;

    var EMAIL_MSG = 'Enter a valid email address (e.g. name@company.com).';
    var PHONE_MSG = 'Enter a valid phone number (7–15 digits; +, spaces, and dashes allowed).';

    function trim(v) {
        return (v || '').trim();
    }

    function digitCount(v) {
        return (v.match(/\d/g) || []).length;
    }

    function isEmail(value, required) {
        var v = trim(value);
        if (!v) return !required;
        if (v.length > 150) return false;
        return EMAIL_RE.test(v);
    }

    function isPhone(value, required) {
        var v = trim(value);
        if (!v) return !required;
        if (v.length > 50) return false;
        if (!PHONE_CHARS_RE.test(v)) return false;
        var digits = digitCount(v);
        return digits >= 7 && digits <= 15;
    }

    function isEmailContactType(type) {
        var t = trim(type).toLowerCase();
        return t === 'personalemail' || t === 'officialemail';
    }

    function isPhoneContactType(type) {
        var t = trim(type).toLowerCase();
        return t === 'personalmobile' || t === 'officialmobile' || t === 'whatsapp' || t === 'emergency';
    }

    function validateEmployeeContact(contactType, contactValue) {
        var value = trim(contactValue);
        if (!value) return { valid: true, message: '' };

        if (isEmailContactType(contactType)) {
            return isEmail(value, false)
                ? { valid: true, message: '' }
                : { valid: false, message: (contactType || 'Email') + ': ' + EMAIL_MSG };
        }

        if (isPhoneContactType(contactType)) {
            return isPhone(value, false)
                ? { valid: true, message: '' }
                : { valid: false, message: (contactType || 'Phone') + ': ' + PHONE_MSG };
        }

        return { valid: true, message: '' };
    }

    function markField(input, valid, message) {
        if (!input) return;
        input.classList.toggle('field-invalid', !valid);
        input.setAttribute('aria-invalid', valid ? 'false' : 'true');

        var group = input.closest('.form-group') || input.parentElement;
        if (!group) return;

        var err = group.querySelector('.field-error');
        if (!err) {
            err = document.createElement('span');
            err.className = 'field-error';
            group.appendChild(err);
        }
        err.textContent = valid ? '' : (message || '');
    }

    function validateInput(input) {
        if (!input) return true;
        var kind = (input.getAttribute('data-validate') || '').toLowerCase();
        if (!kind) return true;

        var label = input.getAttribute('data-label') || input.name || 'Field';
        var required = input.hasAttribute('data-required');
        var value = input.value;
        var valid = true;
        var message = '';

        if (kind === 'email') {
            valid = isEmail(value, required);
            message = valid ? '' : (required && !trim(value) ? label + ' is required.' : label + ': ' + EMAIL_MSG);
        } else if (kind === 'phone') {
            valid = isPhone(value, required);
            message = valid ? '' : (required && !trim(value) ? label + ' is required.' : label + ': ' + PHONE_MSG);
        }

        markField(input, valid, message);
        return valid;
    }

    function validateForm(form) {
        if (!form) return true;
        var inputs = form.querySelectorAll('[data-validate]');
        var ok = true;
        var firstBad = null;

        inputs.forEach(function (input) {
            if (!validateInput(input)) {
                ok = false;
                if (!firstBad) firstBad = input;
            }
        });

        if (firstBad) firstBad.focus();
        return ok;
    }

    function validateEmployeeContactRows(tableSelector) {
        var rows = document.querySelectorAll(tableSelector + ' tbody tr');
        var ok = true;
        var firstBad = null;
        var firstMsg = '';

        rows.forEach(function (tr) {
            var typeEl = tr.querySelector('.contact-type');
            var valueEl = tr.querySelector('.contact-value');
            if (!typeEl || !valueEl) return;

            var result = validateEmployeeContact(typeEl.value, valueEl.value);
            valueEl.classList.toggle('field-invalid', !result.valid);
            if (!result.valid) {
                ok = false;
                if (!firstBad) {
                    firstBad = valueEl;
                    firstMsg = result.message;
                }
            }
        });

        if (firstBad) {
            firstBad.focus();
            if (typeof showClientNotice === 'function') showClientNotice(firstMsg);
            else alert(firstMsg);
        }

        return ok;
    }

    function validateFamilyContactRows(tableSelector) {
        var rows = document.querySelectorAll(tableSelector + ' tbody tr');
        var ok = true;
        var firstBad = null;

        rows.forEach(function (tr) {
            var input = tr.querySelector('.family-contact');
            if (!input || !trim(input.value)) return;
            if (!isPhone(input.value, false)) {
                ok = false;
                input.classList.add('field-invalid');
                if (!firstBad) firstBad = input;
            } else {
                input.classList.remove('field-invalid');
            }
        });

        if (firstBad) {
            firstBad.focus();
            var msg = 'Family contact number: ' + PHONE_MSG;
            if (typeof showClientNotice === 'function') showClientNotice(msg);
            else alert(msg);
        }

        return ok;
    }

    function bindContactTypeHints(row) {
        var typeEl = row.querySelector('.contact-type');
        var valueEl = row.querySelector('.contact-value');
        if (!typeEl || !valueEl) return;

        function syncHint() {
            if (isEmailContactType(typeEl.value)) {
                valueEl.setAttribute('data-validate', 'email');
                valueEl.setAttribute('placeholder', 'name@company.com');
            } else if (isPhoneContactType(typeEl.value)) {
                valueEl.setAttribute('data-validate', 'phone');
                valueEl.setAttribute('placeholder', '+92 300 1234567');
            } else {
                valueEl.removeAttribute('data-validate');
                valueEl.removeAttribute('placeholder');
            }
            valueEl.classList.remove('field-invalid');
        }

        typeEl.addEventListener('change', syncHint);
        syncHint();
        bindInput(valueEl);
    }

    function bindInput(input) {
        if (!input || input.dataset.hrmsBound === '1') return;
        input.dataset.hrmsBound = '1';
        input.addEventListener('blur', function () { validateInput(input); });
        input.addEventListener('input', function () {
            if (input.classList.contains('field-invalid')) validateInput(input);
        });
    }

    function init() {
        document.querySelectorAll('[data-validate]').forEach(bindInput);

        document.querySelectorAll('form[data-hrms-validate="true"]').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                if (!validateForm(form)) e.preventDefault();
            });
        });
    }

    return {
        isEmail: isEmail,
        isPhone: isPhone,
        validateEmployeeContact: validateEmployeeContact,
        validateInput: validateInput,
        validateForm: validateForm,
        validateEmployeeContactRows: validateEmployeeContactRows,
        validateFamilyContactRows: validateFamilyContactRows,
        bindContactTypeHints: bindContactTypeHints,
        bindInput: bindInput,
        init: init,
        EMAIL_MSG: EMAIL_MSG,
        PHONE_MSG: PHONE_MSG
    };
}());

document.addEventListener('DOMContentLoaded', function () {
    if (window.HrmsValidation) HrmsValidation.init();
});
