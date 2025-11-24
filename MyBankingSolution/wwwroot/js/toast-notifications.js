/**
 * Toast Notification System for Banking Application
 * Provides success, error, warning, and info notifications
 */

const ToastNotification = {
    /**
     * Show a success notification
     * @param {string} message - The message to display
     * @param {number} duration - Duration in milliseconds (default: 5000)
     */
    success: function (message, duration = 5000) {
        this.show(message, 'success', duration);
    },

    /**
     * Show an error notification
     * @param {string} message - The message to display
     * @param {number} duration - Duration in milliseconds (default: 7000)
     */
    error: function (message, duration = 7000) {
        this.show(message, 'error', duration);
    },

    /**
     * Show a warning notification
     * @param {string} message - The message to display
     * @param {number} duration - Duration in milliseconds (default: 6000)
     */
    warning: function (message, duration = 6000) {
        this.show(message, 'warning', duration);
    },

    /**
     * Show an info notification
     * @param {string} message - The message to display
     * @param {number} duration - Duration in milliseconds (default: 5000)
     */
    info: function (message, duration = 5000) {
        this.show(message, 'info', duration);
    },

    /**
     * Core function to display a toast notification
     * @param {string} message - The message to display
     * @param {string} type - Type of notification (success, error, warning, info)
     * @param {number} duration - Duration in milliseconds
     */
    show: function (message, type = 'info', duration = 5000) {
        // Create toast container if it doesn't exist
        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            container.className = 'toast-container position-fixed top-0 end-0 p-3';
            container.style.zIndex = '9999';
            document.body.appendChild(container);
        }

        // Create unique ID for this toast
        const toastId = 'toast-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);

        // Determine icon and colors based on type
        const config = this.getConfig(type);

        // Create toast element
        const toastElement = document.createElement('div');
        toastElement.id = toastId;
        toastElement.className = 'toast align-items-center border-0';
        toastElement.setAttribute('role', 'alert');
        toastElement.setAttribute('aria-live', 'assertive');
        toastElement.setAttribute('aria-atomic', 'true');
        toastElement.style.minWidth = '300px';

        toastElement.innerHTML = `
            <div class="d-flex ${config.bgClass} text-white">
                <div class="toast-body d-flex align-items-center">
                    <i class="bi ${config.icon} fs-5 me-2"></i>
                    <span>${this.escapeHtml(message)}</span>
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        `;

        // Add to container
        container.appendChild(toastElement);

        // Initialize Bootstrap toast
        const bsToast = new bootstrap.Toast(toastElement, {
            autohide: true,
            delay: duration
        });

        // Show the toast
        bsToast.show();

        // Remove from DOM after hidden
        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });

        // Play sound (optional)
        this.playSound(type);
    },

    /**
     * Get configuration for toast type
     * @param {string} type - Type of notification
     * @returns {object} Configuration object
     */
    getConfig: function (type) {
        const configs = {
            success: {
                icon: 'bi-check-circle-fill',
                bgClass: 'bg-success'
            },
            error: {
                icon: 'bi-exclamation-circle-fill',
                bgClass: 'bg-danger'
            },
            warning: {
                icon: 'bi-exclamation-triangle-fill',
                bgClass: 'bg-warning'
            },
            info: {
                icon: 'bi-info-circle-fill',
                bgClass: 'bg-primary'
            }
        };
        return configs[type] || configs.info;
    },

    /**
     * Escape HTML to prevent XSS
     * @param {string} text - Text to escape
     * @returns {string} Escaped text
     */
    escapeHtml: function (text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    },

    /**
     * Play notification sound (optional)
     * @param {string} type - Type of notification
     */
    playSound: function (type) {
        // Optional: Implement sound notifications
        // You can add audio files and play them here
    }
};

// Make it globally available
window.ToastNotification = ToastNotification;

// Auto-show toasts from TempData
document.addEventListener('DOMContentLoaded', function () {
    // Check for success message
    const successMessage = document.getElementById('toast-success-message');
    if (successMessage) {
        ToastNotification.success(successMessage.value);
    }

    // Check for error message
    const errorMessage = document.getElementById('toast-error-message');
    if (errorMessage) {
        ToastNotification.error(errorMessage.value);
    }

    // Check for warning message
    const warningMessage = document.getElementById('toast-warning-message');
    if (warningMessage) {
        ToastNotification.warning(warningMessage.value);
    }

    // Check for info message
    const infoMessage = document.getElementById('toast-info-message');
    if (infoMessage) {
        ToastNotification.info(infoMessage.value);
    }
});
