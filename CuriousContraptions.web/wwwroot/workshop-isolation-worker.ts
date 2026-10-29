(() => {
    interface PolicyMessage { data: unknown; source: { url: string } | null; ports: MessagePort[]; }
    interface PolicyFetch { request: Request; respondWith(response: Promise<Response>): void; }
    const workerScope = self as unknown as {
        registration: { scope: string }; clients: { claim(): Promise<void> };
        addEventListener(type: 'activate', callback: (event: { waitUntil(task: Promise<void>): void }) => void): void;
        addEventListener(type: 'message', callback: (event: PolicyMessage) => void): void;
        addEventListener(type: 'fetch', callback: (event: PolicyFetch) => void): void;
    };
    // One scoped streaming header policy. No cache, offline path, skipWaiting or live-game reload.
    const scope = new URL(workerScope.registration.scope);
    function eligible(url: URL) {
        return url.origin === scope.origin && url.pathname.startsWith(scope.pathname);
    }
    workerScope.addEventListener('activate', event => event.waitUntil(workerScope.clients.claim()));
    workerScope.addEventListener('message', event => {
        const port = event.ports[0];
        try {
            if (event.ports.length !== 1 || (event.data === null || typeof event.data !== 'object' || !('policyRequest' in event.data) || event.data.policyRequest !== true) ||
                !event.source || !eligible(new URL(event.source.url))) return;
            port.postMessage({ policyReady: true });
        } finally { for (const owned of event.ports) owned.close(); }
    });
    workerScope.addEventListener('fetch', event => {
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
})();
