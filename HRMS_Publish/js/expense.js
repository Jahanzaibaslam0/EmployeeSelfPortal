(function () {
    var categories = [];

    function readJsonScript(id) {
        var el = document.getElementById(id);
        if (!el || !el.textContent) return [];
        try { return JSON.parse(el.textContent); } catch (e) { return []; }
    }

    function buildCategoryOptions(selectedId) {
        var html = '<option value="0">-- Select --</option>';
        categories.forEach(function (c) {
            var id = c.id || c.Id;
            var name = c.name || c.Name;
            var sel = String(id) === String(selectedId) ? ' selected' : '';
            html += '<option value="' + id + '"' + sel + '>' + name + '</option>';
        });
        return html;
    }

    function esc(v) { return (v == null ? '' : String(v)).replace(/"/g, '&quot;'); }

    window.addExpenseDetailRow = function (data) {
        data = data || {};
        var tbody = document.querySelector('#expenseDetailTable tbody');
        var tr = document.createElement('tr');
        var approval = data.approvalStatus || data.ApprovalStatus || 'Pending';
        tr.innerHTML =
            '<td><select class="form-control det-cat">' + buildCategoryOptions(data.expenseCategoryID || data.ExpenseCategoryID || 0) + '</select></td>' +
            '<td><input type="text" class="form-control det-desc" maxlength="250" value="' + esc(data.description || data.Description) + '" /></td>' +
            '<td><input type="text" class="form-control det-pay" maxlength="50" value="' + esc(data.paymentMethod || data.PaymentMethod) + '" /></td>' +
            '<td><input type="date" class="form-control det-date" value="' + esc(data.transactionDate || data.TransactionDate) + '" /></td>' +
            '<td><input type="text" class="form-control det-curr" maxlength="10" value="' + esc(data.currency || data.Currency || 'PKR') + '" /></td>' +
            '<td><input type="number" step="0.01" class="form-control det-txn-amt" value="' + esc(data.transactionAmount || data.TransactionAmount) + '" /></td>' +
            '<td><input type="number" step="0.01" class="form-control det-amt" value="' + esc(data.amount || data.Amount) + '" /></td>' +
            '<td><select class="form-control det-appr">' +
            ['Pending', 'Approved', 'Rejected'].map(function (s) {
                return '<option value="' + s + '"' + (s === approval ? ' selected' : '') + '>' + s + '</option>';
            }).join('') + '</select></td>' +
            '<td><input type="text" class="form-control det-rcpt" maxlength="50" value="' + esc(data.originalReceiptID || data.OriginalReceiptID) + '" /></td>' +
            '<td><input type="hidden" class="det-path" value="' + esc(data.originalReceiptDocPath || data.OriginalReceiptDocPath) + '" /><span class="muted">—</span></td>' +
            '<td><button type="button" class="btn btn-danger" style="padding:2px 8px;font-size:.75rem;" onclick="removeExpenseDetailRow(this)">X</button></td>';
        tbody.appendChild(tr);
    };

    window.removeExpenseDetailRow = function (btn) {
        var tbody = document.querySelector('#expenseDetailTable tbody');
        if (tbody.querySelectorAll('tr').length <= 1) return;
        btn.closest('tr').remove();
    };

    window.prepareExpensePayload = function () {
        var rows = [];
        document.querySelectorAll('#expenseDetailTable tbody tr').forEach(function (tr) {
            var cat = parseInt(tr.querySelector('.det-cat').value, 10) || 0;
            var desc = tr.querySelector('.det-desc').value.trim();
            var amt = tr.querySelector('.det-amt').value;
            if (cat <= 0 && !desc && !(parseFloat(amt) > 0)) return;
            rows.push({
                expenseCategoryID: cat,
                description: desc,
                paymentMethod: tr.querySelector('.det-pay').value.trim(),
                transactionDate: tr.querySelector('.det-date').value,
                currency: tr.querySelector('.det-curr').value.trim() || 'PKR',
                transactionAmount: tr.querySelector('.det-txn-amt').value,
                amount: amt,
                approvalStatus: tr.querySelector('.det-appr').value,
                originalReceiptID: tr.querySelector('.det-rcpt').value.trim(),
                originalReceiptDocPath: tr.querySelector('.det-path').value || ''
            });
        });
        if (!rows.length) {
            alert('Add at least one expense line item.');
            return false;
        }
        document.getElementById('DetailsJson').value = JSON.stringify(rows);
        return true;
    };

    document.addEventListener('DOMContentLoaded', function () {
        categories = readJsonScript('expenseCategoryData');
        var items = readJsonScript('initialExpenseDetailsData');
        if (!items.length) items = [{}];
        items.forEach(function (d) { addExpenseDetailRow(d); });
    });
})();
