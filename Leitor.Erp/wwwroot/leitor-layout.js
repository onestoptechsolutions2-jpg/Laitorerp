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

    // Loads a Create/Edit Razor Page into the overlay modal (Pages/Shared/Components/FormOverlay)
    // instead of navigating to it - the target page renders completely normally (same route, same
    // PageModel, same validation), it just gets Layout = null and returns only its own markup
    // when it sees the X-Requested-With header this sets (see Pages/Shared/OverlayRequest.cs).
    // Falls back to a real navigation with no JS changes needed if fetch throws for any reason -
    // every trigger is a real <a href> to the real page first, an overlay hook second.
    function initFormOverlay() {
        var modalEl = document.getElementById("leitor-overlay-modal");
        if (!modalEl || typeof bootstrap === "undefined") {
            return;
        }

        var titleEl = modalEl.querySelector("[data-leitor-overlay-title]");
        var bodyEl = modalEl.querySelector("[data-leitor-overlay-body]");
        var modal = new bootstrap.Modal(modalEl);
        var xhrHeader = { "X-Requested-With": "XMLHttpRequest" };

        function showLoading() {
            bodyEl.innerHTML =
                '<div class="leitor-overlay-loading">' +
                '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span>' +
                "<span>" + (modalEl.getAttribute("data-loading-text") || "Loading...") + "</span>" +
                "</div>";
        }

        function renderFragment(html) {
            bodyEl.innerHTML = html;
            bindFormSubmit();
        }

        function bindFormSubmit() {
            var form = bodyEl.querySelector("form");
            if (!form) {
                return;
            }
            form.addEventListener("submit", function (e) {
                e.preventDefault();
                var submitter = e.submitter;
                var formData = new FormData(form);
                if (submitter && submitter.name) {
                    formData.append(submitter.name, submitter.value || "");
                }

                fetch(form.getAttribute("action") || window.location.href, {
                    method: "POST",
                    headers: xhrHeader,
                    body: formData
                })
                    .then(function (response) {
                        var contentType = response.headers.get("content-type") || "";
                        if (contentType.indexOf("application/json") !== -1) {
                            return response.json().then(function (data) {
                                if (data.redirectUrl) {
                                    window.location.href = data.redirectUrl;
                                }
                            });
                        }
                        return response.text().then(renderFragment);
                    })
                    .catch(function () {
                        form.submit();
                    });
            });
        }

        function openOverlay(url, title) {
            titleEl.textContent = title || "";
            showLoading();
            modal.show();

            fetch(url, { headers: xhrHeader })
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error("overlay fetch failed");
                    }
                    return response.text();
                })
                .then(renderFragment)
                .catch(function () {
                    modal.hide();
                    window.location.href = url;
                });
        }

        document.addEventListener("click", function (e) {
            var trigger = e.target.closest("[data-overlay]");
            if (!trigger) {
                return;
            }
            e.preventDefault();
            openOverlay(trigger.getAttribute("href"), trigger.getAttribute("data-overlay-title"));
        });

        // A successful create/update inside the overlay navigates the whole page to the returned
        // redirectUrl (see the JSON branch above) - nothing to re-bind on modal close, but reset
        // the body back to a loading state so a stale form never flashes on the next open.
        modalEl.addEventListener("hidden.bs.modal", function () {
            bodyEl.innerHTML = "";
        });
    }

    // Floating global search (Pages/Shared/Components/GlobalSearch) - fetches
    // Pages/Search/Index.cshtml.cs's JSON-only OnGetAsync as the user types (debounced), and
    // renders results grouped by entity type. Every result is a real <a href> to that record's
    // own Detail page - clicking one is a normal navigation, nothing to intercept.
    function initGlobalSearch() {
        var trigger = document.getElementById("leitor-global-search-trigger");
        var panel = document.getElementById("leitor-global-search-panel");
        var backdrop = document.getElementById("leitor-global-search-backdrop");
        var input = document.getElementById("leitor-global-search-input");
        var closeBtn = document.getElementById("leitor-global-search-close");
        var resultsEl = document.getElementById("leitor-global-search-results");
        if (!trigger || !panel || !backdrop || !input || !resultsEl) {
            return;
        }

        var debounceTimer = null;
        var hintHtml = resultsEl.innerHTML;

        function escapeHtml(value) {
            var div = document.createElement("div");
            div.textContent = value || "";
            return div.innerHTML;
        }

        function open() {
            panel.hidden = false;
            backdrop.hidden = false;
            trigger.setAttribute("aria-expanded", "true");
            input.focus();
        }

        function close() {
            panel.hidden = true;
            backdrop.hidden = true;
            trigger.setAttribute("aria-expanded", "false");
            input.value = "";
            resultsEl.innerHTML = hintHtml;
        }

        function renderResults(results) {
            if (!results || results.length === 0) {
                resultsEl.innerHTML = '<p class="leitor-search-empty">' +
                    (resultsEl.getAttribute("data-no-results-text") || "No results found.") + "</p>";
                return;
            }

            var groups = {};
            var order = [];
            results.forEach(function (item) {
                if (!groups[item.entityType]) {
                    groups[item.entityType] = [];
                    order.push(item.entityType);
                }
                groups[item.entityType].push(item);
            });

            var html = "";
            order.forEach(function (type) {
                html += '<div class="leitor-search-group-label">' + escapeHtml(type) + "</div>";
                groups[type].forEach(function (item) {
                    html += '<a class="leitor-search-result" href="' + escapeHtml(item.url) + '">' +
                        '<div class="leitor-search-result-title">' + escapeHtml(item.title) + "</div>" +
                        (item.subtitle ? '<div class="leitor-search-result-subtitle">' + escapeHtml(item.subtitle) + "</div>" : "") +
                        "</a>";
                });
            });
            resultsEl.innerHTML = html;
        }

        function doSearch(term) {
            if (!term || term.trim().length < 2) {
                resultsEl.innerHTML = hintHtml;
                return;
            }
            fetch("/Search?term=" + encodeURIComponent(term.trim()))
                .then(function (response) {
                    return response.ok ? response.json() : [];
                })
                .then(renderResults)
                .catch(function () {
                    resultsEl.innerHTML = hintHtml;
                });
        }

        trigger.addEventListener("click", open);
        closeBtn && closeBtn.addEventListener("click", close);
        backdrop.addEventListener("click", close);

        input.addEventListener("input", function () {
            clearTimeout(debounceTimer);
            var term = input.value;
            debounceTimer = setTimeout(function () {
                doSearch(term);
            }, 250);
        });

        document.addEventListener("keydown", function (e) {
            var isSearchShortcut = (e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k";
            if (isSearchShortcut) {
                e.preventDefault();
                open();
                return;
            }
            if (e.key === "Escape" && !panel.hidden) {
                close();
            }
        });
    }

    // Deferred to DOMContentLoaded rather than run immediately at parse time: initFormOverlay()
    // looks up #leitor-overlay-modal, which is injected via LayoutHooks.Body.Last (right before
    // </body>) - if this script tag is placed anywhere earlier in the document than that (theme-
    // dependent, and not something this file controls), the lookup would silently fail and every
    // [data-overlay] trigger would fall back to being a plain link with no JS attached. Waiting
    // for DOMContentLoaded is correct regardless of where the script tag actually ends up.
    function init() {
        restoreSidebarCollapseState();
        initActionPanel();
        initFormOverlay();
        initGlobalSearch();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
