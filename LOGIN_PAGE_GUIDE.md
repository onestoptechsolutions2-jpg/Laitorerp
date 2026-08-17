# Leitor ERP — Login Page Guide

## Overview

The login page uses the same "Warm Sunrise" design system as the rest of the app — the same
`.card`/`.btn`/`.form-control`/`.alert` component styles, not a bespoke look. What makes this
page different from every other page in the app is purely structural: it renders under ABP's
**Account layout** (`ThemeManager.CurrentTheme.GetAccountLayout()`), not the standard
Application layout every other page uses, so it doesn't get the app's Global style bundle
automatically and links its own stylesheets directly.

There is no logo/title markup in `Login.cshtml` itself — that chrome comes from the theme's own
Account layout wrapper.

## File structure

```
Leitor.Erp/Pages/Account/
├── Login.cshtml       # Razor page - standard Bootstrap/app classes (.card, .btn, .form-control),
│                       # plus the direct <link> tags this page needs (see below)
├── login.css           # ONLY the page-specific chrome with no main-app equivalent: the auth
│                        # container/hero decoration, password-visibility toggle positioning,
│                        # the inline "Forgot password?" layout, validation-summary list reset
├── login.js             # Password visibility toggle, Enter-to-advance, double-submit guard
└── Login.cshtml.cs      # Code-behind (ABP's own LoginModel — not overridden)
```

## Why this page links its own stylesheets

`ErpModule.ConfigureLayoutHooks()`/`ConfigureBundles()` register the app's design system
(`leitor-tokens.css`, `leitor-theme.css`, and the `ThemeFontsViewComponent` that loads Inter)
against `StandardLayouts.Application` only — the Account layout is deliberately excluded from
that wiring, since ABP's own Account module owns that layout. So `Login.cshtml`'s own
`@section styles` block links the same three things directly:

```cshtml
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800;900&display=swap">
<link rel="stylesheet" href="/leitor-tokens.css" />
<link rel="stylesheet" href="/leitor-theme.css" />
<link rel="stylesheet" href="/Pages/Account/login.css" />
```

`leitor-tokens.css` is the single source of truth for every color/shape/shadow value in the app
(see its own header comment) — both `leitor-theme.css` and `login.css` reference its
`var(--leitor-*)` custom properties rather than each hardcoding their own copy of the palette.
If you're changing a color, shape, or shadow, **edit `leitor-tokens.css` — never redeclare a
value locally in `login.css`.**

## What's standardized vs. what's page-specific

Standardized (comes from `leitor-theme.css`, identical to every other page in the app):
- The card (`.card`/`.card-body`)
- The primary/secondary buttons (`.btn.btn-primary`, `.btn.btn-secondary`)
- Form controls and labels (`.form-control`, `.form-label`, `.form-check`)
- Alerts (`.alert.alert-danger`, `.alert.alert-warning`)

Page-specific (lives in `login.css`, no equivalent elsewhere in the app):
- `.leitor-auth-container` / `.leitor-auth-wrapper` — centers the card in whatever space the
  Account layout leaves. Deliberately no `min-height: 100vh` — the theme's own layout already
  puts logo/title chrome above this content, so claiming a full viewport height on top of that
  reintroduces a scrollbar (this was a real, previously-fixed bug — don't reintroduce it).
- The hero decoration — blurred amber/purple/pink gradient blobs, a faint SVG-noise grain
  texture, and two dashed rings that slow-rotate (`prefers-reduced-motion`-aware). All
  `position: fixed`, specifically so they can't affect the container's box size/scrolling.
- `.leitor-password-wrapper`/`.leitor-password-toggle` — the show/hide password button.
- `.leitor-form-label-wrapper` — puts "Forgot password?" inline with the Password label.
- `.leitor-validation-summary` — strips the bullet/margin off `asp-validation-summary`'s raw
  `<ul><li>` output so it reads as plain stacked lines inside the shared alert box.

## Interactivity (`login.js`)

- **Password visibility toggle** — click the eye icon to show/hide the password; swaps the
  Font Awesome icon and `aria-label`.
- **Enter-to-advance** — pressing Enter in the username field focuses the password field instead
  of submitting.
- **Double-submit guard** — disables the submit button once the form actually POSTs (valid
  client-side state), so a slow connection or double-click can't fire a second login attempt.
  No re-enable needed — the page either navigates away or reloads fresh with a server error.

If you rename a class in `Login.cshtml` that `login.js` selects by (currently:
`.leitor-auth-wrapper form`, `.mb-3`, `button[type="submit"].btn-primary`,
`.leitor-password-wrapper`), update the selector in `login.js` too — nothing enforces this at
compile time since it's plain DOM querying.

## Accessibility

- Semantic `<label>`/`<input>` pairing via `asp-for`.
- `aria-label` on the password-toggle button, updated on each click.
- Visible focus rings on every interactive element (`:focus-visible`), including the toggle.
- Dashed decorative rings and blurred blobs are `pointer-events: none` and purely decorative —
  never load-bearing for information, and their rotation respects
  `prefers-reduced-motion: reduce`.

## Troubleshooting

**Password toggle not working** — check that `login.js` loaded (Network tab) and that
`#PasswordVisibilityButton` still exists with that exact id; the script queries it directly.

**Styling looks wrong / colors don't match the rest of the app** — confirm all three stylesheet
links are present in `Login.cshtml`'s `@section styles` and load in this order:
`leitor-tokens.css` → `leitor-theme.css` → `login.css` (order matters — `login.css`'s rules
should win for anything it overrides, and everything depends on the custom properties
`leitor-tokens.css` defines).

**A new color/radius/shadow is needed** — add it to `wwwroot/leitor-tokens.css` as a
`--leitor-*` custom property, not as a literal value in `login.css`. That keeps this page and
the rest of the app on one shared palette instead of two independent copies.

**Live-verifying this page in a browser** — as of this writing, blocked in the local dev
environment by an unresolved native Postgres `leitor_erp` credential gap (`dotnet run` starts
fine but every page 500s with `28P01: password authentication failed`). The `docker-compose.yml`
path (its own Postgres container, default password matching `appsettings.json`) is the likely
working alternative if this needs live-testing — not yet attempted end-to-end.

---
**Last updated:** 2026-08-17 · matches the "Warm Sunrise" design system.
