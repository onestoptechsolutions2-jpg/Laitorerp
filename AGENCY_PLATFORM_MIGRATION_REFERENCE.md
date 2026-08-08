# Leitor ERP → Agency Operations Platform: Migration & Reference Document

**Purpose:** Section 33 of the *Agency Operations ERP — Master Architecture & Product Brief* calls
for documenting the existing system before any rewrite decision is made. This is that document —
an accurate inventory of what Leitor ERP actually is today, mapped against the brief's proposed
generalized platform concepts, so a future rewrite/migration conversation starts from facts rather
than assumptions.

**This is not a rewrite plan.** No code changes accompany this document. It exists to answer one
question: *if we were to build the platform described in the brief, what already exists, what
would carry over, what would need to be generalized, and what doesn't exist at all yet?*

**One factual correction to the brief:** Section 33 refers to "the existing ABP + Blazor
application." The existing application is **ABP Framework + ASP.NET Core Razor Pages (MVC)**, not
Blazor. There is no Blazor code anywhere in this codebase. This matters because the brief's target
architecture (Section 28) also specifies Blazor — that assumption should be revisited on its own
merits rather than carried forward from a mismatched premise about the current system.

---

## 1. What Leitor ERP Is Today

| | |
|---|---|
| **Framework** | ABP Framework 10.5.0 (open-source/community edition) |
| **Runtime** | .NET 10 |
| **UI** | ASP.NET Core Razor Pages (server-rendered MVC), Bootstrap-based LeptonX Lite theme |
| **Database** | PostgreSQL, single connection string, single schema |
| **ORM** | Entity Framework Core (`Volo.Abp.EntityFrameworkCore.PostgreSql`) |
| **Auth** | ASP.NET Core Identity + OpenIddict (ABP's built-in auth stack) |
| **Multi-tenancy** | Explicitly **disabled** (`ErpModule.IsMultiTenant = false`) — this is already a single-tenant-per-deployment system, not a shared-database SaaS |
| **Background jobs** | ABP's built-in `AsyncPeriodicBackgroundWorkerBase` (in-process, no external queue) |
| **PDF generation** | QuestPDF (Community license) |
| **Deployment** | Docker (`Dockerfile`, `docker-compose.yml`/`docker-compose.override.yml`), targeting Coolify in production |
| **Client assets** | Committed to git under `wwwroot/libs` (no Node/npm build step) |

**The single-tenant-per-deployment model is already exactly what the brief's "container-per-business"
clarification asks for.** `IsMultiTenant = false` means there is no shared-database tenant
isolation to rip out — each Leitor ERP deployment is already one database, one business. The
brief's Section 3 ("Multi-Tenant Architecture") describes a shared-platform-with-tenants model that
the user has since explicitly ruled out in favor of one container/database per business. That
significantly simplifies what a future platform would need: **no tenant abstraction layer is
required at all**, only per-deployment *configuration* (branding, terminology, pipeline shape),
which is a much smaller problem than multi-tenancy.

### ABP modules currently in use (`ErpModule.cs`'s `[DependsOn(...)]` list)

- **Identity + OpenIddict** — user/role management, login, token issuance
- **Permission Management** — the permission-group-per-module system every page/service is gated by
- **Feature Management** — powers the module on/off toggle framework (see §4)
- **Setting Management** — powers the admin-configurable settings screen (SLA hours, alert
  lead times) and email/SMTP config
- **Audit Logging** — automatic request/entity-change audit trail
- **Tenant Management** — installed but unused in practice (`IsMultiTenant = false`); could be
  removed cleanly if a future rewrite drops ABP
- **Mapperly** — compile-time DTO↔entity mapping (not AutoMapper)
- **Swashbuckle** — Swagger/OpenAPI generation over the auto-exposed REST API

A from-scratch rewrite would need to **rebuild all of the above from scratch** (or find equivalent
libraries) if ABP is dropped: permission checking, a settings store, an audit log, background job
infra, and the Identity/OpenIddict auth stack. This is the single biggest cost of the brief's
"do not use ABP" instruction (Section 28) — it is not just a UI framework swap.

---

## 2. Current Module Inventory

Every module below is real, built, and in production use unless marked otherwise. Modules marked
**(togglable)** are gated by `ErpFeatures.*` — off by default, turned on per-deployment via
`Pages/Administration/ModuleToggles`.

### 2.1 CRM / Sales pipeline (always-on core)

The actual, working chain is:

```
Lead → Customer → Opportunity → NeedsAssessment → Proposal → Quote → Order → Invoice → Payment
```

- **Lead** (`Entities/Customers/Lead.cs`) — Source, Channel, Direction, Status, AssignedToUserId,
  `LeadTouch` interaction log. Converts to `Customer` via `LeadAppService.ConvertToCustomerAsync`
  (copies Name/Email/Phone/Notes only — `LeadTouch` history stays keyed to the Lead, not carried
  onto the Customer record).
- **Customer** (`Entities/Customers/Customer.cs`) — the account-level master record: Status,
  AccountOwnerUserId, DefaultPaymentTerms, DefaultPriceListId, PortalUserId (links to a
  self-service portal login). Has `CustomerContact`, `CustomerNote`, `CustomerTask`,
  `CustomerAttachment`, `CustomerContract` as child collections.
- **Opportunity** (`Entities/Opportunities/Opportunity.cs`) — Status, EstimatedValue,
  AssignedToUserId, linked `NeedsAssessment` (a fixed-shape discovery/survey record — see §5 for
  the gap against the brief's generalized Assessment engine) and `Proposal`.
- **Proposal** (`Entities/Opportunities/Proposal.cs`) — versioned, lock/unlock-for-revision
  workflow, PDF generation, delivery-channel logging (email or manual/WhatsApp) via
  `WorkflowStageEvent`.
- **Quote → Order** (`Entities/Sales/Quote.cs`, `Order.cs`) — line-item documents with
  Product/UnitPrice/Quantity/DiscountPercent/TaxRate snapshotted per line at add-time. Orders
  support milestone-based partial billing (`OrderPaymentMilestone`).
- **Invoice → Payment** (`Entities/Sales/Invoice.cs`, `Payment.cs`) — Order→Invoice conversion is
  fully automatic (every line, tax, discount, currency, payment terms copied with zero manual
  re-entry — verified in a recent audit of this codebase). Payment auto-posts to the GL
  immediately on save.

**Governance layer wrapping this whole chain:** `WorkflowStageEvent` (`Entities/Governance/`) is an
append-only audit trail recording every meaningful transition (Lead qualified, Proposal sent,
Order confirmed, Invoice issued, etc.) against any entity in the chain — this is the closest thing
the current system has to the brief's "trigger → condition → action" workflow engine, except it's
a **passive log**, not an active rules engine (nothing *reacts* to a stage event; each transition's
side effects are hardcoded in the relevant C# service method, not configured).

### 2.2 Catalog / Inventory (always-on core)

- **Product** (`Entities/Sales/Product.cs`) — Name, Sku, Barcode, Description, Type
  (Product/Service), UnitPrice, Cost, TaxRateId, CategoryId, IsBundle (explodes into component
  lines on a Quote/Order), TrackInventory/ReorderPoint/ReorderQuantity.
- **ProductCategory**, **ProductBundleItem**, **PriceList** / **PriceListItem** (per-price-list
  override price, not currently discount-based), **ProductVendor** (which Vendor(s) supply a
  Product, at what cost, with what lead time, and which one is preferred).
- **Warehouse** / **StockMovement** — multi-warehouse, append-only stock ledger (`Receipt` /
  `Issue` / `AdjustmentIncrease` / `AdjustmentDecrease` / `TransferOut` / `TransferIn`); quantity
  on hand is always summed live from this ledger, never stored as a running balance.
  `InventoryPostingService.PostAsync` is a single reusable static helper every stock-affecting
  module (Sales, Purchasing, POS) already posts through.

**Explicitly not built:** product variants/attributes (size/color/model), batch/lot numbers, serial
numbers, expiry dates, product images, weight/dimensions, warranty terms stored on the product
itself, or a Bill of Materials. `Product` is a flat, single-SKU-per-record entity.

### 2.3 Procurement (always-on core)

```
Vendor → PurchaseOrder → GoodsReceipt → SupplierInvoice → VendorPayment
```

Three-way match is real and enforced (`GoodsReceiptAppService` blocks over-receiving against the
PO quantity). Goods Receipt → Supplier Invoice line copying is fully automatic. **There is no RFQ
step** — the brief's desired `Requirement → Procurement Requirement → RFQ → Supplier → PO` chain
skips straight from Vendor to PO today. `Vendor` has a single email/phone/address (no
`VendorContact` equivalent to `CustomerContact`), no default price list, no default payment terms,
and no purchase-history/performance-metrics view (there is in fact no Vendor Detail page at all —
only Index/Create/Edit).

### 2.4 Accounting (always-on core)

A complete, real double-entry general ledger — not a simplified approximation:

- Chart of Accounts (`Account`, typed Asset/Liability/Equity/Revenue/Expense, with
  `SystemAccountRole` flags so code can find "the Cash account" etc. programmatically)
- `JournalEntry` / `JournalEntryLine` — manual entries and ~10 auto-posting call sites (Invoice,
  Payment, SupplierInvoice, VendorPayment, Order fulfillment COGS, POS sales)
- Multi-currency (`Currency`, `ExchangeRate`, daily sync worker against Open Exchange Rates)
- Fixed Assets & straight-line Depreciation
- Bank Accounts & manual-match reconciliation
- Budgets (per account/month/year) vs. Actuals variance reporting
- Fiscal Period locking + "Year-End Close" (locks periods; does **not** post a traditional
  GL-zeroing closing entry, since Retained Earnings is always computed live from inception —
  a deliberate design choice, not an oversight)
- Recurring journal templates (daily worker posts due ones)
- Multi-currency revaluation (unrealized FX gain/loss on open foreign-currency AR/AP)
- Full report suite: Trial Balance, Income Statement, Balance Sheet, Cash Flow Statement
  (indirect method), AR/AP Aging, Customer/Vendor Statements of Account
- Kenya-specific VAT Return report (Output VAT exact, Input VAT approximated — a documented scope
  cut, not silent)

This is the single largest, most mature module in the system and maps directly onto the brief's
§20 requirements almost line-for-line already.

### 2.5 Field Service & Support/ITSM (always-on core, plus togglable extensions)

- **FieldServiceJob** — scheduled visits, parts consumed (`FieldServiceJobPart`), notes,
  linked to a `ConfigurationItem` (CMDB asset, if the Assets module is on).
- **WarrantyClaim** — linked to the originating job/product.
- **Ticket** / **TicketMessage** — full ITSM ticketing: type, priority, SLA due-date (computed
  once at creation from a contract's tier or a default table), reopen-count tracking,
  CSAT rating, promote-to-Knowledge-Base action.
- **Problem** (togglable is *not* the right word — this is always-on) — ITIL4-style root-cause
  grouping across multiple related Tickets.
- **ServiceCatalogItem** / **ServiceRequest** (togglable) — a formal service catalog and a
  separate Request-fulfillment flow distinct from incident Tickets.
- **ConfigurationItem** / **ConfigurationItemRelationship** (togglable, `ErpFeatures.AssetManagement`)
  — a minimal CMDB: asset register with a small relationship graph (DependsOn/PartOf/ConnectsTo).
  **This is very close to what your managed-services model (client network infra, servers, asset
  register per client) actually needs** — see §6.
- **KnowledgeArticle** (togglable) — Draft/Published/Archived KB articles, can be promoted
  directly from a resolved Ticket.

### 2.6 Projects (togglable, `ErpFeatures.ProjectManagement`)

`Project` / `ProjectTask` with project-based accounting — `JournalEntryLine.ProjectId` is an
optional tag that lets a `ProjectReportAppService` sum a project's own P&L straight from the
existing GL (no parallel accounting system). An `Order` can optionally attribute itself to a
Project.

### 2.7 Point of Sale (togglable, `ErpFeatures.PointOfSale` — just built this session)

Till/register sessions (`PosSession`, one open session per warehouse), `PosSale` / `PosSaleLine` /
`PosPayment` (split-tender support), paid in full at time of sale — posts stock + GL immediately
through the same `InventoryPostingService`/`JournalPostingService` helpers every other sales
channel uses. Fast product search by name/SKU/barcode (the first AJAX typeahead pattern in the
app). Void reverses both stock and GL with equal-and-opposite postings.

### 2.8 Tax Compliance (togglable, `ErpFeatures.TaxCompliance`)

Kenya-scoped: VAT + withholding tax on vendor payments, VAT Return report.

### 2.9 Governance & Administration (always-on core)

- **DeletionRequest / DeletionGate** — maker-checker: most roles' delete actions file a request
  instead of deleting immediately; a small set of "Decide" permission holders approve/reject.
- **Module Toggles** (`Pages/Administration/ModuleToggles`) — custom on/off switch UI for the 7
  `ErpFeatures.*` flags, backed by ABP's real Feature Management module (not a bespoke table).
- **App Settings** (`Pages/Administration/AppSettings`) — a curated subset of business-tunable
  values (Ticket SLA hours per priority, contract-expiry alert lead time) exposed for admin
  editing, backed by ABP's real Setting Management module.
- **Data Retention** — a daily worker hard-deletes old soft-deleted rows for a conservative entity
  list; a manual "Erase Customer Data" action anonymizes PII in place (GDPR-style, not a full
  delete, so historical Orders/Invoices keep referential integrity).
- **My Workspace** — a personal "what's assigned to me" view (open Tickets/Jobs), separate from
  the org-wide Dashboard.
- **Client/Vendor self-service portals** — `Customer.PortalUserId` / `Vendor.PortalUserId` link an
  external party to a login that shows them their own orders/invoices/tickets, gated purely by
  that link's presence (no separate permission model for portal users).

### 2.10 Explicitly not built at all

- **HR** — no Employee, Department, Payroll, Attendance, Leave, or Performance Review entities
  exist anywhere in the codebase.
- **Manufacturing** — no Bill of Materials, no Manufacturing Order, no component-consumption flow.
- **Partner / Agent / Subcontractor ecosystem** — `Vendor` models the *supply* side (who sells us
  things) but there is no modeling at all of partners who *deliver on our behalf*, referral
  agents, subcontractor day-rate agreements, or revenue-sharing.
- **Commission / revenue-sharing engine** — nothing exists here at any level.
- **Territory management** — no geography/region entity or routing-by-territory logic.
- **CMS** — no page/blog/media management.
- **Configurable pipelines, terminology, custom fields, or form builder** — every entity's stage
  flow is a fixed C# enum; every label is a hardcoded string in one `ErpResource` localization
  file; every Create/Edit page is a hand-written Razor form with a fixed field set.
- **Generic workflow (trigger/condition/action) engine** — see §2.1; only a passive audit log
  exists, not an active rules engine.

---

## 3. Business Rules Worth Naming Explicitly

These are load-bearing conventions that any future rewrite needs to either preserve or
consciously decide to change — they are not accidental, they were deliberate design calls:

- **"Compute, never store."** Stock on hand, account balances, invoice payment status, and
  customer outstanding balance are *never* stored as running totals — always summed live from the
  append-only ledger (`StockMovement`, `JournalEntryLine`, `Payment`) at read time. This trades a
  small amount of query cost for the complete elimination of an entire class of "balance drifted
  out of sync with its transactions" bugs.
- **Snapshot-at-creation for money fields.** A Quote/Order/Invoice line captures
  UnitPrice/TaxRatePercent/ExchangeRateToBase *at the moment it's added* and never recomputes them
  later, even if the underlying Product/TaxRate/ExchangeRate changes afterward. Historical
  documents are immutable in effect even though the row is technically editable.
- **Maker-checker on deletion**, not on every action — deletion specifically is treated as
  higher-risk than create/update and routed through an approval gate for most roles.
- **Feature flags for *new capability modules* only**, never for the always-on core. The line is:
  if disabling it would remove a menu section and a permission group cleanly, it's a feature flag;
  if it's foundational (Sales, Accounting, Support), it's permission-gated only.

---

## 4. Mapping the Brief's Generalized Concepts Against What Exists

| Brief concept (§) | Current Leitor ERP equivalent | Gap |
|---|---|---|
| Client Requirement as central object (§2, §8) | No single generalized entity. Closest fit is `Lead` (unqualified) + `Opportunity` (qualified) + `NeedsAssessment` (discovery), but these are three separate, CRM-specific, fixed-shape entities | Would need a genuinely new generalized `Requirement`/`Brief` entity with custom fields, or a significant refactor merging Lead/Opportunity/NeedsAssessment's shared shape |
| Configurable terminology (§5) | None — all labels are hardcoded strings in `Localization/Erp/en.json`, one string per key, no per-tenant override | Would need a display-name override layer between the entity name and what's rendered |
| Configurable pipelines (§7) | None — `LeadStatus`, `OpportunityStatus`, `TicketStatus`, etc. are fixed C# enums; transitions are validated in C# service methods, not admin-configured rules | Real gap; this is a genuinely new subsystem (a pipeline/stage definition engine + a generic status field replacing typed enums) |
| Assessment engine (§9) | `NeedsAssessment` exists but has a fixed field shape (`NeedsAssessmentType` enum + free-text fields), not a template/section/question builder | Real gap |
| Products & Services (§10) | Built — `Product.Type` is Product/Service already; pricing/tax/supplier/category all exist | Close fit; missing is Partner/Commission linkage on the product itself |
| Partner ecosystem (§11) | Not built | Real gap — `Vendor` is supply-side only |
| Agents & Subcontractors (§12) | Not built | Real gap |
| Territory management (§13) | Not built | Real gap |
| Solution design / multi-solution comparison (§14) | Not built (a Proposal is single-solution) | Real gap |
| Proposal & Quotation (§15) | Built — Proposal + Quote, PDF generation, branding via `ErpCompanyOptions` | Close fit; template is fixed per document type, not admin-configurable |
| Contracts (§16) | `CustomerContract` exists (client-side only — start/end/renewal/SLA hours per tier); no Partner/Agent/Subcontractor/Supplier agreement types | Partial — would need generalizing to a shared Contract entity across party types |
| Projects (§17) | Built — `Project`/`ProjectTask` + project-based accounting | Close fit; no templates, no client-approval workflow on milestones |
| Procurement without owned inventory / dropshipping (§18) | Partially — `ProductVendor` supports a preferred-vendor lookup for a PO line, but there's no RFQ step and no "partner ships directly to client" flow | Partial gap |
| Commission & revenue-sharing engine (§19) | Not built | Real gap — and a genuinely complex one (tiered/margin-based/trigger-conditioned calculation is a rules engine in its own right) |
| Accounting (§20) | Built, extensively — see §2.4 above | This is the strongest, most complete match to the brief of any module |
| Ticketing & Support (§21) | Built, extensively — see §2.5 | Strong match |
| CMS (§22) | Not built | Real gap |
| Notifications engine (§23) | Ad hoc — individual services call `IEmailSender` directly at specific points (e.g. contract-expiry alert worker); no generalized event→action notification config | Partial gap |
| Workflow engine (§24) | `WorkflowStageEvent` is a passive audit log of what happened, not an active trigger/condition/action engine | Real gap — this is one of the biggest, since the brief treats it as central |
| Custom fields (§25) | **Storage mechanism already exists, unused.** Every entity in this codebase (`FullAuditedAggregateRoot`) already carries an `ExtraProperties` JSON column via ABP's `IHasExtraProperties`. ABP ships a real, built-in "Module Entity Extensions" system (`Volo.Abp.ObjectExtending` / `ObjectExtensionManager`) specifically for defining typed extra properties on existing entities at startup — with validation attributes, and ABP's own auto-generated CRUD UI renders them automatically. `ErpModuleExtensionConfigurator.cs` is already scaffolded in this codebase for exactly this purpose and is currently **100% unused** (still the default template comment). | Not a real gap in the way the table implies — the missing piece is a runtime/admin-configurable version (defining new fields through a UI, at any time, per deployment) rather than the compile-time version ABP ships (defined in C# at startup, requires a rebuild+redeploy to add a field). Worth deciding whether compile-time custom fields (cheap, ships fast, needs a code change per new field) are good enough, vs. a genuine runtime field-definition engine (expensive, matches the brief literally) |
| Configurable forms (§26) | Not built — every form is hand-written Razor | Real gap |
| Branding/white-label (§27) | Partial — `ErpCompanyOptions` covers name/logo/address/phone/email for generated documents; no per-deployment theme colors, favicon, or domain config | Partial gap, but see note below — with one-container-per-business, this may not even need to be *runtime*-configurable, just set once at deployment time |
| Multi-tenancy (§3) | Deliberately disabled; superseded by the user's own "one container per business" decision | Not a gap — already the right shape for the stated deployment model |

---

## 5. What "Container Per Business" Actually Changes

Because each business gets its own deployment (own database, own container) rather than a shared
platform with tenant rows, several of the brief's requirements become **much cheaper** than a
shared-SaaS reading of the same document would imply:

- No tenant-scoping needs to be threaded through every query (ABP's multi-tenancy data filter
  exists for exactly this and is already off).
- "Branding & white-label" (§27) doesn't need a runtime tenant-switch UI — it can be `appsettings`
  per deployment, the same way `ErpCompanyOptions` already works today.
- "Configurable terminology/pipelines/forms" (§5, §7, §26) still matter, but they only need to be
  configurable **once per deployment** (at setup time or via an admin screen used rarely), not
  dynamically switchable per-request the way true multi-tenant SaaS would require.

This meaningfully narrows the actual engineering surface of "generalized platform" — most of the
brief's configurability asks are about *avoiding a source fork per client*, not about *runtime
tenant-switching*, and those are different (the second is much harder) problems.

---

## 6. A Note on Your Actual Business Model (Managed IT Services)

Based on what you've described (managed internal IT for clients — network infrastructure, servers,
ERP systems you source/install/maintain, one active MSC/AMC-style subscription per client, with
passwords/configurations/asset register kept per client) — several pieces of this already exist
and are close fits, not gaps:

- **Per-client asset register**: `ConfigurationItem`/`ConfigurationItemRelationship` (the CMDB
  module, `ErpFeatures.AssetManagement`) already models exactly this — a per-customer register of
  servers/network gear with a relationship graph, and `FieldServiceJob.ConfigurationItemId`
  already links a field visit to a specific asset.
- **Recurring subscription/contract per client**: `CustomerContract` already supports
  start/end/renewal/SLA-tier — an MSC/AMC contract is a natural fit for this entity as-is.
- **Missing today**: a secure, per-asset credential/configuration store. Nothing in the current
  schema stores passwords or device configuration data — `ConfigurationItem` has no
  encrypted-secret field, and none should be added casually (this needs a deliberate
  encryption-at-rest design, not a plain text column). This is worth scoping as its own
  conversation before any rewrite decision, since it's a genuine security-sensitive gap regardless
  of which platform direction is chosen.

---

## 7. Decisions (resolved 2026-08-08)

The three open questions above were discussed and resolved. **There is no platform rewrite.**
Leitor ERP continues as an ABP Framework + Razor Pages application, evolving incrementally:

1. **Keep ABP, extend it.** The existing permission/feature-flag/settings/audit-log/background-worker
   infrastructure stays. Where the brief's asks have a real ABP-native mechanism (e.g. custom
   fields via `ObjectExtensionManager`/`ErpModuleExtensionConfigurator.cs`, already scaffolded and
   unused), build on that rather than inventing a parallel system.
2. **Stay on Razor Pages/MVC.** No Blazor migration. The brief's "ABP + Blazor" description of the
   current system was simply wrong (§0 above) and the Blazor requirement was not a deliberate,
   independently-motivated choice.
3. **Build for Laitor's real managed-IT-services needs first; generalize opportunistically.** No
   upfront investment in configurable terminology/pipelines/a form builder/a workflow engine for
   hypothetical future tenants. Each of those gets built only once a second real business is
   actually running on the platform and needs it. Concretely, this means:
   - The credential/configuration-store gap (§6) is a real, near-term priority for Laitor's actual
     business and can be scoped and built on its own, independent of anything else in this
     document.
   - The paused POS/inheritance-fill-up plan (Phases 2-4: Customer/Vendor master data enrichment,
     CRM/Sales inheritance fixes, Purchasing inheritance fixes — including tax modeling on
     `PurchaseOrderLine`/`SupplierInvoiceLine`, which directly answers "track purchases and sales
     with related taxes") is unaffected by this conversation and can resume as-is.
   - Partner/Agent/Subcontractor/Commission modeling (§4, currently a real gap) should only be
     built once Laitor's own operations actually require it, not preemptively.