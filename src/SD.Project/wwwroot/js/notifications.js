/**
 * Notification Badge Module
 * Fetches unread notification count and updates the badge in the header.
 */
(function () {
    'use strict';

    // Configuration
    var POLL_INTERVAL_MS = 60000; // Poll every 60 seconds

    var badge = null;

    /**
     * Initializes the notification badge functionality.
     */
    function init() {
        badge = document.getElementById('notification-badge');
        if (!badge) {
            return; // Badge not present (user not authenticated)
        }

        // Initial fetch
        fetchUnreadCount();

        // Poll for updates
        setInterval(fetchUnreadCount, POLL_INTERVAL_MS);
    }

    /**
     * Fetches the unread notification count from the API.
     */
    async function fetchUnreadCount() {
        try {
            var response = await fetch('/Api/Notifications');
            
            if (!response.ok) {
                console.error('Failed to fetch notification count');
                return;
            }

            var data = await response.json();
            updateBadge(data.count);
        } catch (error) {
            console.error('Error fetching notification count:', error);
        }
    }

    /**
     * Updates the badge display with the count.
     */
    function updateBadge(count) {
        if (!badge) {
            return;
        }

        if (count > 0) {
            badge.textContent = count > 99 ? '99+' : count.toString();
            badge.style.display = 'inline-block';
        } else {
            badge.style.display = 'none';
        }
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
