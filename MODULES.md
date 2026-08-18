# Leitor ERP — Modules & Features Overview

Leitor Investment Company Ltd. is a managed IT & cybersecurity services provider. This ERP
is built to run that business end-to-end: sell the work, deliver it, support it, bill for it,
and account for it — in one system instead of a patchwork of disconnected tools.

## The business model this system is built around

Three revenue streams, in order of relationship:

1. **Recurring retainer contracts** (KES 15k–30k/month per client) — network infrastructure,
   WiFi, firewalls, user/device support, backups, Microsoft 365/Google Workspace administration,
   endpoint security, patching, CCTV oversight, basic security monitoring, incident response,
   vendor coordination.
2. **Projects** (one-off, higher-margin) — network deployments, CCTV installs, server
   deployments, office moves, security upgrades, cloud migrations, hardware/software resale.
3. **Cybersecurity upsell tier** — security assessments, vulnerability/cyber-risk assessments,
   security policy work, awareness training, backup/DR reviews.

The flywheel: **projects feed the recurring contract, and the recurring contract feeds the
business.** A one-off project is meant to convert into an ongoing retainer, not stay a one-off
sale — the ERP models this directly (see Projects → Contract conversion, below), not just as two
unrelated modules.

## How modules are organized

Every module falls into one of two categories:

- **Core (always on)** — gated by permissions only. This is the operational backbone every
  client relationship runs through, regardless of which upsell tiers a given deployment uses.
- **Optional (toggleable)** — off by default, turned on per-deployment from
  **Administration → Module Toggles** (`Erp.ModuleToggles.Manage` permission). These are
  genuinely new capabilities layered on top of the core, not required for the ERP to function.
  Each one is gated three ways: on its AppServices (`[RequiresFeature]`), on its pages
  (`OnGetAsync` checks and 404s if disabled), and on its menu entries (hidden unless both the
  permission *and* the feature are on).

---

## Core modules (always on)

### CRM — Leads → Opportunities → Customers
One pipeline, not three disconnected lists: a Lead (with outreach/dedup/do-not-contact
tracking) becomes an Opportunity (with needs assessments and proposals), which becomes a
Customer (accounts, contacts, contracts, notes, follow-up tasks, file attachments). Customer
records also support GDPR-style PII erasure in place (`Customers.Erase` permission) separate
from hard delete, for data-retention compliance. Customers Index supports CSV export of the
current filtered list. An Opportunity's "Share Package" action emails the customer whichever of
its NeedsAssessment/Proposal/Quote PDFs actually exist as one message, instead of sending them
separately.

### Sales & Quoting
Quote → Order → Invoice → Payment, each stage converting the last (line items copy forward
automatically). A daily `OrderReadyToInvoiceWorker` auto-issues the invoice for any non-Milestone
order that's Confirmed/Fulfilled, not yet invoiced, and whose linked Field Service Jobs (if any)
are all Completed — the same "ready" condition My Workspace's Orders Ready to Invoice list already
computed, now acted on instead of just displayed. Milestone-billed orders are deliberately excluded
(deciding which percentage to bill next stays a human call) and still go through the existing
manual "Issue Final Invoice" action. Invoice payment status (Unpaid/Overdue/PartiallyPaid/PaidInFull/Overpaid) is
always computed live from Payments, never stored — matches how Manager.io behaves. A Quote/Order
can't cross into Sent/Confirmed while its computed margin sits below the admin-editable
`Erp.Sales.MarginFloorPercent` setting (Administration → App Settings) — a `Sales.OverrideMarginGate`
holder can override with a logged reason; anyone else's override reason instead files a request
in the Escalations queue for a manager to approve. An order's deposit milestone percent on
confirmation is admin-editable (`Erp:Sales:SalesDefaultDepositPercent`, default 50%) rather than
hardcoded. Quote/Order/PurchaseOrder Detail pages and Invoice Detail's "Request Payment" action
can share a WhatsApp deep link (prefilled message, opens `wa.me`) alongside the existing email
action, and Quotes/Orders Index support CSV export and a Status filter on top of the existing
search.

### Catalog & Inventory
What you sell (Catalog: products/services, tax rates, categories, price lists) plus what you
have of it (Inventory: warehouses, stock movements, stock-on-hand/low-stock reporting).

### Operations — Field Service & Projects
Field Service Jobs handle scheduled visits (Installation/Maintenance/Repair/Inspection), with
technician notes and parts consumption. Projects (when the Project Management module is
enabled) track larger one-off engagements with tasks and project-level P&L. A Project can
convert directly into a `CustomerContract` — the "project feeds the recurring contract" link —
via a prefilled Contract creation page rather than a bespoke wizard.

### Service Management
The ITIL-style core: **Tickets** (incidents — something reported as broken, with SLA tracking,
reopen-rate analytics, and security-breach classification/containment tracking) and
**Problems** (root-cause records, distinct from Tickets per ITIL4). Also houses **Warranty
Claims** and, when their respective feature toggles are on, Service Catalog, Service Requests,
Asset Management (CMDB), and Change Enablement — see below.

### Procurement
Vendor directory (kept independent of the PO workflow, same split as Catalog/Sales), purchase
orders, and supplier invoices.

### Accounting
Full double-entry ledger: journal entries, chart of accounts, currencies/exchange rates, fixed
assets, bank accounts with reconciliation, budgets, fiscal period locking, recurring journal
templates, and FX revaluation. Financial reports live alongside it: Trial Balance, Income
Statement, Balance Sheet, AR/AP Aging, Cash Flow, Budget Variance, Currency Revaluation.

### Partners — Directory & Agents
Partner/agent directory. (Commission tracking itself is the toggleable **Partner Commission**
module below — the directory is core, the commission math is optional.) When Partner Commission
is on, accepting a Proposal (`ProposalAppService.ConvertToQuoteAsync`) auto-creates a Commission
for any Partner/Agent already tagged on the Opportunity, using that party's own standing
rate/basis/trigger and the Opportunity's `EstimatedValue` — no more retyping numbers that already
existed on the partner record. An `OnClientPayment`-triggered commission created this way starts
Pending with no invoice yet attached; `CommissionAutoPayableService` resolves it once a real
Invoice is fully paid by tracing that invoice back to its Opportunity (Invoice → Order → Quote →
Proposal). A manual "New Commission" is still there for anything the auto-create skips (no
`EstimatedValue` yet, a deal with no Partner/Agent tagged, or a correction).

### Portal
External-facing pages for Customer and Vendor logins. Portal pages never reuse the internal
staff AppServices — they query repositories directly, scoped to `PortalUserId == CurrentUser.Id`,
so a portal permission can never leak another customer's or vendor's data.

### My Workspace
A personal "what's mine" view — open Tickets and upcoming Field Service jobs assigned to the
current user, plus a pending-approvals count if they can decide on Deletion Requests. Also
surfaces overdue/due-soon CustomerTask and ProjectTask reminders, and an "Orders Ready to Invoice"
section that flags any of the salesperson's own orders still awaiting invoicing — a manual
"Issue Final Invoice" click is still available here for Milestone-billed orders, but every other
order this list would have shown is now invoiced automatically overnight (see `OrderReadyToInvoiceWorker`
below), so in practice this section stays empty except for the milestone case.

### Governance
- **Deletion Approvals** — deleting one of 7 top-level records (Customer, Vendor, Order,
  Invoice, Ticket, FieldServiceJob, PurchaseOrder) either happens immediately (if you hold
  `DeletionApprovals.Decide`) or files an approval request instead.
- **Escalations** — a generic version of the same maker/checker idea, for actions other than
  deletion: any blocked action can file an `EscalationItem` (carrying whichever permission is
  needed to decide it, plus a JSON payload of parameters) instead of hard-failing. A registered
  `IEscalationActionHandler` carries out the action on approval. Used by the Sales margin gate
  (see above) and by HR's Leave Request approval; designed so each new consumer is just a new
  handler class, not a change to this page or AppService.
- **Workflow Monitor** — cross-module visibility into records moving through approval/workflow
  stages.

### Administration
Module Toggles (turn optional modules on/off), App Settings (business-tunable values like
Ticket SLA hours per priority and contract-expiry alert lead time — not developer config),
Audit Logs (read-only viewer over every request/entity change ABP already records).

### Reports
Cross-cutting analytics that isn't a financial statement: workflow monitor, sales analytics,
stock on hand/low stock, support analytics (including reopen-rate trend), audit logs.

### UX & error handling (system-wide)
A consistency pass, not a module: `Filters/GlobalPageExceptionFilter` catches any uncaught
exception from a Razor Page handler app-wide and turns it into a friendly toast instead of
ABP's raw `/Error` page, logging unexpected ones for developers; `wwwroot/leitor-notify.js` wires
real `abp.notify`/`abp.message` implementations onto the previously-unused (stub-only)
SweetAlert2 bundle, giving every page themed toasts and a `data-confirm="..."` attribute that
replaces the old plain `confirm()` on every delete/status-change form, with anti-double-submit
and a "Saving..." button state applied globally. `Pages/Shared/PageModelExtensions` +
`Components/StatusToast` give any handler a one-line `this.SetSuccessMessage(...)`. Vendor,
Partner, and Agent gained an `IsActive` flag (Employee/Product/Warehouse/Account/Currency already
had one) so they can be deactivated instead of deleted; hard delete is still available, still
gated by `DependencyGuard`/`DeletionGate`.

Login is a single panel now (the theme's own separate logo/title-bar-with-language-dropdown chrome
is hidden via CSS — see `Pages/Account/login.css` — since LeptonXLite's Account layout is a
precompiled view with no safe direct override; the app's own logo/name render inside the login
card instead). The language switcher is gone app-wide; culture resolves purely from the browser's
`Accept-Language` header (`ErpModule.ConfigureLocalization` strips the cookie provider that used
to permanently override it once clicked). App name/logo/favicon are settings-backed
(`Erp.Branding.*`, Administration → Operations & Config → Branding tab) instead of hardcoded —
`ErpBrandingProvider` and `Components/BrandingStyle` are what actually apply them.

---

## Optional modules (off by default — enable per deployment)

| Module | What it adds |
|---|---|
| **Project Management** | `Project`/`ProjectTask` entities, project-tagged journal entries, project-level P&L reporting. |
| **Tax Compliance** | VAT return report (Output VAT exact from invoice lines, Input VAT approximated) and withholding tax posting. |
| **Service Catalog** | Reference list of the standardized services the business offers, for consistent quoting/service requests. Seeded with 25 real Leitor services on first run — the 13 retainer services (one per `ContractServiceScope` flag), the 7 project types, and the 5 cybersecurity assessment types — editable/extendable afterward via the Service Catalog pages. |
| **Service Request Management** | `ServiceRequest`, deliberately separate from Ticket — ITIL4 distinguishes fulfilling a request from resolving an incident. |
| **Asset Management (CMDB)** | `ConfigurationItem` + relationship graph, plus encrypted `AssetCredential` storage (admin logins/secrets, gated by a separate `RevealCredentials` permission) and security posture fields (endpoint protection, backup verification, patch status). This is what the recurring-retainer's "manage your network gear" promise is actually built on. |
| **Knowledge Management** | `KnowledgeArticle` library; a resolved Ticket can be promoted straight to a knowledge article. |
| **Point of Sale** | Register sessions (open/close/cash-count) and till sales, for hardware/software resale walk-in transactions. |
| **Partner Commission** | Commission calculation/tracking on top of the core Partner/Agent directory. |
| **Cybersecurity** | The upsell-tier module: `SecurityAssessment` records (Vulnerability/CyberRisk/PolicyReview/AwarenessTraining/BackupDrReview, with risk rating and status tracking). This is stream 3 of the business model made concrete. |
| **Change Enablement** | `ChangeRequest` tracking (tiered: Standard/Normal/Emergency, with approval gating on Normal+) for deliberate changes to a Configuration Item — patches, config changes, migrations — kept separate from Tickets, which model something reported broken. Depends on Asset Management being meaningful (nothing to change without a CI). |
| **Shared Calendar** | Standalone `CalendarEvent` records (create/drag/reassign) merged at read time with a read-only feed of `FieldServiceJob`/`Ticket`/`ProjectTask`/`CustomerTask` dates — never a second source of truth for those, just a combined view. |
| **Human Resources** | Employee directory (`Employee`, with optional self-service login link via `UserId`); leave management (`LeaveRequest`) with an approval workflow reusing the generic `EscalationItem`/`EscalationGate` mechanism (no separate approval table); Kenya statutory payroll (`PayrollRun`/`PayrollRunLine`) — PAYE via a first-of-its-kind progressive-band calculator (`PayeCalculator`), NSSF (2-tier), SHA and the Affordable Housing Levy, posted to the ledger via `JournalPostingService.PostMultiLineAsync`. PAYE bands and NSSF tiers are admin-editable seeded tables (`PayeTaxBand`/`NssfTier`, versioned by `EffectiveFrom` — a payroll run only uses the most recent version as of its period end, never stacking older rate-table generations); SHA/Housing Levy/PAYE personal relief are single tunable settings. **All seeded/default figures are best-effort as of implementation time and must be verified against current KRA/NSSF/SHA published tables before running real payroll** — see the Payroll > Tax Bands admin page. |

---

## Where a client relationship touches which modules

A typical engagement moves through the system like this:

**Lead → Opportunity (+ Needs Assessment, Proposal) → Customer → Quote → Order → Invoice
→ Payment**, with a **Project** or **Field Service Job** doing the delivery work, a
**Customer Contract** (with a service-scope checklist matching the 13-item retainer pitch)
turning that delivery into a recurring relationship, **Configuration Items** in the CMDB
tracking what was installed, **Tickets/Problems** handling what breaks afterward, and — once
the relationship matures — **Cybersecurity** assessments as the upsell layer. **Accounting**
sits underneath all of it, posting every invoice, payment, and supplier bill to the ledger.

---

## Notes on architecture (for whoever maintains this next)

- Built on the open-source ABP Framework (not ABP Commercial), single-layer template, MVC/Razor
  Pages, PostgreSQL, deployed via Docker/Coolify. See [README.md](README.md) for run/deploy
  instructions.
- Multi-tenancy is off — this is Leitor's internal system, not a multi-company SaaS product.
- The module toggle mechanism reuses ABP's own Feature Management module rather than a bespoke
  enabled-modules table. See `Features/ErpFeatures.cs`, `Features/ErpFeatureDefinitionProvider.cs`,
  and `Pages/Administration/ModuleToggles/`.
- Permission groups are defined in `Permissions/ErpPermissions.cs` — generally one group per nav
  section, with extra fine-grained permissions only where an action is genuinely a distinct,
  rarer responsibility (e.g. `Assets.RevealCredentials`, `Changes.Approve`,
  `FiscalPeriods.Manage`).
- Global search: a floating search trigger (bottom-right on every page, `Ctrl+K`/`Cmd+K`) queries
  Customers, Leads, Tickets, and Invoices by name/number — see `Services/Search/GlobalSearchAppService.cs`
  and `Pages/Search/Index.cshtml.cs` (a JSON-only endpoint, not a page users navigate to
  directly). Each entity type's results are gated on that module's own view permission. Added
  2026-08-17 after a usability audit flagged "no way to find a record without already knowing
  which of ~30 modules owns it" as the app's single highest-friction gap.
- UI design system ("Warm Sunrise"): every color/shape/shadow value lives in one file,
  `wwwroot/leitor-tokens.css` — both the main app's `leitor-theme.css` and the login page's own
  `Pages/Account/login.css` reference its `var(--leitor-*)` custom properties rather than each
  keeping an independent copy of the palette. See [LOGIN_PAGE_GUIDE.md](LOGIN_PAGE_GUIDE.md) for
  why the login page needs its own direct `<link>` tags instead of the shared bundle. Most
  Create/Edit forms open as an in-page overlay modal (AJAX fetch + JSON redirect) instead of a
  full navigation — see `Pages/Shared/OverlayRequest.cs` and `wwwroot/leitor-layout.js`'s
  `initFormOverlay()`; copy the pattern from any existing Create/Edit page pair when adding one
  (a few pages with embedded line-item sub-tables or multi-handler edit flows are deliberately
  excluded — full-page navigation there instead).
- Mobile bottom nav: on narrow viewports, `MobileBottomNavViewComponent` renders a fixed,
  permission-gated strip (Home/My Workspace/Customers/Sales/Tickets) — the left sidebar stays the
  primary nav on desktop. Same `LayoutHooks.Body.Last` extension point as the overlay-modal shell
  and global search (see `ErpModule.ConfigureLayoutHooks`), since LeptonXLite's own layout is a
  precompiled Razor Class Library with no source to override directly.
- PWA installability: `wwwroot/manifest.json` + `wwwroot/service-worker.js` (app-shell static
  assets only — CSS/JS/images/fonts — never pages or API calls, so an ERP never serves stale
  business data from a cache) + `wwwroot/leitor-pwa.js` (registers the service worker, then shows
  a dismissible "Install Leitor ERP" banner on first mobile visit — Chromium's real `prompt()` via
  `beforeinstallprompt`, or an instructional banner on iOS Safari, which has no install-prompt API
  of its own). `PwaHeadViewComponent` adds the manifest link/theme-color/apple-touch-icon tags via
  the same `LayoutHooks.Head.Last` mechanism as `ThemeFontsViewComponent`; `Pages/Account/Login.cshtml`
  carries the identical tags + script directly (same reason as `LOGIN_PAGE_GUIDE.md`'s existing
  direct-`<link>` pattern) since a first-time visitor's very first page is the login screen, not
  an authenticated one. App icons live at `wwwroot/images/pwa/` — generated from the token
  palette's `--leitor-primary`/`--leitor-primary-dark` gradient, not the existing LeptonX sample
  logo (`wwwroot/images/logo/leptonx/`), which is a mismatched blue never restyled to the app's
  actual amber brand.
- Mobile header contrast (2026-08-18): `leitor-theme.css`'s `.lpx-header` override forces a light
  translucent background — LeptonXLite ships assuming a dark/colored header, so its icon-only
  buttons (mobile menu toggle, search, notifications) likely hardcode a light icon color that
  would go near-invisible against it. Forced to `var(--leitor-ink)` as a defensive fix; **not
  live-verified in a browser** (local Postgres credentials in `appsettings.json` don't match the
  actual local server — a standing gap across several sessions now) — confirm on a real phone
  before considering this closed.
