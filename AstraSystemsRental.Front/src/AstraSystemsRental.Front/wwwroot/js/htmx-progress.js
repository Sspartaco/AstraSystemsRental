(function () {
    var bar = document.getElementById("astra-progress");
    if (!bar) return;

    var active = 0;

    document.body.addEventListener("htmx:beforeRequest", function () {
        active++;
        bar.classList.add("htmx-request");
    });

    document.body.addEventListener("htmx:afterRequest", function () {
        active = Math.max(0, active - 1);
        if (active === 0) bar.classList.remove("htmx-request");
    });
})();
