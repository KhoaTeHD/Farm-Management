document.addEventListener("DOMContentLoaded", () => {
  const data = document.getElementById("worker-salaries");
  const salaries = data ? JSON.parse(data.textContent) || {} : {};
  const worker = document.getElementById("WorkerId");
  const hours = document.getElementById("hoursInput");
  const type = document.getElementById("workTypePreview");
  const preview = document.getElementById("salaryPreview");
  function update() {
    const h = Number(hours.value);
    const valid =
      hours.value !== "" &&
      Number.isFinite(h) &&
      h >= 0 &&
      h <= 24 &&
      h % 0.5 === 0;
    type.textContent = !valid
      ? "Chưa có số giờ hợp lệ"
      : h >= 8
        ? "1 ngày công"
        : h === 4
          ? "0,5 ngày công"
          : h + " giờ lẻ";
    const salary = salaries[worker.value];
    preview.textContent =
      !valid || salary == null || salary <= 0
        ? "Chưa đủ thông tin"
        : new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 0 }).format(
            h >= 8 ? salary : h === 4 ? salary / 2 : (h * salary) / 8,
          ) + " đ";
  }
  worker.addEventListener("change", update);
  hours.addEventListener("input", update);
  update();
});
