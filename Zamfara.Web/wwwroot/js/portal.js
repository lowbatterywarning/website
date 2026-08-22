/* ============================================================
   zamfara.org Portal — client-side school directory filter
   (external file: the CSP blocks inline scripts)
   ============================================================ */

document.addEventListener("DOMContentLoaded", function () {
  const input = document.getElementById("school-search");
  const cards = Array.prototype.slice.call(document.querySelectorAll(".portal-card"));
  const empty = document.getElementById("no-results");
  if (!input) return;

  input.addEventListener("input", function () {
    const q = input.value.trim().toLowerCase();
    let shown = 0;
    cards.forEach(function (card) {
      const hay = (card.getAttribute("data-search") || "").toLowerCase();
      const match = !q || hay.indexOf(q) !== -1;
      card.style.display = match ? "" : "none";
      if (match) shown++;
    });
    if (empty) empty.hidden = shown !== 0;
  });
});
