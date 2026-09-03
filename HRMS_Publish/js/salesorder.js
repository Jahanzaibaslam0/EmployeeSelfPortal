(function () {
    var products = [];
    var readOnly = false;

    function readJsonScript(id) {
        var el = document.getElementById(id);
        if (!el || !el.textContent) return [];
        try { return JSON.parse(el.textContent); } catch (e) { return []; }
    }

    function findProduct(id) {
        return products.find(function (p) {
            return String(p.id || p.Id) === String(id);
        });
    }

    function buildProductOptions(selectedId) {
        var html = '<option value="">-- Select Product --</option>';
        products.forEach(function (p) {
            var id = p.id || p.Id;
            var name = p.name || p.Name;
            var sel = String(id) === String(selectedId) ? ' selected' : '';
            html += '<option value="' + id + '"' + sel + '>' + name + '</option>';
        });
        return html;
    }

    function numVal(el) {
        return parseFloat(el && el.value ? el.value : '') || 0;
    }

    function calcLineNet(tr) {
        var qty = numVal(tr.querySelector('.item-qty'));
        var unitPrice = numVal(tr.querySelector('.item-unit-price'));
        var tax = numVal(tr.querySelector('.item-tax'));
        var discount = numVal(tr.querySelector('.item-discount'));
        return qty * unitPrice + tax - discount;
    }

    function updateTotals() {
        var totalQty = 0;
        var totalTax = 0;
        var totalDiscount = 0;
        var grandTotal = 0;

        document.querySelectorAll('#soItemTable tbody tr').forEach(function (tr) {
            var qty = numVal(tr.querySelector('.item-qty'));
            var tax = numVal(tr.querySelector('.item-tax'));
            var discount = numVal(tr.querySelector('.item-discount'));
            var net = calcLineNet(tr);

            totalQty += qty;
            totalTax += tax;
            totalDiscount += discount;
            grandTotal += net;

            var lineEl = tr.querySelector('.item-line-net');
            if (lineEl) lineEl.textContent = net.toFixed(2);
        });

        var lblQty = document.getElementById('lblTotalQty');
        var lblTax = document.getElementById('lblTotalTax');
        var lblDiscount = document.getElementById('lblTotalDiscount');
        var lblGrand = document.getElementById('lblGrandTotal');

        if (lblQty) lblQty.textContent = totalQty.toFixed(4).replace(/\.?0+$/, '');
        if (lblTax) lblTax.textContent = totalTax.toFixed(2);
        if (lblDiscount) lblDiscount.textContent = totalDiscount.toFixed(2);
        if (lblGrand) lblGrand.textContent = grandTotal.toFixed(2);

        ['TotalQty', 'TotalTax', 'TotalDiscount', 'GrandTotal'].forEach(function (id, i) {
            var el = document.getElementById(id);
            if (!el) return;
            var vals = [totalQty.toFixed(4), totalTax.toFixed(2), totalDiscount.toFixed(2), grandTotal.toFixed(2)];
            el.value = vals[i];
        });
    }

    function bindRowEvents(tr) {
        tr.querySelectorAll('.item-qty, .item-unit-price, .item-tax, .item-discount').forEach(function (el) {
            el.addEventListener('input', updateTotals);
        });

        var productSelect = tr.querySelector('.item-product');
        if (productSelect) {
            productSelect.addEventListener('change', function () {
                var prod = findProduct(this.value);
                if (!prod) return;
                tr.querySelector('.item-product-code').value = prod.code || prod.Code || '';
                tr.querySelector('.item-product-desc').value = prod.productName || prod.ProductName || '';
                var priceEl = tr.querySelector('.item-unit-price');
                var sellingPrice = prod.sellingPrice || prod.SellingPrice || '';
                if (priceEl && sellingPrice && !priceEl.value) {
                    priceEl.value = sellingPrice;
                }
                updateTotals();
            });
        }
    }

    window.addSOItemRow = function (data) {
        data = data || {};
        var tbody = document.querySelector('#soItemTable tbody');
        var tr = document.createElement('tr');
        var disabled = readOnly ? ' disabled' : '';

        tr.innerHTML =
            '<td><select class="form-control item-product"' + disabled + '>' + buildProductOptions(data.productID || data.ProductID) + '</select>' +
            '<input type="hidden" class="item-product-code" /></td>' +
            '<td><input type="text" class="form-control item-product-desc readonly-field" readonly maxlength="200" /></td>' +
            '<td><input type="number" step="0.0001" min="0" class="form-control item-qty"' + disabled + ' /></td>' +
            '<td><input type="number" step="0.0001" min="0" class="form-control item-unit-price"' + disabled + ' /></td>' +
            '<td><input type="number" step="0.01" min="0" class="form-control item-tax"' + disabled + ' /></td>' +
            '<td><input type="number" step="0.01" min="0" class="form-control item-discount"' + disabled + ' /></td>' +
            '<td class="item-line-net" style="text-align:right;font-weight:600;white-space:nowrap;">0.00</td>' +
            '<td>' + (readOnly ? '' : '<button type="button" class="btn btn-danger" style="padding:2px 8px;font-size:.75rem;" onclick="removeSOItemRow(this)">Remove</button>') + '</td>';

        tbody.appendChild(tr);

        tr.querySelector('.item-product-code').value = data.productCode || data.ProductCode || '';
        tr.querySelector('.item-product-desc').value = data.productDescription || data.ProductDescription || '';
        tr.querySelector('.item-qty').value = data.qty || data.Qty || '';
        tr.querySelector('.item-unit-price').value = data.unitPrice || data.UnitPrice || '';
        tr.querySelector('.item-tax').value = data.taxAmount || data.TaxAmount || '';
        tr.querySelector('.item-discount').value = data.discountAmount || data.DiscountAmount || '';

        bindRowEvents(tr);
        updateTotals();
    };

    window.removeSOItemRow = function (btn) {
        if (readOnly) return;
        var tbody = document.querySelector('#soItemTable tbody');
        if (tbody.querySelectorAll('tr').length <= 1) return;
        btn.closest('tr').remove();
        updateTotals();
    };

    function readItemRows() {
        return Array.from(document.querySelectorAll('#soItemTable tbody tr')).map(function (tr) {
            return {
                productID: parseInt(tr.querySelector('.item-product').value, 10) || 0,
                productCode: tr.querySelector('.item-product-code').value.trim(),
                productDescription: tr.querySelector('.item-product-desc').value.trim(),
                qty: tr.querySelector('.item-qty').value,
                unitPrice: tr.querySelector('.item-unit-price').value,
                taxAmount: tr.querySelector('.item-tax').value,
                discountAmount: tr.querySelector('.item-discount').value
            };
        }).filter(function (r) {
            return r.productID > 0 || r.productDescription || r.productCode || r.qty || r.unitPrice;
        });
    }

    function validateForm() {
        var dateEl = document.getElementById('txtSODate');
        if (!dateEl || !dateEl.value) {
            alert('Please enter sales order date.');
            return false;
        }
        var customerEl = document.getElementById('ddlCustomer');
        if (!customerEl || !customerEl.value) {
            alert('Please select a customer from Customer Master.');
            return false;
        }
        var items = readItemRows();
        if (!items.length) {
            alert('Add at least one order line item.');
            return false;
        }
        document.getElementById('ItemsJson').value = JSON.stringify(items);
        updateTotals();
        return true;
    }

    window.prepareSOPayload = function () {
        return validateForm();
    };

    window.prepareSOSubmit = function () {
        return validateForm();
    };

    window.loadCustomerDetails = function () {
        var ddl = document.getElementById('ddlCustomer');
        var nameEl = document.getElementById('txtCustomerName');
        if (!ddl || !ddl.value) {
            if (nameEl) nameEl.value = '';
            return;
        }

        fetch('/SalesOrderMaster?handler=CustomerInfo&customerId=' + encodeURIComponent(ddl.value))
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (nameEl && data.name) nameEl.value = data.name;
            })
            .catch(function () { });
    };

    document.addEventListener('DOMContentLoaded', function () {
        readOnly = document.getElementById('soForm')?.getAttribute('data-readonly') === 'true';
        products = readJsonScript('productLookupData');
        var items = readJsonScript('initialSOItemsData');
        if (!items.length) items = [{}];
        items.forEach(function (d) { addSOItemRow(d); });

        var ddlCustomer = document.getElementById('ddlCustomer');
        if (ddlCustomer && !readOnly) {
            ddlCustomer.addEventListener('change', loadCustomerDetails);
        }
    });
})();
