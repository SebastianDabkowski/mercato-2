/**
 * Search Suggestions Module
 * Provides typeahead suggestions for the global search input with debouncing.
 */
(function () {
    'use strict';

    const DEBOUNCE_DELAY_MS = 300;
    const MIN_SEARCH_LENGTH = 2;

    let debounceTimer = null;
    let currentController = null;
    let searchInput = null;
    let suggestionsContainer = null;
    let activeIndex = -1;

    /**
     * Initializes the search suggestions functionality.
     */
    function init() {
        searchInput = document.getElementById('global-search-input');
        if (!searchInput) {
            return;
        }

        createSuggestionsContainer();
        attachEventListeners();
    }

    /**
     * Creates the suggestions dropdown container.
     */
    function createSuggestionsContainer() {
        suggestionsContainer = document.createElement('div');
        suggestionsContainer.id = 'search-suggestions';
        suggestionsContainer.className = 'search-suggestions-container';
        suggestionsContainer.setAttribute('role', 'listbox');
        suggestionsContainer.setAttribute('aria-label', 'Search suggestions');
        searchInput.parentNode.style.position = 'relative';
        searchInput.parentNode.appendChild(suggestionsContainer);
    }

    /**
     * Attaches event listeners to the search input.
     */
    function attachEventListeners() {
        searchInput.addEventListener('input', handleInput);
        searchInput.addEventListener('keydown', handleKeyDown);
        searchInput.addEventListener('focus', handleFocus);
        searchInput.addEventListener('blur', handleBlur);
        
        // Close suggestions when clicking outside
        document.addEventListener('click', function(e) {
            if (!searchInput.contains(e.target) && !suggestionsContainer.contains(e.target)) {
                hideSuggestions();
            }
        });
    }

    /**
     * Handles input events with debouncing.
     */
    function handleInput(e) {
        const query = e.target.value.trim();

        // Clear any pending request
        if (debounceTimer) {
            clearTimeout(debounceTimer);
        }

        if (currentController) {
            currentController.abort();
        }

        if (query.length < MIN_SEARCH_LENGTH) {
            hideSuggestions();
            return;
        }

        debounceTimer = setTimeout(function() {
            fetchSuggestions(query);
        }, DEBOUNCE_DELAY_MS);
    }

    /**
     * Fetches suggestions from the API.
     */
    async function fetchSuggestions(query) {
        currentController = new AbortController();
        
        try {
            const response = await fetch('/Api/SearchSuggestions?q=' + encodeURIComponent(query), {
                signal: currentController.signal
            });

            if (!response.ok) {
                throw new Error('Network response was not ok');
            }

            const suggestions = await response.json();
            renderSuggestions(suggestions);
        } catch (error) {
            if (error.name !== 'AbortError') {
                console.error('Error fetching suggestions:', error);
                hideSuggestions();
            }
        }
    }

    /**
     * Renders the suggestions in the dropdown.
     */
    function renderSuggestions(suggestions) {
        if (!suggestions || suggestions.length === 0) {
            hideSuggestions();
            return;
        }

        activeIndex = -1;
        suggestionsContainer.innerHTML = '';

        suggestions.forEach(function(suggestion, index) {
            const item = document.createElement('div');
            item.className = 'search-suggestion-item';
            item.setAttribute('role', 'option');
            item.setAttribute('data-index', index);
            item.setAttribute('data-url', suggestion.url || '');
            item.setAttribute('data-type', suggestion.type);
            item.setAttribute('data-text', suggestion.text);

            const icon = getTypeIcon(suggestion.type);
            const typeLabel = getTypeLabel(suggestion.type);

            item.innerHTML = 
                '<span class="suggestion-icon">' + icon + '</span>' +
                '<span class="suggestion-text">' + escapeHtml(suggestion.text) + '</span>' +
                '<span class="suggestion-type badge bg-secondary">' + typeLabel + '</span>';

            item.addEventListener('mousedown', function(e) {
                e.preventDefault();
                selectSuggestion(suggestion);
            });

            item.addEventListener('mouseenter', function() {
                setActiveItem(index);
            });

            suggestionsContainer.appendChild(item);
        });

        showSuggestions();
    }

    /**
     * Handles keyboard navigation.
     */
    function handleKeyDown(e) {
        const items = suggestionsContainer.querySelectorAll('.search-suggestion-item');
        
        if (!items.length || suggestionsContainer.style.display === 'none') {
            return;
        }

        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                setActiveItem(activeIndex < items.length - 1 ? activeIndex + 1 : 0);
                break;
            case 'ArrowUp':
                e.preventDefault();
                setActiveItem(activeIndex > 0 ? activeIndex - 1 : items.length - 1);
                break;
            case 'Enter':
                if (activeIndex >= 0 && items[activeIndex]) {
                    e.preventDefault();
                    const item = items[activeIndex];
                    selectSuggestion({
                        text: item.getAttribute('data-text'),
                        url: item.getAttribute('data-url'),
                        type: item.getAttribute('data-type')
                    });
                }
                break;
            case 'Escape':
                hideSuggestions();
                break;
        }
    }

    /**
     * Sets the active (highlighted) suggestion item.
     */
    function setActiveItem(index) {
        const items = suggestionsContainer.querySelectorAll('.search-suggestion-item');
        
        items.forEach(function(item, i) {
            if (i === index) {
                item.classList.add('active');
                item.setAttribute('aria-selected', 'true');
            } else {
                item.classList.remove('active');
                item.setAttribute('aria-selected', 'false');
            }
        });

        activeIndex = index;
    }

    /**
     * Selects a suggestion and navigates or searches.
     */
    function selectSuggestion(suggestion) {
        // Handle both string and numeric type values
        var isCategory = suggestion.type === 'Category' || suggestion.type === 1;
        if (isCategory && suggestion.url) {
            // Navigate to category page
            window.location.href = suggestion.url;
        } else {
            // Populate the search input and submit the form
            searchInput.value = suggestion.text;
            hideSuggestions();
            searchInput.closest('form').submit();
        }
    }

    /**
     * Shows the suggestions dropdown.
     */
    function showSuggestions() {
        suggestionsContainer.style.display = 'block';
        searchInput.setAttribute('aria-expanded', 'true');
    }

    /**
     * Hides the suggestions dropdown.
     */
    function hideSuggestions() {
        suggestionsContainer.style.display = 'none';
        suggestionsContainer.innerHTML = '';
        searchInput.setAttribute('aria-expanded', 'false');
        activeIndex = -1;
    }

    /**
     * Handles focus on the search input.
     */
    function handleFocus() {
        const query = searchInput.value.trim();
        if (query.length >= MIN_SEARCH_LENGTH) {
            fetchSuggestions(query);
        }
    }

    /**
     * Handles blur on the search input.
     */
    function handleBlur() {
        // Delay hiding to allow click events on suggestions
        setTimeout(function() {
            hideSuggestions();
        }, 200);
    }

    /**
     * Gets the icon for a suggestion type.
     */
    function getTypeIcon(type) {
        // Handle both string and numeric enum values
        switch (type) {
            case 'Category':
            case 1:
                return '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-folder" viewBox="0 0 16 16"><path d="M.54 3.87.5 3a2 2 0 0 1 2-2h3.672a2 2 0 0 1 1.414.586l.828.828A2 2 0 0 0 9.828 3H14.5a2 2 0 0 1 2 2v1.5H.54Z"/><path d="M.5 5.5v7a2 2 0 0 0 2 2h11a2 2 0 0 0 2-2v-7H.5Z"/></svg>';
            case 'Product':
            case 0:
                return '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-box" viewBox="0 0 16 16"><path d="M8.186 1.113a.5.5 0 0 0-.372 0L1.846 3.5 8 5.961 14.154 3.5 8.186 1.113zM15 4.239l-6.5 2.6v7.922l6.5-2.6V4.24zM7.5 14.762V6.838L1 4.239v7.923l6.5 2.6zM7.443.184a1.5 1.5 0 0 1 1.114 0l7.129 2.852A.5.5 0 0 1 16 3.5v8.662a1 1 0 0 1-.629.928l-7.185 2.874a.5.5 0 0 1-.372 0L.63 13.09a1 1 0 0 1-.63-.928V3.5a.5.5 0 0 1 .314-.464L7.443.184z"/></svg>';
            default:
                return '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-search" viewBox="0 0 16 16"><path d="M11.742 10.344a6.5 6.5 0 1 0-1.397 1.398h-.001c.03.04.062.078.098.115l3.85 3.85a1 1 0 0 0 1.415-1.414l-3.85-3.85a1.007 1.007 0 0 0-.115-.1zM12 6.5a5.5 5.5 0 1 1-11 0 5.5 5.5 0 0 1 11 0z"/></svg>';
        }
    }

    /**
     * Gets the label for a suggestion type.
     */
    function getTypeLabel(type) {
        // Handle both string and numeric enum values
        switch (type) {
            case 'Category':
            case 1:
                return 'Category';
            case 'Product':
            case 0:
                return 'Product';
            default:
                return 'Search';
        }
    }

    /**
     * Escapes HTML to prevent XSS.
     */
    function escapeHtml(text) {
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
