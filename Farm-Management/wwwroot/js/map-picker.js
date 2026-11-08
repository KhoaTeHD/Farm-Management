document.addEventListener("DOMContentLoaded", () => {
  const field = document.querySelector('[name="GpsCoordinates"]');
  const status = document.getElementById("mapStatus");
  const gpsButton = document.getElementById("btnGps");
  const searchButton = document.getElementById("btnSearch");
  if (!window.L) {
    document.getElementById("map").hidden = true;
    gpsButton.disabled = true;
    searchButton.disabled = true;
    status.textContent =
      "Chưa tải được bản đồ. Bạn vẫn có thể nhập tọa độ ở bên dưới.";
    return;
  }
  function parse(text) {
    const parts = (text || "").split(",").map((x) => x.trim());
    if (parts.length !== 2 || parts.some((x) => x === "")) return null;
    const lat = Number(parts[0]),
      lng = Number(parts[1]);
    return Number.isFinite(lat) &&
      Number.isFinite(lng) &&
      Math.abs(lat) <= 90 &&
      Math.abs(lng) <= 180
      ? [lat, lng]
      : null;
  }
  const initial = parse(field.value);
  const map = L.map("map").setView(
    initial || [14.0583, 108.2772],
    initial ? 15 : 5,
  );
  const street = L.tileLayer(
    "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
    { attribution: "© OpenStreetMap", maxZoom: 19 },
  );
  const satellite = L.tileLayer(
    "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
    { attribution: "© Esri", maxZoom: 18 },
  );
  street.addTo(map);
  L.control.layers({ "Bản đồ": street, "Vệ tinh": satellite }).addTo(map);
  let marker;
  function showPoint(point, writeField) {
    if (!marker) {
      marker = L.marker(point, { draggable: true }).addTo(map);
      marker.on("dragend", () => {
        const p = marker.getLatLng();
        showPoint([p.lat, p.lng], true);
      });
    } else marker.setLatLng(point);
    if (writeField) {
      field.value = point.map((n) => Number(n.toFixed(6))).join(", ");
      field.dispatchEvent(new Event("change", { bubbles: true }));
    }
    status.textContent =
      "Đã chọn vị trí: " + point.map((n) => n.toFixed(6)).join(", ");
  }
  if (initial) showPoint(initial, false);
  map.on("click", (event) =>
    showPoint([event.latlng.lat, event.latlng.lng], true),
  );
  function syncManualCoordinates() {
    const p = parse(field.value);
    if (p) {
      showPoint(p, false);
      map.setView(p, 15);
    } else {
      if (marker) {
        map.removeLayer(marker);
        marker = null;
      }
      status.textContent = field.value
        ? "Tọa độ chưa hợp lệ. Dạng nhập: vĩ độ, kinh độ."
        : "Chưa chọn vị trí. Có thể bổ sung sau.";
    }
  }
  let coordinateTimer;
  field.addEventListener("input", () => {
    clearTimeout(coordinateTimer);
    coordinateTimer = setTimeout(syncManualCoordinates, 300);
  });
  field.addEventListener("change", syncManualCoordinates);
  field.addEventListener("blur", syncManualCoordinates);
  const search = document.getElementById("mapSearch"),
    results = document.getElementById("searchResults");
  let request;
  function message(text) {
    results.replaceChildren();
    const item = document.createElement("div");
    item.className = "list-group-item text-muted";
    item.textContent = text;
    results.append(item);
    results.hidden = false;
  }
  async function find() {
    const query = search.value.trim();
    if (!query) return;
    request?.abort();
    request = new AbortController();
    searchButton.disabled = true;
    searchButton.textContent = "Đang tìm…";
    message("Đang tìm địa điểm…");
    try {
      const response = await fetch(
        "https://nominatim.openstreetmap.org/search?format=json&countrycodes=vn&limit=5&q=" +
          encodeURIComponent(query),
        { signal: request.signal, headers: { "Accept-Language": "vi" } },
      );
      if (!response.ok) throw new Error("Search failed");
      const data = await response.json();
      results.replaceChildren();
      if (!data.length) {
        message(
          "Không tìm thấy địa điểm phù hợp. Thử thêm tên quận hoặc tỉnh.",
        );
        return;
      }
      data.forEach((item) => {
        const button = document.createElement("button");
        button.type = "button";
        button.className = "list-group-item list-group-item-action text-start";
        button.textContent = item.display_name;
        button.addEventListener("click", () => {
          const p = [Number(item.lat), Number(item.lon)];
          showPoint(p, true);
          map.setView(p, 15);
          results.hidden = true;
        });
        results.append(button);
      });
      results.hidden = false;
    } catch (error) {
      if (error.name !== "AbortError")
        message(
          "Không tìm được địa điểm lúc này. Bạn có thể chọn trực tiếp trên bản đồ.",
        );
    } finally {
      searchButton.disabled = false;
      searchButton.textContent = "Tìm";
    }
  }
  searchButton.addEventListener("click", find);
  search.addEventListener("keydown", (e) => {
    if (e.key === "Enter") {
      e.preventDefault();
      find();
    }
  });
  document.addEventListener("click", (e) => {
    if (
      !results.contains(e.target) &&
      e.target !== search &&
      e.target !== searchButton
    )
      results.hidden = true;
  });
  gpsButton.addEventListener("click", () => {
    if (!navigator.geolocation) {
      status.textContent =
        "Trình duyệt không hỗ trợ vị trí. Hãy chọn điểm trên bản đồ.";
      return;
    }
    gpsButton.disabled = true;
    gpsButton.textContent = "Đang lấy vị trí…";
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        const p = [pos.coords.latitude, pos.coords.longitude];
        showPoint(p, true);
        map.setView(p, 16);
        gpsButton.disabled = false;
        gpsButton.textContent = "Vị trí hiện tại";
      },
      () => {
        status.textContent =
          "Chưa lấy được vị trí. Bạn có thể chọn trực tiếp trên bản đồ.";
        gpsButton.disabled = false;
        gpsButton.textContent = "Vị trí hiện tại";
      },
      { enableHighAccuracy: true, timeout: 10000 },
    );
  });
});
