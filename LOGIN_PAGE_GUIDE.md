# Leitor ERP - Modern Login Page Design Guide

## Overview

The login page has been redesigned with a modern, professional corporate aesthetic featuring:

- **Clean card-based layout** with gradient background
- **Professional branding** with logo and company name
- **Smooth animations** and transitions
- **Enhanced accessibility** for all users
- **Responsive design** for mobile and desktop
- **Dark mode support** via CSS media queries
- **Password visibility toggle** for better UX
- **Social login integration** (Google, GitHub, Microsoft)
- **Comprehensive form validation**

## File Structure

```
Leitor.Erp/Pages/Account/
├── Login.cshtml          # Razor page template (updated)
├── login.css             # Modern styling
├── login.js              # Interactivity & validation
└── Login.cshtml.cs       # Code-behind (unchanged)
```

## Key Features

### 1. Visual Design

**Color Scheme:**
- Primary: `#0ea5e9` (Sky Blue) - Gradient to `#0284c7` (Darker Blue)
- Background: `#0f172a` to `#1e293b` (Deep Navy Gradient)
- Text: `#1e293b` (Dark Slate)
- Secondary Text: `#64748b` (Slate)
- Borders: `#e2e8f0` (Light Slate)

**Typography:**
- Headings: 24px, weight 600
- Labels: 14px, weight 500
- Body: 14px, weight 400

### 2. Responsive Breakpoints

- **Desktop (> 480px)**: Full width 420px card
- **Mobile (≤ 480px)**: Adjusted padding and font sizes
- **Social buttons**: 2-column grid on desktop, 1-column on mobile

### 3. Interactive Elements

**Password Visibility Toggle:**
- Click the eye icon to show/hide password
- Icon changes from `fa-eye-slash` to `fa-eye`
- Smooth color transitions on hover

**Form Validation:**
- Real-time validation feedback
- Error messages below fields
- Focus on first invalid field on submit

**Animations:**
- Card slides in on page load (300ms)
- Buttons have hover lift effect (2px transform)
- Smooth color transitions (200ms)

## Customization Guide

### Changing Colors

Edit `login.css`:

```css
/* Primary color (blue) - change these */
background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%);
border-color: #0ea5e9;
color: #0ea5e9;

/* Background gradient - change these */
background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);

/* Text colors - change these */
color: #1e293b;        /* Primary text */
color: #64748b;        /* Secondary text */
color: #94a3b8;        /* Muted text */
```

### Changing Logo

Edit `Login.cshtml`:

```html
<div class="leitor-logo-icon">
    <span>L</span>  <!-- Change this to your logo or img tag -->
</div>
<div class="leitor-logo-text">
    <h1>@EL["App:Title"]</h1>  <!-- Title from localization -->
    <p>@EL["Auth:SignInSubtitle"]</p>  <!-- Subtitle from localization -->
</div>
```

Or use an image:

```html
<div class="leitor-logo-icon">
    <img src="/images/logo.png" alt="Leitor Logo" style="width: 100%; height: 100%;" />
</div>
```

### Adjusting Card Size

Edit `login.css`:

```css
.leitor-auth-wrapper {
    width: 100%;
    max-width: 420px;  /* Change this value */
}
```

### Changing Padding/Spacing

Edit `login.css`:

```css
.leitor-login-card {
    padding: 48px 40px;  /* Top/Bottom Left/Right */
}

.leitor-logo-section {
    margin-bottom: 32px;  /* Space below logo */
}

.leitor-form-group {
    margin-bottom: 20px;  /* Space between form fields */
}
```

### Font Family

Edit `login.css`:

```css
.leitor-form-control {
    font-family: 'Your Font Name', sans-serif;
}
```

## Localization

Strings are pulled from ABP localization resources. Key strings:

- `@L["Login"]` - Login button text
- `@L["EmailOrUsername"]` - Email field label
- `@L["Password"]` - Password field label
- `@L["RememberMe"]` - Remember me checkbox
- `@L["ForgotPassword"]` - Forgot password link
- `@L["OrLoginWith"]` - Social login divider
- `@L["AreYouANewUser"]` - Sign up prompt
- `@EL["App:Title"]` - Application title
- `@EL["Auth:SignInSubtitle"]` - Subtitle

Add these to your `Localization/Erp/en.json`:

```json
{
  "EmailOrUsername": "Email or Username",
  "EnterPassword": "Enter your password",
  "TogglePasswordVisibility": "Toggle password visibility",
  "App:Title": "Leitor ERP",
  "Auth:SignInSubtitle": "Enterprise Resource Planning"
}
```

## Browser Support

- ✅ Chrome/Edge (latest)
- ✅ Firefox (latest)
- ✅ Safari (latest)
- ✅ Mobile browsers (iOS Safari, Chrome Mobile)

## Accessibility Features

- **Semantic HTML**: Proper `<label>` and `<input>` associations
- **ARIA attributes**: `aria-label` on password toggle button
- **Keyboard navigation**: Tab through all interactive elements
- **Focus rings**: Clear visual focus indicators
- **Color contrast**: WCAG AA compliant ratios
- **Error announcements**: Screen reader friendly error messages

## Dark Mode

The page automatically adapts to system dark mode preferences via `@media (prefers-color-scheme: dark)`.

**Testing dark mode:**
1. macOS: System Preferences → General → Appearance → Dark
2. Windows: Settings → Personalization → Colors → Dark
3. Browser DevTools: Ctrl+Shift+P → "Dark mode"

## Performance Optimizations

- Minimal CSS (no external frameworks)
- Inline critical styles
- Efficient JavaScript (no dependencies)
- Optimized animations (GPU accelerated transforms)
- Proper image compression for logo

## Security Considerations

- ✅ Password field masked by default
- ✅ No hardcoded credentials
- ✅ Secure form submission (POST only)
- ✅ CSRF token support (handled by ABP)
- ✅ Input validation on both client & server
- ✅ No password logging in JavaScript

## Troubleshooting

**Password toggle not working:**
- Ensure `login.js` is loading (check Network tab)
- Check browser console for JavaScript errors
- Verify Font Awesome is installed for icons

**Styling looks broken:**
- Clear browser cache (Ctrl+Shift+Delete)
- Check that `login.css` is loading
- Verify no CSS conflicts from other stylesheets

**Form not submitting:**
- Ensure JavaScript is enabled
- Check server-side validation errors
- Verify ASP.NET form tokens are present

**Mobile layout issues:**
- Check viewport meta tag in layout
- Test responsive breakpoints at 480px
- Ensure touch targets are 44px+ minimum

## Future Enhancements

Consider adding:

1. **Two-Factor Authentication (2FA)** - TOTP/SMS support
2. **Passwordless login** - Magic links or biometric auth
3. **Multi-tenant selection** - Tenant picker before login
4. **Language selector** - For multi-language apps
5. **Session management** - "Remember device" option
6. **Login history** - Recent login activity display
7. **Custom themes** - User-selectable themes
8. **Progressive enhancement** - Work without JavaScript

## Testing Checklist

- [ ] Desktop layout (> 1024px)
- [ ] Tablet layout (768px - 1024px)
- [ ] Mobile layout (< 768px)
- [ ] Password visibility toggle
- [ ] Form validation errors
- [ ] Keyboard navigation (Tab key)
- [ ] Screen reader testing (NVDA/JAWS)
- [ ] Dark mode appearance
- [ ] Social login buttons (if configured)
- [ ] Error messages display correctly
- [ ] Forgot password link works
- [ ] Sign up link works
- [ ] Browser autofill works
- [ ] Session timeout behavior

## Support & Issues

For issues or questions:

1. Check this guide first
2. Review browser console for errors
3. Test in different browsers
4. Verify ABP modules are loaded
5. Check localization strings are defined

---

**Design Version:** 1.0  
**Last Updated:** 2026-08-04  
**Compatibility:** Leitor.Erp with ABP Framework 10.5.0+
