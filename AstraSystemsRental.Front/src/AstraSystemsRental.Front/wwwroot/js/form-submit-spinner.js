(function () {
    document.querySelectorAll("form[data-astra-submit-spinner]").forEach(function (form) {
        form.addEventListener("submit", function () {
            var submitButton = form.querySelector('button[type="submit"]');
            if (!submitButton) return;

            submitButton.disabled = true;
            var icon = submitButton.querySelector(".astra-submit-spinner-icon");
            if (icon) icon.classList.remove("hidden");
        });
    });
})();
