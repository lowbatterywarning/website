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

  // ---- Contact form: validation + Formspree AJAX submission ----
  const contactForm = document.getElementById("contactForm");

  if (contactForm) {
    contactForm.addEventListener("submit", function (e) {
      e.preventDefault();

      // --- DOM refs ---
      const nameField    = contactForm.querySelector("#name");
      const emailField   = contactForm.querySelector("#email");
      const messageField = contactForm.querySelector("#message");
      const submitBtn    = document.getElementById("formSubmitBtn");
      const msgBox       = document.getElementById("formMessage");

      // --- Clear previous state ---
      document.querySelectorAll(".form-error").forEach(function (el) { el.remove(); });
      msgBox.className = "form-message";
      msgBox.textContent = "";

      let valid = true;

      // --- Inline error helper ---
      function showError(field, msg) {
        valid = false;
        var err = document.createElement("small");
        err.className = "form-error";
        err.textContent = msg;
        field.insertAdjacentElement("afterend", err);
        field.setAttribute("aria-invalid", "true");
      }

      function clearError(field) {
        field.removeAttribute("aria-invalid");
      }

      // --- Validation ---
      if (!nameField || nameField.value.trim() === "") {
        showError(nameField, "Please enter your full name.");
      } else {
        clearError(nameField);
      }

      if (!emailField || emailField.value.trim() === "") {
        showError(emailField, "Please enter your email address.");
      } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(emailField.value.trim())) {
        showError(emailField, "Please enter a valid email address (e.g., name@domain.com).");
      } else {
        clearError(emailField);
      }

      if (!messageField || messageField.value.trim() === "") {
        showError(messageField, "Please enter a message.");
      } else if (messageField.value.trim().length < 10) {
        showError(messageField, "Please provide a bit more detail (at least 10 characters).");
      } else {
        clearError(messageField);
      }

      if (!valid) {
        // Focus the first invalid field
        var firstError = contactForm.querySelector(".form-error");
        if (firstError && firstError.previousElementSibling) {
          firstError.previousElementSibling.focus();
        }
        return;
      }

      // --- Submit via AJAX to Formspree ---
      submitBtn.disabled = true;
      submitBtn.textContent = "Sending...";

      var formData = new FormData(contactForm);

      fetch(contactForm.action, {
        method: "POST",
        body: formData,
        headers: { "Accept": "application/json" }
      })
        .then(function (response) {
          if (response.ok) {
            // Success
            msgBox.className = "form-message form-message--success";
            msgBox.textContent = "Thank you for your message! We'll get back to you within 1–2 business days.";
            contactForm.reset();
            // Scroll message into view
            msgBox.scrollIntoView({ behavior: "smooth", block: "center" });
          } else {
            return response.json().then(function (data) {
              throw new Error(data.error || "Something went wrong. Please try again.");
            });
          }
        })
        .catch(function (error) {
          // Network error or server error
          msgBox.className = "form-message form-message--error";
          msgBox.textContent = error.message || "Unable to send your message. Please check your connection and try again, or email us directly at info@school.edu.";
          msgBox.scrollIntoView({ behavior: "smooth", block: "center" });
        })
        .finally(function () {
          submitBtn.disabled = false;
          submitBtn.textContent = "Send Message";
        });
    });
  }

});
