(() => {
  const kind = document.querySelector("#journal-kind");
  if (!kind) return;

  const plantSelect = document.querySelector("#PlantId");
  const materialSelect = document.querySelector("#MaterialItemId");
  const materialHint = document.querySelector("#material-required-hint");
  const unitInput = document.querySelector("#Unit");
  const planSelect = document.querySelector("#CareStepId");
  const workerSearch = document.querySelector("#worker-picker-search");
  const workerCount = document.querySelector("#worker-selected-count");
  const workerError = document.querySelector("#worker-picker-error");
  const form = document.querySelector("#daily-work-form");

  const allowedCategories = {
    Fertilize: ["Fertilizer", "Other"],
    Pesticide: ["Pesticide", "Other"],
    Care: ["Fertilizer", "Pesticide", "Other"],
  };

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

    if (materialSelect) {
      const cats = allowedCategories[kind.value] || [];
      [...materialSelect.options].forEach((opt) => {
        if (!opt.value) {
          opt.disabled = false;
          opt.hidden = false;
          return;
        }
        const ok = cats.includes(opt.dataset.category);
        opt.disabled = !ok;
        opt.hidden = !ok;
      });
      if (materialSelect.selectedOptions[0]?.disabled) {
        materialSelect.value = "";
      }
      const needsMaterial =
        ["Fertilize", "Pesticide"].includes(kind.value) &&
        materialSelect.dataset.hasLegacy !== "true";
      materialSelect.required = needsMaterial;
      if (materialHint) {
        materialHint.textContent = ["Fertilize", "Pesticide"].includes(
          kind.value,
        )
          ? "(bắt buộc)"
          : "(tùy chọn)";
      }
    }

    if (planSelect) {
      const selectedPlantId = plantSelect ? plantSelect.value : "";
      [...planSelect.options].forEach((option) => {
        if (!option.value) {
          option.disabled = false;
          option.hidden = false;
          return;
        }
        const kindMatches = option.dataset.kind === kind.value;
        const plantMatches =
          !selectedPlantId ||
          !option.dataset.plantId ||
          option.dataset.plantId === selectedPlantId;
        option.disabled = !(kindMatches && plantMatches);
        option.hidden = option.disabled;
      });
      if (planSelect.selectedOptions[0]?.disabled) {
        planSelect.value = "";
      }
    }
  };

  const applyDefaultUnit = () => {
    if (!materialSelect || !unitInput) return;
    const defaultUnit =
      materialSelect.selectedOptions[0]?.dataset.defaultUnit || "";
    if (
      defaultUnit &&
      (!unitInput.value.trim() || unitInput.dataset.autoFilled === "true")
    ) {
      unitInput.value = defaultUnit;
      unitInput.dataset.autoFilled = "true";
    }
  };

  const updateWorkers = () => {
    const checkboxes = document.querySelectorAll('input[name="WorkerIds"]');
    let count = 0;
    checkboxes.forEach((cb) => {
      const label = cb.closest("[data-worker-option]");
      if (cb.checked) {
        count++;
        label?.classList.add("is-selected");
      } else {
        label?.classList.remove("is-selected");
      }
    });
    if (workerCount) {
      workerCount.textContent = `Đã chọn: ${count} công nhân`;
      workerCount.className =
        count > 0 ? "badge bg-success" : "badge bg-secondary";
    }
    if (workerError && count > 0) {
      workerError.hidden = true;
    }
  };

  if (workerSearch) {
    workerSearch.addEventListener("input", () => {
      const term = workerSearch.value.trim().toLowerCase();
      document.querySelectorAll("[data-worker-option]").forEach((opt) => {
        const text = (opt.dataset.search || "").toLowerCase();
        opt.hidden = !!term && !text.includes(term);
      });
    });
  }

  document
    .querySelectorAll('input[name="WorkerIds"]')
    .forEach((cb) => cb.addEventListener("change", updateWorkers));

  if (unitInput) {
    unitInput.addEventListener("input", () => {
      unitInput.dataset.autoFilled = "false";
    });
  }

  kind.addEventListener("change", update);
  plantSelect?.addEventListener("change", update);
  materialSelect?.addEventListener("change", () => {
    update();
    applyDefaultUnit();
  });

  if (form) {
    form.addEventListener("submit", (event) => {
      const checkedCount = form.querySelectorAll(
        'input[name="WorkerIds"]:checked',
      ).length;
      if (checkedCount === 0) {
        event.preventDefault();
        if (workerError) {
          workerError.hidden = false;
          workerError.scrollIntoView({ behavior: "smooth", block: "center" });
        }
      }
    });
  }

  update();
  updateWorkers();
})();
