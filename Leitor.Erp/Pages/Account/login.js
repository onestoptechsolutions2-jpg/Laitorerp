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
        const form = document.querySelector('.leitor-auth-wrapper form');
        if (!form) return;

        form.addEventListener('submit', function (e) {
            // Clear previous error states
            document.querySelectorAll('.leitor-auth-wrapper .mb-3').forEach(group => {
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
                    errorField.closest('.mb-3')?.classList.add('has-error');
                }
                return;
            }

            // Disable the submit button once the form is actually going to POST, so a slow
            // connection or double-click can't fire a second login attempt. No need to
            // re-enable it - the page either navigates away or reloads fresh with an error.
            const submitButton = form.querySelector('button[type="submit"].btn-primary');
            if (submitButton) {
                submitButton.disabled = true;
            }
        });
    };

    // Handle Enter key on username field to move to password. Note: asp-for is a Razor tag
    // helper attribute stripped from the rendered HTML - the real selector is the "name"
    // attribute ASP.NET generates from it.
    const setupEnterToAdvance = function () {
        const userField = document.querySelector('[name="LoginInput.UserNameOrEmailAddress"]');
        const passwordField = document.querySelector('[name="LoginInput.Password"]');

        if (userField && passwordField) {
            userField.addEventListener('keypress', function (e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    passwordField.focus();
                }
            });
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
        setupEnterToAdvance();
    };

    // Start initialization
    initialize();

    // Re-initialize if Abp reloads the page
    if (window.abp) {
        abp.event.on('abp.pageLoaded', initialize);
    }
})();
