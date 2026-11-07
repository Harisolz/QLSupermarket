
document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("productFilterForm");
    const container = document.getElementById("product-list");

    if (!form || !container) return;

    let activeController = null;

    // Tải danh sách sản phẩm bằng AJAX
    async function loadProducts(url, updateHistory = true) {
        if (activeController) {
            activeController.abort();
        }

        const requestController = new AbortController();
        activeController = requestController;

        container.classList.add("products-loading");
        container.setAttribute("aria-busy", "true");

        try {
            const response = await fetch(url, {
                method: "GET",
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                },
                signal: requestController.signal
            });

            if (!response.ok) {
                throw new Error("HTTP " + response.status);
            }

            const html = await response.text();

            // Chỉ cập nhật bảng, không reload trang
            container.innerHTML = html;

            // Đổi URL trên trình duyệt
            if (updateHistory) {
                window.history.pushState({}, "", url);
            }

            // Không dùng scrollTo()
            // Giữ nguyên vị trí cuộn hiện tại

        } catch (error) {
            if (error.name === "AbortError") return;

            console.error("Lỗi AJAX:", error);

            const oldAlert = document.getElementById(
                "productsAjaxError"
            );

            if (oldAlert) oldAlert.remove();

            const alert = document.createElement("div");
            alert.id = "productsAjaxError";
            alert.className = "alert alert-danger mt-3";
            alert.textContent =
                "Không thể tải sản phẩm. Vui lòng thử lại.";

            container.prepend(alert);
        } finally {
            if (activeController === requestController) {
                activeController = null;
                container.classList.remove("products-loading");
                container.removeAttribute("aria-busy");
            }
        }
    }

    // Tìm kiếm và lọc bằng AJAX
    form.addEventListener("submit", (event) => {
        event.preventDefault();

        const formData = new FormData(form);
        const params = new URLSearchParams();

        for (const [key, value] of formData.entries()) {
            const text = String(value).trim();

            if (text !== "") {
                params.set(key, text);
            }
        }

        params.set("page", "1");

        const url = new URL(form.action, location.origin);
        url.search = params.toString();

        loadProducts(url.toString());
    });

    // Phân trang bằng AJAX
    container.addEventListener("click", (event) => {
        const link = event.target.closest(
            ".pagination a.page-link"
        );

        if (!link) return;

        event.preventDefault();

        const pageItem = link.closest(".page-item");

        if (
            pageItem?.classList.contains("disabled") ||
            pageItem?.classList.contains("active")
        ) {
            return;
        }

        loadProducts(link.href);
    });

    // Nút đặt lại bộ lọc cũng dùng AJAX
    const resetButton = document.querySelector(
        ".products-page .btn-reset"
    );

    if (resetButton) {
        resetButton.addEventListener("click", (event) => {
            event.preventDefault();

            form.reset();

            // Xóa cả các giá trị có sẵn trong form
            for (const element of form.elements) {
                if (
                    element.name &&
                    (element.tagName === "INPUT" ||
                        element.tagName === "SELECT")
                ) {
                    element.value = "";
                }
            }

            loadProducts(resetButton.href);
        });
    }

    // Hỗ trợ nút Back / Forward
    window.addEventListener("popstate", () => {
        const params = new URLSearchParams(location.search);

        for (const element of form.elements) {
            if (!element.name) continue;

            element.value = params.get(element.name) ?? "";
        }

        loadProducts(location.href, false);
    });
});
