
document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("createOrderForm");
    const tbody = document.getElementById("productRows");
    const addButton = document.getElementById("addProductBtn");
    const template = document.getElementById("productRowTemplate");
    const totalQuantity = document.getElementById("totalQuantity");
    const grandTotal = document.getElementById("grandTotal");
    const warning = document.getElementById("stockWarning");
    const saveButton = document.getElementById("saveOrderBtn");

    if (!form || !tbody || !template) return;

    const money = new Intl.NumberFormat("vi-VN", {
        style: "currency",
        currency: "VND",
        maximumFractionDigits: 0
    });

    function reindexRows() {
        const rows = tbody.querySelectorAll(".product-row");

        rows.forEach((row, index) => {
            const product = row.querySelector(".product-select");
            const quantity = row.querySelector(".quantity-input");

            product.name = `OrderDetails[${index}].ProductId`;
            quantity.name = `OrderDetails[${index}].Quantity`;
        });
    }

    function calculateTotal() {
        let quantitySum = 0;
        let amountSum = 0;
        let errors = [];

        const selectedProducts = new Map();

        const rows = tbody.querySelectorAll(".product-row");

        rows.forEach(row => {
            const select = row.querySelector(".product-select");
            const quantityInput = row.querySelector(".quantity-input");
            const option = select.selectedOptions[0];

            const price = Number(option?.dataset.price || 0);
            const stock = Number(option?.dataset.stock || 0);
            const quantity = Number(quantityInput.value || 0);

            const unitPrice = row.querySelector(".unit-price");
            const lineTotal = row.querySelector(".line-total");

            unitPrice.textContent = money.format(price);

            const validQuantity =
                Number.isInteger(quantity) && quantity > 0;

            const lineAmount = validQuantity
                ? price * quantity
                : 0;

            lineTotal.textContent = money.format(lineAmount);

            if (!select.value) {
                return;
            }

            if (!validQuantity) {
                errors.push("Số lượng phải là số nguyên lớn hơn 0.");
                return;
            }

            quantitySum += quantity;
            amountSum += lineAmount;

            const current = selectedProducts.get(select.value) || {
                name: option.textContent.trim(),
                quantity: 0,
                stock: stock
            };

            current.quantity += quantity;
            selectedProducts.set(select.value, current);
        });

        for (const product of selectedProducts.values()) {
            if (product.quantity > product.stock) {
                errors.push(
                    `${product.name}: chỉ còn ${product.stock} sản phẩm trong kho.`
                );
            }
        }

        if (rows.length === 0) {
            errors.push("Vui lòng thêm ít nhất một sản phẩm.");
        }

        totalQuantity.textContent = quantitySum;
        grandTotal.textContent = money.format(amountSum);

        warning.textContent = errors.join(" ");
        saveButton.disabled = errors.length > 0;

        return errors.length === 0;
    }

    function addRow() {
        const fragment = template.content.cloneNode(true);
        tbody.appendChild(fragment);

        reindexRows();
        calculateTotal();
    }

    addButton.addEventListener("click", addRow);

    tbody.addEventListener("click", function (event) {
        const removeButton = event.target.closest(
            ".btn-remove-product"
        );

        if (!removeButton) return;

        const row = removeButton.closest(".product-row");

        row.remove();

        reindexRows();
        calculateTotal();
    });

    tbody.addEventListener("change", calculateTotal);
    tbody.addEventListener("input", calculateTotal);

    form.addEventListener("submit", function (event) {
        reindexRows();

        if (!calculateTotal()) {
            event.preventDefault();
            warning.scrollIntoView({
                behavior: "smooth",
                block: "center"
            });
        }
    });

    reindexRows();
    calculateTotal();
});
