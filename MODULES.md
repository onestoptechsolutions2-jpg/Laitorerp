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
from hard delete, for data-retention compliance.

### Sales & Quoting
Quote → Order → Invoice → Payment, each stage converting the last (line items copy forward
automatically). Invoice payment status (Unpaid/Overdue/PartiallyPaid/PaidInFull/Overpaid) is
always computed live from Payments, never stored — matches how Manager.io behaves. A Quote/Order
can't cross into Sent/Confirmed while its computed margin sits below the admin-editable
`Erp.Sales.MarginFloorPercent` setting (Administration → App Settings) — a `Sales.OverrideMarginGate`
holder can override with a logged reason; anyone else's override reason instead files a request
in the Escalations queue for a manager to approve.

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
module below — the directory is core, the commission math is optional.)

### Portal
External-facing pages for Customer and Vendor logins. Portal pages never reuse the internal
staff AppServices — they query repositories directly, scoped to `PortalUserId == CurrentUser.Id`,
so a portal permission can never leak another customer's or vendor's data.

### My Workspace
A personal "what's mine" view — open Tickets and upcoming Field Service jobs assigned to the
current user, plus a pending-approvals count if they can decide on Deletion Requests.

### Governance
- **Deletion Approvals** — deleting one of 7 top-level records (Customer, Vendor, Order,
  Invoice, Ticket, FieldServiceJob, PurchaseOrder) either happens immediately (if you hold
  `DeletionApprovals.Decide`) or files an approval request instead.
- **Escalations** — a generic version of the same maker/checker idea, for actions other than
  deletion: any blocked action can file an `EscalationItem` (carrying whichever permission is
  needed to decide it, plus a JSON payload of parameters) instead of hard-failing. A registered
  `IEscalationActionHandler` carries out the action on approval. Currently used by the Sales
  margin gate (see above); designed so a future consumer is a new handler class, not a change to
  this page or AppService.
- **Workflow Monitor** — cross-module visibility into records moving through approval/workflow
  stages.

### Administration
Module Toggles (turn optional modules on/off), App Settings (business-tunable values like
Ticket SLA hours per priority and contract-expiry alert lead time — not developer config),
Audit Logs (read-only viewer over every request/entity change ABP already records).

### Reports
Cross-cutting analytics that isn't a financial statement: workflow monitor, sales analytics,
stock on hand/low stock, support analytics (including reopen-rate trend), audit logs.

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
