/**
 * Push Notifications Module
 * Handles service worker registration and push subscription management.
 */
(function () {
    'use strict';

    // Check for push notification support
    var isPushSupported = 'serviceWorker' in navigator && 
                          'PushManager' in window && 
                          'Notification' in window;

    var serviceWorkerRegistration = null;
    var pushSubscription = null;

    /**
     * Initializes push notification functionality.
     */
    function init() {
        if (!isPushSupported) {
            console.log('[Push] Push notifications not supported');
            return;
        }

        // Register service worker
        registerServiceWorker();
    }

    /**
     * Registers the service worker.
     */
    async function registerServiceWorker() {
        try {
            serviceWorkerRegistration = await navigator.serviceWorker.register('/sw.js');
            console.log('[Push] Service Worker registered');

            // Listen for navigation messages from service worker
            navigator.serviceWorker.addEventListener('message', function (event) {
                if (event.data && event.data.type === 'NAVIGATE') {
                    window.location.href = event.data.url;
                }
            });

            // Check current subscription status
            pushSubscription = await serviceWorkerRegistration.pushManager.getSubscription();
            if (pushSubscription) {
                console.log('[Push] Existing subscription found');
            }
        } catch (error) {
            console.error('[Push] Service Worker registration failed:', error);
        }
    }

    /**
     * Converts a base64 string to Uint8Array for VAPID key.
     */
    function urlBase64ToUint8Array(base64String) {
        var padding = '='.repeat((4 - base64String.length % 4) % 4);
        var base64 = (base64String + padding)
            .replace(/-/g, '+')
            .replace(/_/g, '/');
        
        var rawData = window.atob(base64);
        var outputArray = new Uint8Array(rawData.length);
        
        for (var i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        
        return outputArray;
    }

    /**
     * Converts an ArrayBuffer to a base64 string.
     */
    function arrayBufferToBase64(buffer) {
        var bytes = new Uint8Array(buffer);
        var binary = '';
        for (var i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return window.btoa(binary);
    }

    /**
     * Gets the device name for the current browser.
     */
    function getDeviceName() {
        var ua = navigator.userAgent;
        var browser = 'Unknown Browser';
        var os = 'Unknown OS';

        // Detect browser
        if (ua.includes('Chrome') && !ua.includes('Edge')) {
            browser = 'Chrome';
        } else if (ua.includes('Firefox')) {
            browser = 'Firefox';
        } else if (ua.includes('Safari') && !ua.includes('Chrome')) {
            browser = 'Safari';
        } else if (ua.includes('Edge')) {
            browser = 'Edge';
        }

        // Detect OS
        if (ua.includes('Windows')) {
            os = 'Windows';
        } else if (ua.includes('Mac OS')) {
            os = 'macOS';
        } else if (ua.includes('Linux')) {
            os = 'Linux';
        } else if (ua.includes('Android')) {
            os = 'Android';
        } else if (ua.includes('iPhone') || ua.includes('iPad')) {
            os = 'iOS';
        }

        return browser + ' on ' + os;
    }

    /**
     * Requests permission and subscribes to push notifications.
     */
    async function subscribeToPush() {
        if (!isPushSupported) {
            return { success: false, error: 'Push notifications not supported' };
        }

        if (!serviceWorkerRegistration) {
            return { success: false, error: 'Service worker not ready' };
        }

        try {
            // Request notification permission
            var permission = await Notification.requestPermission();
            
            if (permission !== 'granted') {
                console.log('[Push] Notification permission denied');
                return { success: false, error: 'Permission denied' };
            }

            // Get VAPID public key from server
            var keyResponse = await fetch('/Api/PushSubscription?handler=VapidKey');
            if (!keyResponse.ok) {
                throw new Error('Failed to get VAPID key');
            }
            
            var keyData = await keyResponse.json();
            if (!keyData.publicKey) {
                throw new Error('VAPID key not configured on server');
            }

            // Subscribe to push manager
            var applicationServerKey = urlBase64ToUint8Array(keyData.publicKey);
            pushSubscription = await serviceWorkerRegistration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: applicationServerKey
            });

            console.log('[Push] Subscribed to push manager');

            // Send subscription to server
            var p256dh = arrayBufferToBase64(pushSubscription.getKey('p256dh'));
            var auth = arrayBufferToBase64(pushSubscription.getKey('auth'));

            var response = await fetch('/Api/PushSubscription?handler=Subscribe', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    endpoint: pushSubscription.endpoint,
                    p256dh: p256dh,
                    auth: auth,
                    deviceName: getDeviceName()
                })
            });

            var result = await response.json();
            
            if (result.success) {
                console.log('[Push] Subscription saved to server');
                return { success: true };
            } else {
                throw new Error(result.error || 'Failed to save subscription');
            }
        } catch (error) {
            console.error('[Push] Subscription failed:', error);
            return { success: false, error: error.message };
        }
    }

    /**
     * Unsubscribes from push notifications.
     */
    async function unsubscribeFromPush() {
        if (!pushSubscription) {
            return { success: true };
        }

        try {
            var endpoint = pushSubscription.endpoint;
            
            // Unsubscribe from push manager
            await pushSubscription.unsubscribe();
            pushSubscription = null;

            // Remove subscription from server
            var response = await fetch('/Api/PushSubscription?handler=Unsubscribe', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ endpoint: endpoint })
            });

            var result = await response.json();
            console.log('[Push] Unsubscribed from push notifications');
            
            return { success: true };
        } catch (error) {
            console.error('[Push] Unsubscribe failed:', error);
            return { success: false, error: error.message };
        }
    }

    /**
     * Checks if push notifications are currently enabled.
     */
    async function isPushEnabled() {
        if (!isPushSupported || !serviceWorkerRegistration) {
            return false;
        }

        pushSubscription = await serviceWorkerRegistration.pushManager.getSubscription();
        return pushSubscription !== null;
    }

    /**
     * Gets the current notification permission status.
     */
    function getPermissionStatus() {
        if (!('Notification' in window)) {
            return 'unsupported';
        }
        return Notification.permission;
    }

    /**
     * Checks push status from the server.
     */
    async function checkServerStatus() {
        try {
            var response = await fetch('/Api/PushSubscription?handler=Status');
            return await response.json();
        } catch (error) {
            console.error('[Push] Failed to check server status:', error);
            return { enabled: false, authenticated: false };
        }
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Expose API for settings page
    window.MercatoPush = {
        isSupported: function () { return isPushSupported; },
        subscribe: subscribeToPush,
        unsubscribe: unsubscribeFromPush,
        isEnabled: isPushEnabled,
        getPermission: getPermissionStatus,
        checkStatus: checkServerStatus
    };
})();
