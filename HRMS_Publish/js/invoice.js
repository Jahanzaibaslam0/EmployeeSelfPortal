(function () {
    var products = [];
    var salesTypes = [];

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

    function buildSalesTypeOptions(selected) {
        var html = '<option value="">--</option>';
        salesTypes.forEach(function (t) {
            var sel = t === selected ? ' selected' : '';
            html += '<option value="' + t + '"' + sel + '>' + t + '</option>';
        });
        return html;
    }

    function numVal(el) {
        return parseFloat(el && el.value ? el.value : '') || 0;
    }

    function calcLineTotal(tr) {
        var qty = numVal(tr.querySelector('.item-qty'));
        var unitPrice = numVal(tr.querySelector('.item-unit-price'));
        var tax = numVal(tr.querySelector('.item-tax'));
        var extraTax = numVal(tr.querySelector('.item-extra-tax'));
        var fedPayable = numVal(tr.querySelector('.item-fed'));
        var furtherTax = numVal(tr.querySelector('.item-further-tax'));
        var discount = numVal(tr.querySelector('.item-discount'));
        return qty * unitPrice + tax + extraTax + fedPayable + furtherTax - discount;
    }

    function updateGrandTotal() {
        var total = 0;
        document.querySelectorAll('#invoiceItemTable tbody tr').forEach(function (tr) {
            var line = calcLineTotal(tr);
            total += line;
            var lineEl = tr.querySelector('.item-line-total');
            if (lineEl) lineEl.textContent = line.toFixed(2);
        });
        var grandEl = document.getElementById('lblGrandTotal');
        var hiddenEl = document.getElementById('TotalAmount');
        if (grandEl) grandEl.textContent = total.toFixed(2);
        if (hiddenEl) hiddenEl.value = total.toFixed(2);
    }

    function bindRowEvents(tr) {
        tr.querySelectorAll('.item-qty, .item-unit-price, .item-tax, .item-extra-tax, .item-fed, .item-further-tax, .item-discount').forEach(function (el) {
            el.addEventListener('input', updateGrandTotal);
        });

        var productSelect = tr.querySelector('.item-product');
        if (productSelect) {
            productSelect.addEventListener('change', function () {
                var prod = findProduct(this.value);
                if (!prod) return;
                tr.querySelector('.item-item-id').value = prod.itemID || prod.ItemID || '';
                tr.querySelector('.item-hs-code').value = prod.hsCode || prod.HSCode || '';
                tr.querySelector('.item-product-name').value = prod.productName || prod.ProductName || '';
                tr.querySelector('.item-uom').value = prod.unitOfMeasure || prod.UnitOfMeasure || '';
                updateGrandTotal();
            });
        }
    }

    window.addInvoiceItemRow = function (data) {
        data = data || {};
        var tbody = document.querySelector('#invoiceItemTable tbody');
        var tr = document.createElement('tr');

        tr.innerHTML =
            '<td><select class="form-control item-product">' + buildProductOptions(data.productID || data.ProductID) + '</select></td>' +
            '<td><input type="text" class="form-control item-item-id" maxlength="50" /></td>' +
            '<td><input type="text" class="form-control item-hs-code" maxlength="50" /></td>' +
            '<td><input type="text" class="form-control item-product-name" maxlength="200" /></td>' +
            '<td><input type="number" step="0.0001" min="0" class="form-control item-qty" /></td>' +
            '<td><input type="text" class="form-control item-uom" maxlength="50" /></td>' +
            '<td><input type="number" step="0.0001" min="0" class="form-control item-unit-price" /></td>' +
            '<td><input type="number" step="0.01" min="0" class="form-control item-tax" /></td>' +
            '<td><input type="number" step="0.01" min="0" class="form-control item-extra-tax" /></td>' +
            '<td><input type="number" step="0.01" min="0" class="form-control item-fed" /></td>' +
            '<td><select class="form-control item-sales-type">' + buildSalesTypeOptions(data.salesType || data.SalesType || '') + '</select></td>' +
            '<td><input type="text" class="form-control item-sro-serial" maxlength="100" /></td>' +
            '<td><input type="number" step="0.01" min="0" class="form-control item-further-tax" /></td>' +
            '<td><input type="number" step="0.01" min="0" class="form-control item-discount" /></td>' +
            '<td class="item-line-total" style="text-align:right;font-weight:600;white-space:nowrap;">0.00</td>' +
            '<td><button type="button" class="btn btn-danger" style="padding:2px 8px;font-size:.75rem;" onclick="removeInvoiceItemRow(this)">Remove</button></td>';

        tbody.appendChild(tr);

        tr.querySelector('.item-item-id').value = data.itemID || data.ItemID || '';
        tr.querySelector('.item-hs-code').value = data.hsCode || data.HSCode || '';
        tr.querySelector('.item-product-name').value = data.productName || data.ProductName || '';
        tr.querySelector('.item-qty').value = data.qty || data.Qty || '';
        tr.querySelector('.item-uom').value = data.unitOfMeasure || data.UnitOfMeasure || '';
        tr.querySelector('.item-unit-price').value = data.unitPrice || data.UnitPrice || '';
        tr.querySelector('.item-tax').value = data.taxAmount || data.TaxAmount || '';
        tr.querySelector('.item-extra-tax').value = data.extraTax || data.ExtraTax || '';
        tr.querySelector('.item-fed').value = data.fedPayable || data.FedPayable || '';
        tr.querySelector('.item-sro-serial').value = data.sroItemSerialNo || data.SroItemSerialNo || '';
        tr.querySelector('.item-further-tax').value = data.furtherTax || data.FurtherTax || '';
        tr.querySelector('.item-discount').value = data.discount || data.Discount || '';

        bindRowEvents(tr);
        updateGrandTotal();
    };

    window.removeInvoiceItemRow = function (btn) {
        var tbody = document.querySelector('#invoiceItemTable tbody');
        if (tbody.querySelectorAll('tr').length <= 1) return;
        btn.closest('tr').remove();
        updateGrandTotal();
    };

    function readItemRows() {
        return Array.from(document.querySelectorAll('#invoiceItemTable tbody tr')).map(function (tr) {
            return {
                productID: parseInt(tr.querySelector('.item-product').value, 10) || 0,
                itemID: tr.querySelector('.item-item-id').value.trim(),
                hsCode: tr.querySelector('.item-hs-code').value.trim(),
                productName: tr.querySelector('.item-product-name').value.trim(),
                qty: tr.querySelector('.item-qty').value,
                unitOfMeasure: tr.querySelector('.item-uom').value.trim(),
                unitPrice: tr.querySelector('.item-unit-price').value,
                taxAmount: tr.querySelector('.item-tax').value,
                extraTax: tr.querySelector('.item-extra-tax').value,
                fedPayable: tr.querySelector('.item-fed').value,
                salesType: tr.querySelector('.item-sales-type').value,
                sroItemSerialNo: tr.querySelector('.item-sro-serial').value.trim(),
                furtherTax: tr.querySelector('.item-further-tax').value,
                discount: tr.querySelector('.item-discount').value
            };
        }).filter(function (r) {
            return r.productID > 0 || r.productName || r.itemID || r.qty || r.unitPrice;
        });
    }

    window.prepareInvoicePayload = function () {
        var dateEl = document.getElementById('txtInvoiceDate');
        if (!dateEl || !dateEl.value) {
            alert('Please enter invoice date.');
            return false;
        }
        var customerEl = document.getElementById('ddlCustomer');
        if (!customerEl || !customerEl.value) {
            alert('Please select a customer from Customer Master.');
            return false;
        }
        var items = readItemRows();
        if (!items.length) {
            alert('Add at least one invoice line item.');
            return false;
        }
        document.getElementById('ItemsJson').value = JSON.stringify(items);
        updateGrandTotal();
        return true;
    };

    window.loadCustomerDetails = function () {
        var ddl = document.getElementById('ddlCustomer');
        var nameEl = document.getElementById('txtCustomerName');
        var ntnEl = document.getElementById('txtCustomerNTNCNIC');
        var addrEl = document.getElementById('txtCustomerAddress');
        if (!ddl || !ddl.value) return;

        fetch('/InvoiceMaster?handler=CustomerInfo&customerId=' + encodeURIComponent(ddl.value))
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (nameEl && data.name) nameEl.value = data.name;
                if (ntnEl && data.ntnCnic) ntnEl.value = data.ntnCnic;
                if (addrEl && data.address) addrEl.value = data.address;
            })
            .catch(function () { });
    };

    document.addEventListener('DOMContentLoaded', function () {
        products = readJsonScript('productLookupData');
        salesTypes = readJsonScript('salesTypeOptionsData');
        var items = readJsonScript('initialInvoiceItemsData');
        if (!items.length) items = [{}];
        items.forEach(function (d) { addInvoiceItemRow(d); });

        var ddlCustomer = document.getElementById('ddlCustomer');
        if (ddlCustomer) {
            ddlCustomer.addEventListener('change', loadCustomerDetails);
        }
    });
})();
