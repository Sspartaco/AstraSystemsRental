(function () {
    var input = document.getElementById("plate-input");
    var check = document.getElementById("plate-check");
    if (!input) return;

    var valid = /^([A-Z]{3}\d{3}|[A-Z]{3}\d{2}[A-Z]|\d{3,10})$/;

    input.addEventListener("input", function () {
        var clean = input.value.toUpperCase().replace(/[^A-Z0-9]/g, "");
        if (clean !== input.value) input.value = clean;

        input.classList.remove("border-accent/60", "border-danger/60");
        if (check) check.classList.add("hidden");

        if (clean.length === 0) return;

        if (valid.test(clean)) {
            input.classList.add("border-accent/60");
            if (check) check.classList.remove("hidden");
        } else if (clean.length >= 6) {
            input.classList.add("border-danger/60");
        }
    });
})();
