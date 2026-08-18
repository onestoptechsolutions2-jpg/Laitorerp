// Leitor ERP service worker.
//
// Deliberately minimal: this exists so Chromium/Android will consider the app installable (a
// registered service worker with a fetch handler is one of the install criteria alongside
// manifest.json - see wwwroot/leitor-pwa.js), not to make the app work offline. An ERP showing
// stale invoices/orders/tickets because a page got served from a cache would be actively harmful,
// so only same-origin static assets (css/js/images/fonts) are ever cached - every navigation,
// every /api call, and every Razor Page GET/POST passes straight through to the network,
// uncached, every time.
const CACHE_NAME = "leitor-static-v1";
const STATIC_EXTENSIONS = [".css", ".js", ".png", ".jpg", ".jpeg", ".svg", ".woff", ".woff2", ".ico"];

function isCacheableStaticAsset(request) {
    if (request.method !== "GET") {
        return false;
    }

    var url = new URL(request.url);
    if (url.origin !== self.location.origin) {
        return false;
    }

    return STATIC_EXTENSIONS.some(function (ext) {
        return url.pathname.toLowerCase().endsWith(ext);
    });
}

self.addEventListener("install", function (event) {
    self.skipWaiting();
});

self.addEventListener("activate", function (event) {
    event.waitUntil(
        caches.keys().then(function (keys) {
            return Promise.all(
                keys
                    .filter(function (key) {
                        return key !== CACHE_NAME;
                    })
                    .map(function (key) {
                        return caches.delete(key);
                    })
            );
        }).then(function () {
            return self.clients.claim();
        })
    );
});

self.addEventListener("fetch", function (event) {
    if (!isCacheableStaticAsset(event.request)) {
        return; // let the browser handle it normally - no offline fallback for pages/API calls
    }

    event.respondWith(
        caches.open(CACHE_NAME).then(function (cache) {
            return cache.match(event.request).then(function (cached) {
                var networkFetch = fetch(event.request)
                    .then(function (response) {
                        if (response && response.ok) {
                            cache.put(event.request, response.clone());
                        }
                        return response;
                    })
                    .catch(function () {
                        return cached;
                    });

                // Cache-first for instant repeat loads, but always refresh the cache in the
                // background so a changed CSS/JS file doesn't stay stuck stale indefinitely.
                return cached || networkFetch;
            });
        })
    );
});
