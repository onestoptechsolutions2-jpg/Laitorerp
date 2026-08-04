/**
 * Leitor ERP - Login Page Interactivity
 * Handles password visibility toggle and form validation
 */

(function () {
    'use strict';

    // Password visibility toggle
    const setupPasswordToggle = function () {
        const toggleButton = document.getElementById('PasswordVisibilityButton');
        if (!toggleButton) return;

        const passwordInput = toggleButton.closest('.leitor-password-wrapper')?.querySelector('input[type="password"]');
        if (!passwordInput) return;

        toggleButton.addEventListener('click', function (e) {
            e.preventDefault();

            const isPassword = passwordInput.type === 'password';
            passwordInput.type = isPassword ? 'text' : 'password';

            // Update icon
            const icon = toggleButton.querySelector('i');
            if (isPassword) {
                icon.classList.remove('fa-eye-slash');
                icon.classList.add('fa-eye');
                toggleButton.setAttribute('aria-label', 'Hide password');
            } else {
                icon.classList.remove('fa-eye');
                icon.classList.add('fa-eye-slash');
                toggleButton.setAttribute('aria-label', 'Show password');
            }
        });
    };

    // Form submission handler with validation
    const setupFormValidation = function () {
        const form = document.querySelector('.leitor-login-form');
        if (!form) return;

        form.addEventListener('submit', function (e) {
            // Clear previous error states
            document.querySelectorAll('.leitor-form-group').forEach(group => {
                group.classList.remove('has-error');
            });

            // Let ASP.NET validation handle it - just provide visual feedback
            const isValid = form.checkValidity();

            if (!isValid) {
                e.preventDefault();
                // Add focus to first error field
                const errorField = form.querySelector('input:invalid');
                if (errorField) {
                    errorField.focus();
                    errorField.closest('.leitor-form-group')?.classList.add('has-error');
                }
            }
        });
    };

    // Enhanced accessibility
    const setupAccessibility = function () {
        // Ensure all interactive elements are focusable
        const interactiveElements = document.querySelectorAll('button, a, input');
        interactiveElements.forEach(el => {
            if (!el.hasAttribute('tabindex') && el.tagName !== 'BUTTON' && el.tagName !== 'A') {
                el.setAttribute('tabindex', '0');
            }
        });

        // Handle Enter key on username field to move to password
        const userField = document.querySelector('[asp-for="LoginInput.UserNameOrEmailAddress"]');
        const passwordField = document.querySelector('[asp-for="LoginInput.Password"]');

        if (userField && passwordField) {
            userField.addEventListener('keypress', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    passwordField.focus();
                }
            });
        }
    };

    // Animate form on load
    const setupFormAnimation = function () {
        const card = document.querySelector('.leitor-login-card');
        if (card) {
            card.style.opacity = '0';
            card.style.transform = 'translateY(20px)';

            // Trigger animation after a brief delay to ensure rendering
            setTimeout(function () {
                card.style.transition = 'all 0.4s ease-out';
                card.style.opacity = '1';
                card.style.transform = 'translateY(0)';
            }, 50);
        }
    };

    // Handle social login button styling
    const setupSocialLogins = function () {
        const socialButtons = document.querySelectorAll('.leitor-btn-social');
        socialButtons.forEach(btn => {
            btn.addEventListener('click', function () {
                this.disabled = true;
                this.style.opacity = '0.6';
            });
        });
    };

    // Auto-focus email field on load
    const autoFocusEmail = function () {
        const emailField = document.querySelector('[asp-for="LoginInput.UserNameOrEmailAddress"]');
        if (emailField && !emailField.value) {
            emailField.focus();
        }
    };

    // Initialize all functionality when DOM is ready
    const initialize = function () {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', initialize);
            return;
        }

        setupPasswordToggle();
        setupFormValidation();
        setupAccessibility();
        setupFormAnimation();
        setupSocialLogins();
        autoFocusEmail();
    };

    // Start initialization
    initialize();

    // Re-initialize if Abp reloads the page
    if (window.abp) {
        abp.event.on('abp.pageLoaded', initialize);
    }
})();
