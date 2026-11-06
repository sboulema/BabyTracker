const CACHE = 'babytracker-v1';
const OFFLINE_URL = '/offline.html';
const STATIC_PREFIXES = ['/css/', '/js/', '/icons/'];

self.addEventListener('install', (event) => {
  event.waitUntil(caches.open(CACHE).then((cache) => cache.add(OFFLINE_URL)));
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) =>
      Promise.all(keys.filter((key) => key !== CACHE).map((key) => caches.delete(key)))
    )
  );
  self.clients.claim();
});

self.addEventListener('fetch', (event) => {
  const { request } = event;
  const url = new URL(request.url);

  // Leave uploads (tus at /import), non-GET and cross-origin requests untouched
  if (request.method !== 'GET' || url.origin !== location.origin || url.pathname.startsWith('/import')) {
    return;
  }

  // Pages: always network (private, per-user data), offline page as fallback
  if (request.mode === 'navigate') {
    event.respondWith(fetch(request).catch(() => caches.match(OFFLINE_URL)));
    return;
  }

  // Static assets: stale-while-revalidate
  if (STATIC_PREFIXES.some((prefix) => url.pathname.startsWith(prefix))) {
    event.respondWith(
      caches.open(CACHE).then(async (cache) => {
        const cached = await cache.match(request);
        const network = fetch(request).then((response) => {
          if (response.ok) {
            cache.put(request, response.clone());
          }
          return response;
        });
        return cached ?? network;
      })
    );
  }
});
