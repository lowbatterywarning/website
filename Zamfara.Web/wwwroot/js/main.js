/* ============================================================
   School Sites — Main JavaScript
   ============================================================ */

document.addEventListener("DOMContentLoaded", function () {

  // ---- Mobile hamburger toggle (with animated X) ----
  const hamburger = document.querySelector(".hamburger");
  const nav = document.querySelector(".main-nav");

  if (hamburger && nav) {
    hamburger.addEventListener("click", function () {
      hamburger.classList.toggle("open");
      nav.classList.toggle("open");
    });

    // Close menu when a nav link is clicked (mobile)
    nav.querySelectorAll("a").forEach(function (link) {
      link.addEventListener("click", function () {
        hamburger.classList.remove("open");
        nav.classList.remove("open");
      });
    });

    // Close menu when clicking outside
    document.addEventListener("click", function (e) {
      if (!hamburger.contains(e.target) && !nav.contains(e.target)) {
        hamburger.classList.remove("open");
        nav.classList.remove("open");
      }
    });
  }

  // ---- Highlight current page in nav ----
  // Compare full paths so sub-site routes (e.g. /school-one/about) match.
  var currentPath = window.location.pathname.replace(/\/+$/, "");
  const navLinks = document.querySelectorAll(".main-nav a");

  navLinks.forEach(function (link) {
    let linkPath = "";
    try {
      linkPath = new URL(link.href).pathname.replace(/\/+$/, "");
    } catch (e) {
      linkPath = link.getAttribute("href") || "";
    }
    if (linkPath === currentPath) {
      link.classList.add("active");
    }
  });

  // ---- Smooth scroll for anchor links (already handled by CSS scroll-behavior) ----
  // No additional code needed — CSS `scroll-behavior: smooth` handles this.

});
