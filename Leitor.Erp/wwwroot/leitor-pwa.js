(function () {
    "use strict";

    var DISMISSED_STORAGE_KEY = "leitor:pwa-install-dismissed";
    var MOBILE_QUERY = "(max-width: 767.98px)";

    function isStandalone() {
        return window.matchMedia("(display-mode: standalone)").matches
            || window.navigator.standalone === true; // iOS Safari's own flag, no matchMedia support
    }

    function isDismissed() {
        try {
            return window.localStorage.getItem(DISMISSED_STORAGE_KEY) === "1";
        } catch (e) {
            return false; // storage blocked (private browsing etc.) - fail open, just don't persist
        }
    }

    function markDismissed() {
        try {
            window.localStorage.setItem(DISMISSED_STORAGE_KEY, "1");
        } catch (e) {
            // ignore - nothing to persist to, banner just may reappear next visit
        }
    }

    function registerServiceWorker() {
        if (!("serviceWorker" in navigator)) {
            return;
        }
        window.addEventListener("load", function () {
            navigator.serviceWorker.register("/service-worker.js").catch(function () {
                // Installability degrades gracefully without one - not worth surfacing to the user.
            });
        });
    }

    function isIosSafari() {
        var ua = window.navigator.userAgent;
        var isIos = /iPad|iPhone|iPod/.test(ua) && !window.MSStream;
        var isSafari = /Safari/.test(ua) && !/CriOS|FxiOS|EdgiOS|OPiOS/.test(ua);
        return isIos && isSafari;
    }

    function buildBanner(options) {
        var banner = document.createElement("div");
        banner.id = "leitor-pwa-banner";
        banner.className = "leitor-pwa-banner";
        banner.setAttribute("role", "dialog");
        banner.setAttribute("aria-label", "Install Leitor ERP");

        var icon = document.createElement("img");
        icon.src = "/images/pwa/icon-192.png";
        icon.alt = "";
        icon.className = "leitor-pwa-banner-icon";

        var text = document.createElement("div");
        text.className = "leitor-pwa-banner-text";
        var title = document.createElement("strong");
        title.textContent = "Install Leitor ERP";
        var subtitle = document.createElement("span");
        subtitle.textContent = options.subtitle;
        text.appendChild(title);
        text.appendChild(subtitle);

        var actions = document.createElement("div");
        actions.className = "leitor-pwa-banner-actions";

        var actionBtn = document.createElement("button");
        actionBtn.type = "button";
        actionBtn.className = "leitor-pwa-banner-install";
        actionBtn.textContent = options.actionLabel;

        var dismissBtn = document.createElement("button");
        dismissBtn.type = "button";
        dismissBtn.className = "leitor-pwa-banner-dismiss";
        dismissBtn.setAttribute("aria-label", "Dismiss");
        dismissBtn.innerHTML = '<i class="fas fa-xmark" aria-hidden="true"></i>';

        actions.appendChild(actionBtn);
        actions.appendChild(dismissBtn);

        banner.appendChild(icon);
        banner.appendChild(text);
        banner.appendChild(actions);

        dismissBtn.addEventListener("click", function () {
            markDismissed();
            banner.remove();
        });
        actionBtn.addEventListener("click", options.onAction);

        return banner;
    }

    function showBanner(options) {
        if (document.getElementById("leitor-pwa-banner")) {
            return;
        }
        document.body.appendChild(buildBanner(options));
    }

    function initInstallPrompt() {
        if (isStandalone() || isDismissed() || !window.matchMedia(MOBILE_QUERY).matches) {
            return;
        }

        var deferredPrompt = null;

        // Chromium/Android: the browser tells us installability was met and hands over a
        // prompt() we control instead of showing its own mini-infobar (preventDefault()).
        window.addEventListener("beforeinstallprompt", function (event) {
            event.preventDefault();
            deferredPrompt = event;

            showBanner({
                subtitle: "Add it to your home screen for one-tap access.",
                actionLabel: "Install",
                onAction: function () {
                    var banner = document.getElementById("leitor-pwa-banner");
                    deferredPrompt.prompt();
                    deferredPrompt.userChoice.finally(function () {
                        markDismissed();
                        if (banner) {
                            banner.remove();
                        }
                    });
                }
            });
        });

        window.addEventListener("appinstalled", function () {
            markDismissed();
            var banner = document.getElementById("leitor-pwa-banner");
            if (banner) {
                banner.remove();
            }
        });

        // iOS Safari never fires beforeinstallprompt - there's no programmatic install, only the
        // Share sheet's own "Add to Home Screen", so this is an instructional banner instead of an
        // action button. Delayed slightly so it doesn't compete with the page's first paint.
        if (isIosSafari()) {
            window.setTimeout(function () {
                showBanner({
                    subtitle: 'Tap Share, then "Add to Home Screen".',
                    actionLabel: "Got it",
                    onAction: function () {
                        markDismissed();
                        var banner = document.getElementById("leitor-pwa-banner");
                        if (banner) {
                            banner.remove();
                        }
                    }
                });
            }, 2000);
        }
    }

    registerServiceWorker();

    // Same DOMContentLoaded-guard convention as leitor-layout.js's own init() - showBanner()
    // touches document.body, and script tag placement relative to it is theme-dependent.
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initInstallPrompt);
    } else {
        initInstallPrompt();
    }
})();
