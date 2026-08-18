(function () {
    "use strict";

    if (typeof abp === "undefined") {
        return;
    }

    // wwwroot/libs/abp/core/abp.js ships abp.notify.*/abp.message.* as unimplemented stubs -
    // abp.notify.success just logs "not implemented!" with zero UI, abp.message.confirm/alert
    // are bare confirm()/alert(). Nothing in application code called these before this file
    // existed. SweetAlert2 is vendored on disk (wwwroot/libs/sweetalert2, pulled in transitively
    // via the LeptonXLite theme's own npm deps) but was never wired to anything - this is the
    // first real implementation. Loaded after abp.js and before leitor-layout.js in
    // ErpModule.ConfigureBundles so the reassignments below land after the stubs are defined and
    // everything else in the app can rely on them being real.

    function uxConfig(name, fallback) {
        var el = document.getElementById("leitor-ux-config");
        var value = el && el.getAttribute("data-" + name);
        return value || fallback;
    }

    function swalReady() {
        return typeof Swal !== "undefined";
    }

    var ICONS = { success: "success", info: "info", warn: "warning", error: "error" };

    function makeToast() {
        return Swal.mixin({
            toast: true,
            position: "top-end",
            showConfirmButton: false,
            timer: 4500,
            timerProgressBar: true,
            customClass: { popup: "leitor-toast" }
        });
    }

    function notify(kind) {
        return function (message) {
            if (!swalReady()) {
                return;
            }
            makeToast().fire({ icon: ICONS[kind], title: message });
        };
    }

    abp.notify.success = notify("success");
    abp.notify.info = notify("info");
    abp.notify.warn = notify("warn");
    abp.notify.error = notify("error");

    // Signature kept identical to abp's own stub (message, titleOrCallback, callback) - see
    // abp.js ~lines 306-341 - so any caller using the documented ABP contract keeps working.
    function showMessage(kind) {
        return function (message, title) {
            if (!swalReady()) {
                window.alert((title || "") + " " + message);
                return;
            }
            Swal.fire({
                icon: ICONS[kind],
                title: title || undefined,
                text: message,
                confirmButtonText: uxConfig("ok-text", "OK"),
                customClass: { popup: "leitor-swal" }
            });
        };
    }

    abp.message.info = showMessage("info");
    abp.message.success = showMessage("success");
    abp.message.warn = showMessage("warn");
    abp.message.error = showMessage("error");

    abp.message.confirm = function (message, titleOrCallback, callback) {
        var title;
        if (titleOrCallback && typeof titleOrCallback !== "string") {
            callback = titleOrCallback;
            title = undefined;
        } else {
            title = titleOrCallback;
        }

        if (!swalReady()) {
            var result = window.confirm(message);
            callback && callback(result);
            return;
        }

        Swal.fire({
            icon: "warning",
            title: title || uxConfig("confirm-title", "Are you sure?"),
            text: message,
            showCancelButton: true,
            confirmButtonText: uxConfig("ok-text", "OK"),
            cancelButtonText: uxConfig("cancel-text", "Cancel"),
            reverseButtons: true,
            customClass: { popup: "leitor-swal leitor-swal-confirm" }
        }).then(function (swalResult) {
            callback && callback(swalResult.isConfirmed);
        });
    };

    // data-confirm="<message>" [data-confirm-variant="danger|warning"] - progressive-enhancement
    // replacement for the inline onsubmit="return confirm('...')" pattern used on every
    // delete/status-change form in the app. One delegated listener covers all of them; re-entrancy
    // is guarded by a "confirmed" flag set on the form right before the programmatic re-submit, so
    // the second, real submit isn't caught by this same check again.
    function initConfirmForms() {
        // Capture phase (the `true` below), not the default bubble phase: this must run and
        // potentially cancel the event before it ever reaches the theme's own jQuery submit
        // handler (LeptonXLite.Global.*.js), regardless of script/registration order (jQuery's
        // .on()/delegated handlers are bubble-phase by default, so a capture-phase listener always
        // runs first). That handler appears to set up some one-shot state expecting a single,
        // uninterrupted submission - a real production bug: seeing this same logical submission
        // twice (once cancelled here, once for real via the later requestSubmit() below) left it
        // dereferencing an undefined promise, "Cannot read properties of undefined (reading
        // 'done')". The old inline onsubmit="return confirm(...)" never hit this because a
        // cancelled confirm() was simply never delivered a second time. stopPropagation on this
        // first, cancelled attempt keeps the theme's handler from ever seeing it at all; the later
        // requestSubmit() below fires a completely fresh, un-intercepted event on confirm, so the
        // theme's handler only ever sees the exact single-delivery shape it always did for a
        // "user confirmed" submission.
        document.addEventListener("submit", function (e) {
            var form = e.target;
            if (!(form instanceof HTMLFormElement) || !form.hasAttribute("data-confirm")) {
                return;
            }
            if (form.dataset.confirmed === "1") {
                return;
            }
            e.preventDefault();
            e.stopPropagation();

            var message = form.getAttribute("data-confirm");
            var variant = form.getAttribute("data-confirm-variant") || "warning";

            function proceed() {
                form.dataset.confirmed = "1";
                if (form.requestSubmit) {
                    form.requestSubmit();
                } else {
                    form.submit();
                }
            }

            if (!swalReady()) {
                if (window.confirm(message)) {
                    proceed();
                }
                return;
            }

            Swal.fire({
                icon: variant === "danger" ? "error" : "warning",
                title: uxConfig("confirm-title", "Are you sure?"),
                text: message,
                showCancelButton: true,
                confirmButtonText: uxConfig("ok-text", "OK"),
                cancelButtonText: uxConfig("cancel-text", "Cancel"),
                reverseButtons: true,
                confirmButtonColor: variant === "danger" ? "var(--leitor-danger)" : undefined,
                customClass: { popup: "leitor-swal leitor-swal-confirm" }
            }).then(function (result) {
                if (result.isConfirmed) {
                    proceed();
                }
            });
        }, true);
    }

    // Anti-double-submit + "Saving..." feedback for every standard form post in the app. Runs
    // after any data-confirm gate above has already let the real submit through (preventDefault
    // there stops this listener from ever seeing that first, cancelled event as a live submit).
    // Skips forms inside the overlay modal - wwwroot/leitor-layout.js's bindFormSubmit() already
    // shows its own loading state and does its own fetch-based submit, so wiring this in too would
    // fight it. A disabled/relabelled button needs no explicit reset: a validation-failure
    // redisplay or a PRG redirect both load a fresh page with a fresh, enabled button.
    function initSubmitLoadingState() {
        document.addEventListener("submit", function (e) {
            var form = e.target;
            if (!(form instanceof HTMLFormElement) || e.defaultPrevented) {
                return;
            }
            if (form.closest("#leitor-overlay-modal") || form.hasAttribute("data-no-loading")) {
                return;
            }
            var button = form.querySelector("button[type=submit]:not([disabled])") ||
                form.querySelector("input[type=submit]:not([disabled])");
            if (!button) {
                return;
            }
            button.disabled = true;
            var loadingText = button.getAttribute("data-loading-text") || uxConfig("saving-text", "Saving...");
            if (button.tagName === "INPUT") {
                button.value = loadingText;
            } else {
                button.textContent = loadingText;
            }
        });
    }

    // TempData-driven success/error toast on page load - server side counterpart is
    // Pages/Shared/PageModelExtensions.SetSuccessMessage/SetErrorMessage, rendered into
    // #leitor-ux-config's data attributes by Components/StatusToast. TempData is read once
    // server-side when the view component renders, so there's nothing to clear here.
    function initStatusToast() {
        var el = document.getElementById("leitor-ux-config");
        var message = el && el.getAttribute("data-status-message");
        if (!message) {
            return;
        }
        var type = el.getAttribute("data-status-type") || "success";
        (abp.notify[type] || abp.notify.success)(message);
    }

    function init() {
        initConfirmForms();
        initSubmitLoadingState();
        initStatusToast();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
