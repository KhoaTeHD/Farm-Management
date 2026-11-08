document.addEventListener("DOMContentLoaded", () => {
  const date = document.getElementById("dateSelector");
  date?.addEventListener("change", () => {
    if (date.value && date.checkValidity())
      window.location.assign(
        "/Attendance?date=" + encodeURIComponent(date.value),
      );
  });
  const all = document.getElementById("selectAll");
  const rows = [...document.querySelectorAll(".worker-row")];
  function update() {
    let count = 0;
    rows.forEach((row) => {
      const selected = row.querySelector(".worker-checkbox").checked;
      row
        .querySelectorAll('input[name="hours"], input[name="notes"]')
        .forEach((input) => {
          input.disabled = !selected;
        });
      row.classList.toggle("row-selected", selected);
      if (selected) count++;
    });
    if (all) {
      all.checked = count === rows.length;
      all.indeterminate = count > 0 && count < rows.length;
    }
    const button = document.getElementById("submitBtn");
    if (button) button.disabled = count === 0;
    const label = document.getElementById("selectedCount");
    if (label) label.textContent = count;
  }
  all?.addEventListener("change", () => {
    rows.forEach((row) => {
      row.querySelector(".worker-checkbox").checked = all.checked;
    });
    update();
  });
  rows.forEach((row) => {
    row.querySelector(".worker-checkbox").addEventListener("change", update);
    row.querySelector(".hours-input").addEventListener("input", (event) => {
      const h = Number(event.target.value);
      const valid =
        event.target.value !== "" && h >= 0 && h <= 24 && h % 0.5 === 0;
      row.querySelector(".work-type-badge").textContent = !valid
        ? "Kiểm tra"
        : h >= 8
          ? "1 công"
          : h === 4
            ? "0,5 công"
            : h + " giờ lẻ";
    });
  });
  update();
  window.addEventListener("pageshow", update);
});
