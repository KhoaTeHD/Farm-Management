document.addEventListener("DOMContentLoaded", () => {
  const table = document.querySelector(".attendance-calendar");
  if (!table) return;
  const cells = [...table.querySelectorAll(".hours-cell")];
  const status = document.getElementById("calendar-save-status");
  let pending = 0;
  function color(input) {
    const h = input.value === "" ? null : Number(input.value);
    input.classList.remove(
      "cell-full",
      "cell-half",
      "cell-odd",
      "cell-invalid",
    );
    if (h === null) return;
    input.classList.add(
      !Number.isFinite(h) || h < 0 || h > 24 || h % 0.5
        ? "cell-invalid"
        : h >= 8
          ? "cell-full"
          : h === 4
            ? "cell-half"
            : "cell-odd",
    );
  }
  function updateTotals() {
    let totalDays = 0,
      totalOdd = 0;
    table.querySelectorAll("tbody tr").forEach((row) => {
      let days = 0,
        odd = 0;
      row.querySelectorAll(".hours-cell").forEach((input) => {
        const text = input.dataset.savedHours;
        if (!text) return;
        const h = Number(text);
        if (!Number.isFinite(h) || h < 0 || h > 24 || h % 0.5) return;
        if (h >= 8) days += 1;
        else if (h === 4) days += 0.5;
        else odd += h;
      });
      row.querySelector(".worker-total-days").textContent = days;
      row.querySelector(".worker-odd-hours").textContent = odd;
      totalDays += days;
      totalOdd += odd;
    });
    document.getElementById("calendar-total-days").textContent = totalDays;
    document.getElementById("calendar-odd-hours").textContent = totalOdd;
  }
  async function save(input) {
    const original = input.dataset.savedHours || "";
    if (input.value === original || input.dataset.saving) return;
    const hours = input.value === "" ? 0 : Number(input.value);
    if (
      !Number.isFinite(hours) ||
      hours < 0 ||
      hours > 24 ||
      hours % 0.5 ||
      !input.checkValidity()
    ) {
      input.value = original;
      color(input);
      window.FarmUI.notify("Số giờ từ 0 đến 24, theo bước 0,5 giờ.", "error");
      return;
    }
    input.dataset.saving = "true";
    input.readOnly = true;
    input.classList.add("cell-saving");
    input.setAttribute("aria-busy", "true");
    input.nextElementSibling.style.display = "block";
    pending++;
    status.textContent = "Đang lưu…";
    try {
      const payload = new URLSearchParams({
        __RequestVerificationToken: document.querySelector(
          'input[name="__RequestVerificationToken"]',
        ).value,
        workerId: input.dataset.workerId,
        date: input.dataset.date,
        hours: String(hours),
      });
      const response = await fetch(table.dataset.saveUrl, {
        method: "POST",
        body: payload,
        headers: { "X-Requested-With": "XMLHttpRequest" },
      });
      if (!response.ok)
        throw new Error("Không lưu được chấm công. Vui lòng thử lại.");
      const result = await response.json();
      if (!result.success)
        throw new Error(result.message || "Không lưu được chấm công.");
      input.value = hours === 0 ? "" : String(hours);
      input.dataset.savedHours = input.value;
      color(input);
      updateTotals();
    } catch (error) {
      input.value = original;
      color(input);
      window.FarmUI.notify(
        error.message || "Mất kết nối. Vui lòng thử lại.",
        "error",
      );
    } finally {
      delete input.dataset.saving;
      input.readOnly = false;
      input.classList.remove("cell-saving");
      input.removeAttribute("aria-busy");
      input.nextElementSibling.style.display = "none";
      pending--;
      status.textContent = pending > 0 ? "Đang lưu…" : "Đã xử lý xong";
    }
  }
  cells.forEach((input) => {
    input.addEventListener("blur", () => save(input));
    input.addEventListener("input", () => color(input));
    input.addEventListener("keydown", (e) => {
      if (e.key === "Enter") {
        e.preventDefault();
        input.blur();
        return;
      }
      if (!["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown"].includes(e.key))
        return;
      const available = cells.filter(
        (c) => !c.disabled && !c.closest("tr").hidden,
      );
      const index = available.indexOf(input);
      let target;
      if (e.key === "ArrowRight") target = available[index + 1];
      if (e.key === "ArrowLeft") target = available[index - 1];
      if (e.key === "ArrowUp" || e.key === "ArrowDown") {
        const rows = [...table.querySelectorAll("tbody tr")].filter(
          (row) => !row.hidden,
        );
        const rowIndex = rows.indexOf(input.closest("tr"));
        const nextRow = rows[rowIndex + (e.key === "ArrowDown" ? 1 : -1)];
        target = nextRow?.children[
          input.closest("td").cellIndex
        ]?.querySelector("input:not(:disabled)");
      }
      if (target) {
        e.preventDefault();
        target.focus();
        target.select();
      }
    });
  });
  document.getElementById("calendar-search")?.addEventListener("input", (e) => {
    const query = window.FarmUI.normalize(e.target.value);
    const rows = [...table.querySelectorAll("[data-calendar-worker]")];
    let visible = 0;
    rows.forEach((row) => {
      row.hidden = !window.FarmUI.normalize(row.dataset.search).includes(query);
      if (!row.hidden) visible++;
    });
    document.getElementById("calendar-no-results").hidden = visible > 0;
  });
  window.addEventListener("beforeunload", (e) => {
    if (pending > 0) {
      e.preventDefault();
      e.returnValue = "";
    }
  });
});
