document.addEventListener("DOMContentLoaded", () => {
  const type = document.getElementById("PlantTypeId"),
    seed = document.getElementById("SeedId");
  if (!type || !seed) return;
  function filter(clear) {
    for (const option of seed.options) {
      if (!option.value) continue;
      const match = option.dataset.type === type.value;
      option.disabled = !match;
      option.hidden = !match;
    }
    if (clear && seed.selectedOptions[0]?.disabled) seed.value = "";
    document.getElementById("seed-help").textContent = type.value
      ? "Chọn giống thuộc loại cây đã chọn."
      : "Chọn loại cây trước để xem các giống phù hợp.";
  }
  type.addEventListener("change", () => filter(true));
  filter(false);
  document.getElementById("estimate-harvest").addEventListener("click", () => {
    const date = document.getElementById("PlantingDate").value;
    const days = Number(type.selectedOptions[0]?.dataset.cycle);
    if (!date || days < 1 || days > 36500 || !Number.isInteger(days)) {
      window.FarmUI.notify(
        "Cần có ngày trồng và chu kỳ sinh trưởng hợp lệ của loại cây.",
        "error",
      );
      return;
    }
    const d = new Date(date + "T00:00:00Z");
    d.setUTCDate(d.getUTCDate() + days);
    if (d.getUTCFullYear() > 9998) {
      window.FarmUI.notify("Ngày dự kiến vượt giới hạn.", "error");
      return;
    }
    document.getElementById("ExpectedHarvestDate").value = d
      .toISOString()
      .slice(0, 10);
  });
});
