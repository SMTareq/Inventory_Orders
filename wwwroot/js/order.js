/**
 * order.js - Dynamic Order Form Logic
 * Handles: product loading, row management, auto-pricing, stock validation, total calculation
 */

'use strict';

let products = [];
let rowIndex = 0;

$(document).ready(function () {
    loadProducts();
    updateSubmitState();
});

// ─── Load products from server via AJAX ────────────────────────────────────
function loadProducts() {
    $.get(window.PRODUCTS_URL, function (data) {
        products = data;
        // Add first row after products load
        addRow();
    }).fail(function () {
        showAlert('Failed to load products. Please refresh.', 'danger');
    });
}

// ─── Add a new order item row ────────────────────────────────────────────────
function addRow() {
    if (products.length === 0) {
        showAlert('No products available in stock.', 'warning');
        return;
    }

    const idx = rowIndex++;
    const optionsHtml = products.map(p =>
        `<option value="${p.id}" 
                 data-price="${p.price}" 
                 data-stock="${p.quantityInStock}">
            ${escHtml(p.name)} (${escHtml(p.sku)}) — Stock: ${p.quantityInStock}
         </option>`
    ).join('');

    const rowHtml = `
        <tr id="row-${idx}" class="order-row">
            <td>
                <select name="Items[${idx}].ProductId" 
                        class="form-select product-select" 
                        data-row="${idx}" required>
                    <option value="">— Select Product —</option>
                    ${optionsHtml}
                </select>
            </td>
            <td>
                <input type="number" 
                       name="Items[${idx}].Quantity" 
                       class="form-control qty-input" 
                       data-row="${idx}"
                       min="1" value="1" required />
                <div class="invalid-feedback"></div>
            </td>
            <td>
                <div class="input-group input-group-sm">
                    <span class="input-group-text">$</span>
                    <input type="text" 
                           name="Items[${idx}].UnitPrice"
                           class="form-control unit-price bg-light" 
                           data-row="${idx}"
                           readonly placeholder="—" />
                </div>
            </td>
            <td>
                <span class="line-total fw-semibold text-success">—</span>
            </td>
            <td class="text-center">
                <button type="button" class="btn btn-sm btn-outline-danger remove-row" data-row="${idx}">
                    <i class="bi bi-x-lg"></i>
                </button>
            </td>
        </tr>`;

    $('#orderItemsBody').append(rowHtml);
    updateSubmitState();
}

// ─── Event Delegation ────────────────────────────────────────────────────────

// Add row button
$(document).on('click', '#addRowBtn', function () {
    addRow();
});

// Product selected → auto-fill price
$(document).on('change', '.product-select', function () {
    const row = $(this).data('row');
    const selected = $(this).find(':selected');
    const price = parseFloat(selected.data('price')) || 0;

    $(`input.unit-price[data-row="${row}"]`).val(price > 0 ? price.toFixed(2) : '');
    recalcRow(row);
    validateRow(row);
    recalcTotal();
    updateSubmitState();
});

// Quantity changed
$(document).on('input change', '.qty-input', function () {
    const row = $(this).data('row');
    recalcRow(row);
    validateRow(row);
    recalcTotal();
    updateSubmitState();
});

// Remove row
$(document).on('click', '.remove-row', function () {
    const row = $(this).data('row');
    $(`#row-${row}`).remove();
    recalcTotal();
    updateSubmitState();
});

// Form submit guard
$('#orderForm').on('submit', function (e) {
    if (!validateAllRows()) {
        e.preventDefault();
        showAlert('Please fix the errors before submitting.', 'danger');
    }
});

// ─── Calculation Helpers ─────────────────────────────────────────────────────

function recalcRow(rowIdx) {
    const price = parseFloat($(`input.unit-price[data-row="${rowIdx}"]`).val()) || 0;
    const qty = parseInt($(`input.qty-input[data-row="${rowIdx}"]`).val()) || 0;
    const lineTotal = price * qty;

    $(`#row-${rowIdx} .line-total`).text(
        lineTotal > 0 ? '$' + lineTotal.toFixed(2) : '—'
    );
}

function recalcTotal() {
    let total = 0;
    $('.order-row').each(function () {
        const price = parseFloat($(this).find('.unit-price').val()) || 0;
        const qty = parseInt($(this).find('.qty-input').val()) || 0;
        total += price * qty;
    });
    $('#orderTotal').text('$' + total.toFixed(2));
}

// ─── Stock Validation ────────────────────────────────────────────────────────

function validateRow(rowIdx) {
    const selectEl = $(`select.product-select[data-row="${rowIdx}"]`);
    const qtyEl = $(`input.qty-input[data-row="${rowIdx}"]`);
    const stock = parseInt(selectEl.find(':selected').data('stock')) || 0;
    const qty = parseInt(qtyEl.val()) || 0;
    const productId = selectEl.val();

    qtyEl.removeClass('is-invalid is-valid');

    if (!productId || qty < 1) {
        qtyEl.addClass('is-invalid');
        qtyEl.siblings('.invalid-feedback').text('Quantity must be at least 1.');
        return false;
    }

    if (qty > stock) {
        qtyEl.addClass('is-invalid');
        qtyEl.siblings('.invalid-feedback').text(`Only ${stock} in stock.`);
        return false;
    }

    qtyEl.addClass('is-valid');
    qtyEl.siblings('.invalid-feedback').text('');
    return true;
}

function validateAllRows() {
    let valid = true;
    let duplicates = findDuplicateProducts();

    if (duplicates.length > 0) {
        showAlert(`Duplicate products detected: ${duplicates.join(', ')}. Merge them into one row.`, 'warning');
        valid = false;
    }

    $('.order-row').each(function () {
        const row = $(this).find('.product-select').data('row');
        if (!validateRow(row)) valid = false;
    });

    return valid;
}

function findDuplicateProducts() {
    const seen = {};
    const dupes = [];
    $('.product-select').each(function () {
        const val = $(this).val();
        const name = $(this).find(':selected').text().split('—')[0].trim();
        if (val && seen[val]) dupes.push(name);
        seen[val] = true;
    });
    return dupes;
}

// ─── UI Helpers ──────────────────────────────────────────────────────────────

function updateSubmitState() {
    const hasRows = $('.order-row').length > 0;
    const hasProducts = $('.product-select').filter(function () {
        return $(this).val() !== '';
    }).length > 0;

    $('#submitBtn').prop('disabled', !hasRows || !hasProducts);
}

function showAlert(msg, type = 'warning') {
    $('#stockWarning')
        .removeClass('d-none alert-warning alert-danger alert-info')
        .addClass(`alert-${type} d-block`);
    $('#stockWarningText').text(msg);

    setTimeout(() => $('#stockWarning').addClass('d-none'), 5000);
}

function escHtml(str) {
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}
