/**
 * Recently Viewed Products - Client-side storage utility.
 * Stores product IDs in localStorage for the current device/session.
 * Maximum items configurable via data attribute on container element.
 */
(function () {
    'use strict';

    const STORAGE_KEY = 'recentlyViewedProducts';
    const DEFAULT_MAX_ITEMS = 10;

    /**
     * Gets the list of recently viewed product IDs from storage.
     * @returns {string[]} Array of product IDs, most recent first.
     */
    function getRecentlyViewed() {
        try {
            const data = localStorage.getItem(STORAGE_KEY);
            if (!data) return [];
            const items = JSON.parse(data);
            return Array.isArray(items) ? items : [];
        } catch (e) {
            console.warn('Error reading recently viewed products:', e);
            return [];
        }
    }

    /**
     * Saves the list of recently viewed product IDs to storage.
     * @param {string[]} items Array of product IDs.
     */
    function saveRecentlyViewed(items) {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
        } catch (e) {
            console.warn('Error saving recently viewed products:', e);
        }
    }

    /**
     * Adds a product ID to the recently viewed list.
     * If the product already exists, it's moved to the front.
     * @param {string} productId The product ID to add.
     * @param {number} maxItems Maximum number of items to keep.
     */
    function addRecentlyViewed(productId, maxItems) {
        if (!productId) return;
        
        maxItems = maxItems || DEFAULT_MAX_ITEMS;
        let items = getRecentlyViewed();
        
        // Remove existing instance if present
        items = items.filter(id => id !== productId);
        
        // Add to front of list
        items.unshift(productId);
        
        // Trim to max items
        if (items.length > maxItems) {
            items = items.slice(0, maxItems);
        }
        
        saveRecentlyViewed(items);
    }

    /**
     * Clears all recently viewed products.
     */
    function clearRecentlyViewed() {
        try {
            localStorage.removeItem(STORAGE_KEY);
        } catch (e) {
            console.warn('Error clearing recently viewed products:', e);
        }
    }

    /**
     * Gets the comma-separated list of recently viewed product IDs.
     * @returns {string} Comma-separated product IDs.
     */
    function getRecentlyViewedString() {
        return getRecentlyViewed().join(',');
    }

    // Initialize: Track product view on product detail pages
    document.addEventListener('DOMContentLoaded', function () {
        // Check if we're on a product detail page
        const productDetailContainer = document.querySelector('[data-product-id]');
        if (productDetailContainer) {
            const productId = productDetailContainer.dataset.productId;
            const maxItems = parseInt(productDetailContainer.dataset.maxRecentItems, 10) || DEFAULT_MAX_ITEMS;
            addRecentlyViewed(productId, maxItems);
        }

        // Load recently viewed products section if present
        const recentlyViewedContainer = document.querySelector('[data-recently-viewed]');
        if (recentlyViewedContainer) {
            const currentProductId = recentlyViewedContainer.dataset.currentProductId;
            let items = getRecentlyViewed();
            
            // Exclude current product if viewing a product page
            if (currentProductId) {
                items = items.filter(id => id !== currentProductId);
            }
            
            if (items.length > 0) {
                // Get the max items to display from the container
                const maxDisplay = parseInt(recentlyViewedContainer.dataset.maxDisplay, 10) || 5;
                const displayItems = items.slice(0, maxDisplay);
                
                // Make AJAX request to get product data
                fetch('/Api/RecentlyViewed?ids=' + encodeURIComponent(displayItems.join(',')))
                    .then(response => {
                        if (!response.ok) {
                            throw new Error('Failed to fetch recently viewed products');
                        }
                        return response.json();
                    })
                    .then(products => {
                        if (products && products.length > 0) {
                            renderRecentlyViewed(recentlyViewedContainer, products);
                        }
                    })
                    .catch(error => {
                        console.warn('Error loading recently viewed products:', error);
                    });
            }
        }
    });

    /**
     * Renders the recently viewed products into the container.
     * @param {HTMLElement} container The container element.
     * @param {Array} products Array of product objects.
     */
    function renderRecentlyViewed(container, products) {
        if (!products || products.length === 0) {
            container.style.display = 'none';
            return;
        }

        // Show the container
        container.style.display = 'block';

        // Find the products list container
        const listContainer = container.querySelector('[data-recently-viewed-list]');
        if (!listContainer) return;

        // Clear existing content
        listContainer.innerHTML = '';

        // Render each product
        products.forEach(product => {
            const productCard = document.createElement('div');
            productCard.className = 'col';
            
            const hasImage = product.mainImageThumbnailUrl;
            const imageHtml = hasImage
                ? `<img src="${escapeHtml(product.mainImageThumbnailUrl)}" class="card-img-top" alt="${escapeHtml(product.name)}" style="height: 120px; object-fit: cover;">`
                : `<div class="card-img-top bg-light d-flex align-items-center justify-content-center" style="height: 120px;">
                       <span class="text-muted small">No Image</span>
                   </div>`;

            productCard.innerHTML = `
                <a href="/Buyer/Product?id=${escapeHtml(product.id)}" class="text-decoration-none">
                    <div class="card h-100 hover-shadow">
                        ${imageHtml}
                        <div class="card-body p-2">
                            <h6 class="card-title text-truncate mb-1" title="${escapeHtml(product.name)}">${escapeHtml(product.name)}</h6>
                            <p class="card-text mb-0">
                                <strong class="text-primary">${escapeHtml(product.currency)} ${formatPrice(product.amount)}</strong>
                            </p>
                        </div>
                    </div>
                </a>
            `;
            
            listContainer.appendChild(productCard);
        });
    }

    /**
     * Escapes HTML special characters.
     * @param {string} str The string to escape.
     * @returns {string} The escaped string.
     */
    function escapeHtml(str) {
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    /**
     * Formats a price number to 2 decimal places.
     * @param {number} price The price to format.
     * @returns {string} The formatted price.
     */
    function formatPrice(price) {
        return Number(price).toFixed(2);
    }

    // Expose functions globally for external use if needed
    window.RecentlyViewed = {
        get: getRecentlyViewed,
        add: addRecentlyViewed,
        clear: clearRecentlyViewed,
        getString: getRecentlyViewedString
    };
})();
