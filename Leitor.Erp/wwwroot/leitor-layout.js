(function () {
    "use strict";

    var RIGHT_PANEL_STORAGE_KEY = "leitor:right-panel-collapsed";

    // The shipped Lepton X bundle already *saves* left-sidebar collapse state - its own
    // toggleSidebarHover() toggles a "hover-trigger" class on #lpx-wrapper and writes a
    // `lpx_side-menu-state` cookie set to "1" when collapsed or "" when expanded - but its own
    // loadMenuState() restore method is never called from anywhere else in the bundle - confirmed
    // by inspecting the shipped lepton-x.bundle.min.js, "loadMenuState" appears exactly once, as
    // its own definition. Every full-page navigation (this is server-rendered Razor Pages, not an
    // SPA) therefore silently re-expands the sidebar. This restores the framework's own saved
    // state using its own cookie/value convention, so it never fights the framework's own click
    // handler.
    function restoreSidebarCollapseState() {
        var match = document.cookie.match(/(?:^|; )lpx_side-menu-state=([^;]*)/);
        var wasCollapsed = !!match && match[1] === "1";
        var wrapper = document.querySelector("#lpx-wrapper");
        if (wrapper && wasCollapsed) {
            wrapper.classList.add("hover-trigger");
        }
    }

    function initActionPanel() {
        var panel = document.getElementById("leitor-action-panel");
        var backdrop = document.querySelector("[data-leitor-panel-backdrop]");
        var tab = document.querySelector("[data-leitor-panel-toggle]");
        if (!panel) {
            return;
        }

        function setOpen(open) {
            panel.classList.toggle("is-open", open);
            panel.setAttribute("aria-hidden", open ? "false" : "true");
            if (backdrop) {
                backdrop.classList.toggle("is-open", open);
            }
            if (tab) {
                tab.setAttribute("aria-expanded", open ? "true" : "false");
            }
            document.body.classList.toggle("leitor-panel-open", open);

            if (open) {
                localStorage.removeItem(RIGHT_PANEL_STORAGE_KEY);
            } else {
                localStorage.setItem(RIGHT_PANEL_STORAGE_KEY, "1");
            }
        }

        if (tab) {
            tab.addEventListener("click", function () {
                setOpen(!panel.classList.contains("is-open"));
            });
        }
        document.querySelectorAll("[data-leitor-panel-close]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                setOpen(false);
            });
        });
        if (backdrop) {
            backdrop.addEventListener("click", function () {
                setOpen(false);
            });
        }
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && panel.classList.contains("is-open")) {
                setOpen(false);
            }
        });

        // Open by default (it's the point of the rail) unless the user explicitly closed it on a
        // previous page - narrow-viewport visitors get the overlay CSS's collapsed default look
        // regardless, since .is-open only controls the slide-in transform there, not visibility.
        setOpen(localStorage.getItem(RIGHT_PANEL_STORAGE_KEY) !== "1");
    }

    restoreSidebarCollapseState();
    initActionPanel();
})();
