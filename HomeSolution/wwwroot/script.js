let emptyGuid = "00000000-0000-0000-0000-000000000000";
let supplierId = null;
let allProducts = null;

let productsContainer = document.getElementById("products-data");
if (productsContainer) {
    supplierId = productsContainer.dataset.supplierid;
    allProducts = JSON.parse(productsContainer.dataset.allproducts);
}


let itemsContainer = document.getElementById("items-container");
if (itemsContainer) {
    itemsContainer.querySelectorAll(".order-item").forEach(calculateLineTotal);
    calculateGrandTotal();
}

document.addEventListener("DOMContentLoaded", function () {

    document.querySelectorAll(".delete-link").forEach(function (btn) {
        btn.addEventListener("click", function (e) {
            if (!confirm("Are you sure you want to delete? This action cannot be undone!")) {
                e.preventDefault();
            }
        });
    });

    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('remove-item-btn')) {
            let items = itemsContainer.querySelectorAll(".order-item");
            if (items.length > 1) e.target.closest('.order-item').remove();
            else {
                let row = e.target.closest('.order-item');
                resetRow(row);
            }
            setTimeout(calculateGrandTotal, 0); // delay so row is removed first
            reindexOrderItems();
        }
        if (e.target.classList.contains('add-item-btn')) {
            const rowIndex = getRowIndex();
            let filteredProducts = allProducts;
            if (productsContainer) {
                filteredProducts = filterProductsBySupplierId(supplierId, allProducts);
            }
            addItemRow(rowIndex, filteredProducts);
            setTimeout(calculateGrandTotal, 0); // delay so row is added first
            reindexOrderItems();
        }

    });

    if (itemsContainer != null) {
        itemsContainer.addEventListener("input", (e) => {
            if (!e.target.matches(".quantity-input, .unit-price-input")) return;
            const row = e.target.closest(".order-item");
            if (row) {
                calculateLineTotal(row);
                calculateGrandTotal();
            }
        });
    }

    var supplierSelect = document.querySelector('.supplier-select');
    if (supplierSelect) {
        supplierSelect.addEventListener("change", function (e) {
            supplierId = e.target.value; // the value of the selected option
            let filteredProducts = filterProductsBySupplierId(supplierId, allProducts);
            loadProducts(filteredProducts);
        });
    }
});

function getRowIndex() {
    const container = document.getElementById("items-container");
    if (!container) return 0;
    return container.querySelectorAll(".order-item").length;
}

function filterProductsBySupplierId(supplierId, products) {
    if (!supplierId) { // the call is from "Create Customer Order View"
        return products.filter(p => p.ProductId != emptyGuid);
    }
    return products.filter(p => (supplierId != emptyGuid && (!supplierId || p.SupplierId == supplierId)));
}

function loadProducts(products) {
    // Update existing product selects in current order items
    document.querySelectorAll('.order-item select[name$=".ProductId"]').forEach(select => {
        const defaultOption = '<option value="">-- please select --</option>';
        select.innerHTML = defaultOption + products
            .map(p => `<option value="${p.ProductId}">${p.Display}</option>`)
            .join('');

        //select.value = "";
    });
}

function addItemRow(rowIndex, products) {
    const container = document.getElementById("items-container");
    if (!container) return;

    // Build options from products array
    const defaultOption = '<option value="" selected>-- please select --</option>';
    const productsOptionsHtml = defaultOption + products.map(p => `<option value="${p.ProductId}">${p.Display}</option>`).join('');
    const div = document.createElement('div');
    div.className = 'row g-2 mb-4 justify-content-between order-item';
    div.innerHTML = `
                    <div class="col-md-1">
                        <input name="Items[${rowIndex}].ItemNumber" type="number" min="1" value="${rowIndex + 1}" class="form-control" disabled/>
                        <input type="hidden" name="Items[${rowIndex}].ItemNumber" value="${rowIndex + 1}" />
                    </div>
                    <div class="col-md-3">
                    <select name="Items[${rowIndex}].ProductId" class="form-select">
                        ${productsOptionsHtml}
                    </select>
                </div>
                <div class="col-md-2">
                    <input name="Items[${rowIndex}].Quantity" type="number" min="1" value="1" class="form-control quantity-input" />
                </div>
                <div class="col-md-2">
                    <input name="Items[${rowIndex}].UnitPrice" type="number" min="0" step="0.01" value="10.00" class="form-control unit-price-input" />
                </div>
                <div class="col-md-2">
                    <input type="number" value="10.00" class="form-control line-total-input" disabled/>
                </div>
                <div class="col-md-2 d-flex align-items-end">
                    <button type="button" class="btn btn-outline-danger w-100 remove-item-btn">Remove</button>
                </div>`;
    container.appendChild(div);
}

function resetRow(row) {
    if (!row) return;

    // Reset Product select
    const productSelect = row.querySelector('select[name$=".ProductId"]');
    if (productSelect) {
        productSelect.selectedIndex = 0; // first option, usually "-- please select --"
    }

    // Reset Quantity input
    const quantityInput = row.querySelector('input[name$=".Quantity"]');
    if (quantityInput) {
        quantityInput.value = 1; // default quantity
    }

    // Reset UnitPrice input
    const unitPriceInput = row.querySelector('input[name$=".UnitPrice"]');
    if (unitPriceInput) {
        unitPriceInput.value = 10.00; // default unit price
    }

    // Optionally reset LineTotal if you have one
    const lineTotalInput = row.querySelector('input[name$=".LineTotal"]');
    if (lineTotalInput) {
        lineTotalInput.value = "0.00";
    }

    // Recalculate totals if needed
    calculateLineTotal(row);
    calculateGrandTotal();
}


function reindexOrderItems() {
    const items = document.querySelectorAll('.order-item');
    items.forEach((it, idx) => {
        it.dataset.index = idx;

        // update all name attributes with [N] index
        it.querySelectorAll('[name]').forEach(el => {
            el.name = el.name.replace(/\[\d+\]/, `[${idx}]`);
        });

        // update id attributes created by Razor (Items_0__FieldName)
        it.querySelectorAll('[id]').forEach(el => {
            el.id = el.id.replace(/_\d+__/, `_${idx}__`);
        });

        // update labels 'for' attributes if any
        it.querySelectorAll('label[for]').forEach(lbl => {
            lbl.htmlFor = lbl.htmlFor.replace(/_\d+__/, `_${idx}__`);
        });

        // update all inputs for ItemNumber (disabled + hidden)
        const itemNumberInputs = it.querySelectorAll('input[name$=".ItemNumber"]');
        itemNumberInputs.forEach(input => {
            input.value = idx + 1;
        });
    });
}

function calculateGrandTotal() {
    const container = document.getElementById("orderItems") || document;
    let sum = 0;
    container.querySelectorAll(".order-item").forEach(row => {
        sum += toNumber(row.querySelector(".line-total-input")?.value || "0");
    });
    const grand = document.getElementById("orderGrandTotal");
    if (grand) grand.value = sum.toFixed(2);
}

function calculateLineTotal(row) {
    const qty = toNumber(row.querySelector(".quantity-input")?.value || "0");
    const price = toNumber(row.querySelector(".unit-price-input")?.value || "0");
    const total = qty * price;
    const totalInput = row.querySelector(".line-total-input");
    if (totalInput) totalInput.value = total.toFixed(2);
}

function toNumber(v) {
    if (typeof v === "number") return isFinite(v) ? v : 0;
    if (v == null) return 0;

    // Keep only digits, separators, and minus; strip currency etc.
    let s = String(v).trim().replace(/[^\d,.\-\u00A0'\s]/g, "");
    // Remove spaces, NBSPs, and apostrophes used as thousand separators
    s = s.replace(/[\s\u00A0']/g, "");

    const hasComma = s.includes(",");
    const hasDot = s.includes(".");

    if (hasComma && hasDot) {
        // If both exist, assume the *last* one is the decimal sep
        const lastComma = s.lastIndexOf(",");
        const lastDot = s.lastIndexOf(".");
        const dec = lastComma > lastDot ? "," : ".";
        const grp = dec === "," ? "." : ",";
        s = s.replace(new RegExp("\\" + grp, "g"), ""); // drop grouping sep
        s = s.replace(dec, ".");                        // normalize decimal
        return parseFloat(s) || 0;
    }

    if (hasComma) {
        // Only comma present: decide if decimal or grouping
        const i = s.lastIndexOf(",");
        const decimals = s.length - i - 1;
        if (decimals === 3 && i > 0) {
            // Likely grouping (e.g., 1,234) → remove commas
            return parseFloat(s.replace(/,/g, "")) || 0;
        }
        return parseFloat(s.replace(",", ".")) || 0; // decimal comma
    }

    if (hasDot) {
        // Only dot present: decide if decimal or grouping
        const i = s.lastIndexOf(".");
        const decimals = s.length - i - 1;
        if (decimals === 3 && i > 0) {
            // Likely grouping (e.g., 1.234) → remove dots
            return parseFloat(s.replace(/\./g, "")) || 0;
        }
        return parseFloat(s) || 0; // decimal dot
    }

    return parseFloat(s) || 0;
}

