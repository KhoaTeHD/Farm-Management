(() => {
  "use strict";
  const normalize = (value) =>
    (value || "")
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .replace(/đ/g, "d")
      .replace(/Đ/g, "D")
      .toLowerCase();
  window.FarmUI = {
    notify(message, type = "success") {
      const region = document.getElementById("toast-region");
      if (!region) return;
      const toast = document.createElement("div");
      toast.className = "farm-toast" + (type === "error" ? " is-error" : "");
      toast.setAttribute("role", type === "error" ? "alert" : "status");
      const text = document.createElement("span");
      text.textContent = message;
      const close = document.createElement("button");
      close.type = "button";
      close.textContent = "×";
      close.setAttribute("aria-label", "Đóng thông báo");
      close.addEventListener("click", () => toast.remove());
      toast.append(text, close);
      region.append(toast);
      while (region.children.length > 3) region.firstElementChild.remove();
      setTimeout(() => toast.remove(), type === "error" ? 10000 : 4500);
    },
    normalize,
  };
  document.addEventListener("DOMContentLoaded", () => {
    const menu = document.getElementById("app-navigation");
    const toggle = document.querySelector(".mobile-menu-toggle");
    const workspace = document.querySelector(".app-workspace");
    const mobile = window.matchMedia("(max-width: 767px)");
    function setMenu(open, restoreFocus = false) {
      document.body.classList.toggle("menu-open", open);
      toggle?.setAttribute("aria-expanded", String(open));
      if (menu) menu.inert = mobile.matches && !open;
      if (workspace) workspace.inert = mobile.matches && open;
      if (open) menu?.querySelector("a")?.focus();
      else if (restoreFocus) toggle?.focus();
    }
    toggle?.addEventListener("click", () =>
      setMenu(!document.body.classList.contains("menu-open")),
    );
    document
      .querySelectorAll("[data-close-menu]")
      .forEach((x) => x.addEventListener("click", () => setMenu(false, true)));
    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape" && document.body.classList.contains("menu-open"))
        setMenu(false, true);
      if (
        e.key === "Tab" &&
        mobile.matches &&
        document.body.classList.contains("menu-open")
      ) {
        const links = [...menu.querySelectorAll("a, button")];
        if (e.shiftKey && document.activeElement === links[0]) {
          e.preventDefault();
          links.at(-1).focus();
        } else if (!e.shiftKey && document.activeElement === links.at(-1)) {
          e.preventDefault();
          links[0].focus();
        }
      }
    });
    mobile.addEventListener("change", () => setMenu(false));
    setMenu(false);
    document.querySelectorAll("[data-list]").forEach((list) => {
      const rows = [...list.querySelectorAll("[data-list-row]")];
      if (!rows.length) return;
      const search = list.querySelector("[data-list-search]");
      const status = list.querySelector("[data-list-status]");
      const size = Number(list.dataset.pageSize || 10);
      let page = 1;
      function update() {
        const query = normalize(search?.value.trim());
        const matched = rows.filter(
          (row) =>
            normalize(row.dataset.search || row.textContent).includes(query) &&
            (!status?.value || row.dataset.status === status.value),
        );
        const pages = Math.max(1, Math.ceil(matched.length / size));
        page = Math.max(1, Math.min(page, pages));
        rows.forEach((row) => {
          row.hidden = true;
        });
        matched.slice((page - 1) * size, page * size).forEach((row) => {
          row.hidden = false;
        });
        const count = list.querySelector("[data-list-count]");
        if (count) {
          count.textContent = matched.length
            ? `${(page - 1) * size + 1}–${Math.min(page * size, matched.length)} / ${matched.length} kết quả`
            : "0 kết quả";
          count.setAttribute("aria-live", "polite");
        }
        const empty = list.querySelector("[data-list-empty]");
        if (empty) empty.hidden = matched.length > 0;
        const pageLabel = list.querySelector("[data-list-page]");
        if (pageLabel) pageLabel.textContent = `${page} / ${pages}`;
        const previous = list.querySelector("[data-list-prev]");
        if (previous) previous.disabled = page === 1;
        const next = list.querySelector("[data-list-next]");
        if (next) next.disabled = page >= pages;
      }
      search?.addEventListener("input", () => {
        page = 1;
        update();
      });
      status?.addEventListener("change", () => {
        page = 1;
        update();
      });
      list.querySelector("[data-list-prev]")?.addEventListener("click", () => {
        page--;
        update();
      });
      list.querySelector("[data-list-next]")?.addEventListener("click", () => {
        page++;
        update();
      });
      update();
    });
    document.querySelectorAll("[data-val-required]").forEach((input) => {
      if (input.type !== "hidden" && input.id)
        document
          .querySelector(`label[for="${CSS.escape(input.id)}"]`)
          ?.classList.add("is-required");
    });
    document.querySelectorAll("form[data-save-form]").forEach((form) => {
      form.addEventListener("submit", (event) => {
        if (window.jQuery?.validator && !window.jQuery(form).valid()) return;
        if (form.dataset.saving === "true") {
          event.preventDefault();
          return;
        }
        form.dataset.saving = "true";
        form.querySelectorAll('button[type="submit"]').forEach((button) => {
          button.disabled = true;
          button.setAttribute("aria-busy", "true");
          button.dataset.originalText = button.textContent;
          button.textContent = "Đang lưu…";
        });
      });
    });
    const invalid = document.querySelector(
      'form .input-validation-error:not([type="hidden"])',
    );
    if (invalid) invalid.focus();
    const salaryInput = document.querySelector("[data-salary-input]");
    function updateSalary() {
      const output = document.querySelector("[data-hourly-preview]");
      if (!output) return;
      const salary = Number(salaryInput?.value);
      output.textContent =
        salaryInput?.value && salary > 0
          ? "Tương đương " +
            new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 0 }).format(
              salary / 8,
            ) +
            " đ / giờ."
          : "";
    }
    salaryInput?.addEventListener("input", updateSalary);
    updateSalary();
    window.addEventListener("pageshow", () => {
      document.querySelectorAll("form[data-save-form]").forEach((form) => {
        delete form.dataset.saving;
        form
          .querySelectorAll("button[data-original-text]")
          .forEach((button) => {
            button.disabled = false;
            button.removeAttribute("aria-busy");
            button.textContent = button.dataset.originalText;
          });
      });
    });
  });
})();
