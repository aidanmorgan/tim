// One scoped streaming header policy. No cache, offline path, skipWaiting or live-game reload.
const policyVersion = 1;
const scope = new URL(self.registration.scope);
function eligible(url) {
    return url.origin === scope.origin && url.pathname.startsWith(scope.pathname);
}
self.addEventListener('activate', event => event.waitUntil(self.clients.claim()));
self.addEventListener('message', event => {
    const port = event.ports[0];
    try {
        if (event.ports.length !== 1 || event.data?.policyRequest !== policyVersion ||
            !event.source || !eligible(new URL(event.source.url))) return;
        port.postMessage({ policyVersion });
    } finally { for (const owned of event.ports) owned.close(); }
});
self.addEventListener('fetch', event => {
    const request = event.request;
    if (request.method !== 'GET' || !eligible(new URL(request.url))) return;
    event.respondWith((async () => {
        const response = await fetch(request, { redirect: 'error' });
        if (response.redirected || !response.url || !eligible(new URL(response.url)) ||
            response.type === 'opaque' || response.type === 'opaqueredirect' || response.type === 'error')
            throw new Error('Unsupported Workshop isolation response.');
        if (!response.ok) return response;
        const headers = new Headers(response.headers);
        headers.set('Cross-Origin-Opener-Policy', 'same-origin');
        headers.set('Cross-Origin-Embedder-Policy', 'require-corp');
        headers.set('Cross-Origin-Resource-Policy', 'same-origin');
        return new Response(response.body, { status: response.status, statusText: response.statusText, headers });
    })());
});
