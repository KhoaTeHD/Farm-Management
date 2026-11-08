(() => {
  const kind = document.querySelector("#journal-kind");
  if (!kind) return;
  const update = () => {
    document.querySelectorAll("[data-journal-kinds]").forEach((section) => {
      section.hidden = !section.dataset.journalKinds
        .split(" ")
        .includes(kind.value);
    });
    document
      .querySelectorAll(
        "[data-journal-kinds] input,[data-journal-kinds] select,[data-journal-kinds] textarea",
      )
      .forEach((input) => {
        input.disabled = !!input.closest("[data-journal-kinds][hidden]");
      });
    const required = {
      Amount: ["Water", "Fertilize", "Pesticide", "Harvest"],
      Unit: ["Water", "Fertilize", "Pesticide", "Harvest"],
      Material: ["Fertilize", "Pesticide"],
      WithdrawalDays: ["Pesticide"],
      Quality: ["Harvest"],
      GrowthStage: ["Observation"],
      Condition: ["Observation"],
    };
    Object.entries(required).forEach(([id, kinds]) => {
      document.getElementById(id).required = kinds.includes(kind.value);
    });
    const lot = document.querySelector("#InputLotId");
    [...lot.options].forEach((o) => {
      o.disabled = !!o.value && o.dataset.kind !== kind.value;
      o.hidden = o.disabled;
    });
    if (lot.selectedOptions[0]?.disabled) lot.value = "";
    lot.required = ["Fertilize", "Pesticide"].includes(kind.value);
    document.querySelector("#WaterResourceId").required =
      kind.value === "Water";
    if (lot.selectedOptions[0]?.dataset.product)
      document.querySelector("#Material").value =
        lot.selectedOptions[0].dataset.product;
    const unit = document.querySelector("#Unit");
    [...unit.options].forEach((option) => {
      const valid =
        !option.value || option.dataset.kinds?.split(" ").includes(kind.value);
      option.disabled = !valid;
      option.hidden = !valid;
    });
    if (unit.selectedOptions[0]?.disabled) unit.value = "";
    if (kind.value === "Harvest") unit.value = "kg";
    const plan = document.querySelector("#CareStepId");
    [...plan.options].forEach((option) => {
      option.disabled = !!option.value && option.dataset.kind !== kind.value;
      option.hidden = option.disabled;
    });
    if (plan.selectedOptions[0]?.disabled) plan.value = "";
  };
  kind.addEventListener("change", update);
  document.querySelector("#InputLotId").addEventListener("change", update);
  update();
})();
