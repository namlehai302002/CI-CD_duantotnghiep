const WMS_CACHE_NAME = 'wms-pro-pwa-shell-v20260521-p406';
const WMS_SHELL_ASSETS = [
    '/offline.html',
    '/manifest.webmanifest',
    '/css/site.css',
    '/css/wms-offline.css',
    '/js/site.js',
    '/js/offline-page.js',
    '/js/mobile-scanner.js',
    '/js/offline-scan-queue.js',
    '/js/pwa.js',
    '/lib/jquery/dist/jquery.min.js',
    '/lib/html5-qrcode/html5-qrcode.min.js',
    '/images/logo.svg',
    '/images/pwa-icon-192.png',
    '/images/pwa-icon-512.png',
    '/favicon.ico'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(WMS_CACHE_NAME)
            .then(cache => cache.addAll(WMS_SHELL_ASSETS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys()
            .then(keys => Promise.all(keys
                .filter(key => key !== WMS_CACHE_NAME)
                .map(key => caches.delete(key))))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', event => {
    const request = event.request;
    if (request.method !== 'GET') return;

    const url = new URL(request.url);
    if (url.origin !== self.location.origin) return;

    if (request.mode === 'navigate') {
        event.respondWith(
            fetch(request).catch(() => caches.match('/offline.html'))
        );
        return;
    }

    if (!WMS_SHELL_ASSETS.includes(url.pathname)) return;

    event.respondWith(
        caches.match(request).then(cached => cached || fetch(request))
    );
});
