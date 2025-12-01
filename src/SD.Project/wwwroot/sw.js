/**
 * Service Worker for Web Push Notifications
 * Handles push events and notification clicks.
 */

'use strict';

// Cache version for offline support
var CACHE_NAME = 'mercato-v1';

// Install event - cache essential assets
self.addEventListener('install', function (event) {
    console.log('[Service Worker] Installing...');
    self.skipWaiting();
});

// Activate event - clean up old caches
self.addEventListener('activate', function (event) {
    console.log('[Service Worker] Activating...');
    event.waitUntil(self.clients.claim());
});

// Push event - handle incoming push notifications
self.addEventListener('push', function (event) {
    console.log('[Service Worker] Push received:', event);

    var data = {
        title: 'Mercato Marketplace',
        body: 'You have a new notification',
        icon: '/favicon.ico',
        badge: '/favicon.ico',
        data: { url: '/' }
    };

    // Parse push data if available
    if (event.data) {
        try {
            var payload = event.data.json();
            data.title = payload.title || data.title;
            data.body = payload.body || data.body;
            data.icon = payload.icon || data.icon;
            data.badge = payload.badge || data.badge;
            
            if (payload.data) {
                data.data = payload.data;
                if (payload.data.url) {
                    data.data.url = payload.data.url;
                }
            }
        } catch (e) {
            console.error('[Service Worker] Error parsing push data:', e);
            data.body = event.data.text();
        }
    }

    var options = {
        body: data.body,
        icon: data.icon,
        badge: data.badge,
        vibrate: [100, 50, 100],
        data: data.data,
        actions: [
            { action: 'open', title: 'Open' },
            { action: 'dismiss', title: 'Dismiss' }
        ],
        requireInteraction: true,
        tag: data.data && data.data.tag ? data.data.tag : 'mercato-notification'
    };

    event.waitUntil(
        self.registration.showNotification(data.title, options)
    );
});

// Notification click event - handle user interaction
self.addEventListener('notificationclick', function (event) {
    console.log('[Service Worker] Notification clicked:', event.action);

    event.notification.close();

    if (event.action === 'dismiss') {
        return;
    }

    // Get the URL to open
    var url = '/';
    if (event.notification.data && event.notification.data.url) {
        url = event.notification.data.url;
    }

    // Build full URL for navigation
    var fullUrl = new URL(url, self.location.origin).href;

    // Focus or open the appropriate window
    event.waitUntil(
        self.clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then(function (clientList) {
                // Check if a window is already open
                for (var i = 0; i < clientList.length; i++) {
                    var client = clientList[i];
                    if (client.url.includes(self.location.origin) && 'focus' in client) {
                        // Use postMessage to navigate the client
                        client.postMessage({ type: 'NAVIGATE', url: fullUrl });
                        return client.focus();
                    }
                }
                
                // No window open, open a new one
                if (self.clients.openWindow) {
                    return self.clients.openWindow(fullUrl);
                }
            })
    );
});

// Notification close event
self.addEventListener('notificationclose', function (event) {
    console.log('[Service Worker] Notification closed');
});

// Push subscription change event - handle subscription updates
self.addEventListener('pushsubscriptionchange', function (event) {
    console.log('[Service Worker] Push subscription changed');
    
    event.waitUntil(
        self.registration.pushManager.subscribe(event.oldSubscription.options)
            .then(function (subscription) {
                // Convert ArrayBuffer to base64 safely (avoids stack overflow with large arrays)
                function arrayBufferToBase64(buffer) {
                    var bytes = new Uint8Array(buffer);
                    var binary = '';
                    for (var i = 0; i < bytes.byteLength; i++) {
                        binary += String.fromCharCode(bytes[i]);
                    }
                    return btoa(binary);
                }

                // Re-subscribe and update the server
                return fetch('/Api/PushSubscription?handler=Subscribe', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        endpoint: subscription.endpoint,
                        p256dh: arrayBufferToBase64(subscription.getKey('p256dh')),
                        auth: arrayBufferToBase64(subscription.getKey('auth'))
                    })
                });
            })
    );
});
