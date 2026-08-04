/**
 * Leitor ERP - Minimalist Login Page
 * Password visibility toggle only
 */

(function () {
    'use strict';

    const setupPasswordToggle = function () {
        const toggleButton = document.getElementById('PasswordVisibilityButton');
        if (!toggleButton) return;

        const passwordInput = toggleButton.closest('.leitor-password-wrapper')?.querySelector('input[type="password"]');
        if (!passwordInput) return;

        toggleButton.addEventListener('click', function (e) {
            e.preventDefault();

            const isPassword = passwordInput.type === 'password';
            passwordInput.type = isPassword ? 'text' : 'password';

            const icon = toggleButton.querySelector('i');
            if (isPassword) {
                icon.classList.remove('fa-eye-slash');
                icon.classList.add('fa-eye');
            } else {
                icon.classList.remove('fa-eye');
                icon.classList.add('fa-eye-slash');
            }
        });
    };

    const initialize = function () {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', initialize);
            return;
        }
        setupPasswordToggle();
    };

    initialize();
})();
