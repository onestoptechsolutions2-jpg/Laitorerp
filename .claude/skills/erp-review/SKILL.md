---
name: erp-review
description: "Work the 2026-08-22 UI/UX and product-gap review of this ERP (erpreview.pdf) — a 10-item backlog covering the overlay-modal Save bug on Calendar/Leads, the Quotes detail page's missing overlay pattern + actions-menu + broken Confirm dialog, WhatsApp/email share content, duplicate Category data, the Identity/HR and Cybersecurity module overlaps, Ticket/ServiceRequest/WarrantyClaim duplication, Field Service, sidebar menu redundancy, and My Workspace layout. Invoke as /erp-review to list, investigate, or fix a numbered item."
---

# /erp-review

A working backlog for the admin walkthrough review captured in `erpreview.pdf` (screenshots of erp.laitor.co.ke annotated by the user, 2026-08-22). The verbatim reporter notes are preserved in `references/erpreview-2026-08-22.md`; this file turns them into investigable, fixable items with real file pointers into this codebase.

## Usage

```
/erp-review              # list all 10 items with status (open / needs-clarification / blocked-on-#)
/erp-review <n>           # investigate item n's real cause in the codebase, propose a fix
/erp-review <n> fix       # implement item n (only after investigation confirms the diagnosis)
```

## Ground rules

- The screenshots are mostly un-annotated UI chrome (sidebar + stacked panels) — don't infer a root cause from the image alone. Read the actual `.cshtml` / `.cshtml.cs` / client-side JS driving the page before changing anything.
- Items **6b** and **7a** already have prior audit decisions on record in memory — check these before re-designing from scratch:
  - `project_cybersecurity_service_reframe_2026-08-18`
  - `project_checklist_engine_design_2026-08-18`
  - `project_consolidation_audit_2026-08-18`
  - `feature_quick_wins_automation_2026-08-18` (notes the ServiceRequest retirement was deferred as "38 call sites, too big for a quick win")
- Item **2** has no reporter caption at all — ask the user before touching Opportunities/Customers overlay behavior; don't guess a bug that may not exist.
- After fixing any item: add/extend a `Leitor.Erp.Tests` case (per `feedback_write_tests_with_features`) and update `MODULES.md` if the fix changes module boundaries (per `feedback_keep_docs_updated`).
- None of these items have been implemented yet as of 2026-08-22. Update the checkbox below as items land.

## Backlog

### 1. [ ] Calendar "New Event" and Leads "Create" don't save — Save re-opens the panel stack instead of submitting
**Symptom:** Calendar → New Event → Save does nothing but flash the parent overlay menu. CRM → Leads → New Lead → Save does the same thing instead of persisting the record.
**Files:** `Leitor.Erp/Pages/Calendar/Create.cshtml(.cs)`, `Leitor.Erp/Services/Calendar/CalendarEventAppService.cs`, `Leitor.Erp/Pages/Leads/Create.cshtml(.cs)`.
**Hypothesis:** the overlay-modal AJAX Create mechanism from `feature_overlay_modal_ui_2026-08-11` (piloted on Customers/Leads/Vendors) has a submit-button selector collision with the overlay-close handler on these two forms, so the POST never fires and the UI just falls back to the parent panel. `CalendarEventAppServiceTests.cs` exists but only covers the app service, not page-level submit wiring — that's likely why this slipped through.
**First step:** find the shared client-side script that drives the overlay AJAX submit and diff its selector/binding against a form that *does* work (Customers or Vendors Create).

### 2. [needs clarification] Opportunities / Customers search-overlay screenshots
No caption was given for this pair of screenshots — they show the standard stacked-panel overlay navigation, which is the expected pattern per `feature_overlay_modal_ui_2026-08-11`. Don't touch Opportunities/Customers overlay behavior on the assumption something's wrong here; ask the user what specifically looked off before spending time on it.

### 3. [partially fixed 2026-08-22] Quotes Detail breaks the overlay pattern, action buttons should collapse into one menu; Confirm dialog bug (c) is fixed
**Files:** `Leitor.Erp/Pages/Sales/Quotes/Detail.cshtml(.cs)`, `Leitor.Erp/wwwroot/leitor-notify.js`.
Three sub-issues on one page:
- **a. [ ] open** The page renders as a plain full page instead of inside the overlay stack used elsewhere (Customers/Leads/Vendors).
- **b. [ ] open** The action row (Edit / Convert to Order / Download PDF / Email to Customer / Share via WhatsApp / Back to list) should collapse into a single "Actions" button that reveals a dropdown — the reporter wants this pattern applied everywhere a similar action row exists (Orders, Invoices, Purchase Orders detail pages all have the same shape — see their `Detail.cshtml.cs` files).
- **c. [x] fixed** Clicking **Confirm** on the "Convert to Order?" dialog does nothing. **Root cause found and fixed**: this wasn't specific to Quotes at all — every `data-confirm` form in the app (`wwwroot/leitor-notify.js`'s `initConfirmForms()`) re-submitted via `form.requestSubmit()` after confirmation, which re-dispatches a real `submit` event through the *entire* listener chain a second time, including a buggy LeptonXLite theme jQuery handler. Confirming just fed the theme's broken handler the exact sequence that hangs it, silently. Switched to classic `form.submit()`, which bypasses the DOM event system entirely (spec: no `submit` event fires), so nothing can intercept it. This also fixed the user-reported "Leads Convert to Customer does nothing after Confirm" bug — same shared mechanism, confirmed via code trace (not directly reproduced live, since local Postgres auth is still broken per `feature_overlay_modal_ui_2026-08-11`). Verify live on both Quotes→Convert to Order and Leads→Convert to Customer next deploy.
**Fix still needed for a/b:** extend the Customers/Leads/Vendors overlay mechanism to Quotes Detail; build one shared actions-menu partial (candidate: `Pages/Shared/_ActionsMenu.cshtml`) and adopt it on Quotes/Orders/Invoices/Purchase Orders detail pages instead of duplicating a button row on each.

### 4. [ ] WhatsApp/email share text should use first name only + generate a smart, downloadable document link
**Current:** "Hello janitrix, please find attached our quotation Q-000025..." — full/raw name, and relies on an email attachment that WhatsApp can't send at all (a `wa.me` link is text-only).
**Files:** `Leitor.Erp/Pages/Shared/PhoneLinks.cs`, `Leitor.Erp/Pages/Shared/_PhoneActions.cshtml` (this already has the Kenyan-number heuristic from `feature_click_to_call_2026-08-10` — extend it, don't replace it).
**Needed:**
- a first-name-only extraction helper (small addition to `PhoneLinks.cs` or a new `NameHelpers` utility) used everywhere a customer/contact name is interpolated into an outbound message;
- a public, token-based document link (new minimal endpoint, e.g. `Pages/Documents/{token}`) that serves a read-only download of the generated PDF, so WhatsApp share has something to actually link to;
- reuse across every place a document currently gets emailed/shared: Quotes, Orders, Invoices, Contract Templates, Proposals, and the Service Catalog share link in item 7b. Build the token-link endpoint once, wire it everywhere.

### 5. [ ] Categories index shows "No results found" despite Catalog having categories
**Files:** `Leitor.Erp/Pages/Catalog/Categories/Index.cshtml.cs`, `Leitor.Erp/Services/Sales/ProductCategoryAppService.cs`.
**Hypothesis:** category records exist but the index query's filter (product-vs-service scope, or a tenant/type flag) doesn't match what Create/Edit actually persist — this is the same "enum mirrors catalogue by name, no FK" pattern already flagged in `project_cybersecurity_service_reframe_2026-08-18`. Read `ProductCategoryAppService`'s list predicate first, then compare against what Categories/Create actually writes.

### 6. [ ] Identity/HR overlap; generalize Cybersecurity into an assessment-template engine
- **a.** Employees *are* system users — Identity Management (Roles/Users) and HR (Employees) shouldn't be two disconnected trees; role/job-description management belongs as an extension of HR. This is the identity-fragmentation problem already documented and only partially fixed per `project_identity_model_audit_2026-08-18` — route through that audit's recommendation rather than redesigning fresh.
- **b.** Cybersecurity should stop being a standalone module wrapping one narrow `SecurityAssessment` entity (`Leitor.Erp/Entities/Cybersecurity/SecurityAssessment*.cs`, `Pages/Cybersecurity/Assessments/*`). Instead: a generic assessment-template/checklist engine where the user picks a category from a dropdown (Cybersecurity, CCTV, Network, Digital Marketing, etc.), each backed by its own best-practice checklist; a customer can have multiple assessments across categories; a completed assessment drives a Proposal. This is exactly the design already scoped in `project_checklist_engine_design_2026-08-18` (generic Template/Section/Item → Instance/InstanceItem, built from ContractTemplate/ProjectTask/Proposal precedents) — implement that design, retiring `SecurityAssessment*` in favor of a seeded "Cybersecurity Assessment" template.

### 7. [ ] Ticket / ServiceRequest / WarrantyClaim need one coherent model; Service Catalog needs a shareable marketing link
- **a.** `Entities/Support/TicketType.cs` already models General/Technical/Billing/Complaint/SecurityIncident, but `ServiceRequests/ServiceRequest.cs` and Warranty Claims live as separate top-level entities/sidebar items instead of Ticket subtypes or Ticket-linked records. This is the exact "ServiceRequest dead end" duplication already flagged in `project_consolidation_audit_2026-08-18`, whose retirement was deferred in `feature_quick_wins_automation_2026-08-18` as too large for a quick win (38 call sites). **This review item is confirmation to schedule that consolidation**, not a new design task.
- **b.** Service Catalog needs a public shareable link — for marketing campaigns and as a footer link on shared quotes/proposals/WhatsApp messages. Build this on the same public-token-link infrastructure as item 4; don't build it twice.

### 8. [ ] Field Service should follow the same best-practice patterns as the rest of Operations
**Files:** `Leitor.Erp/Pages/FieldService/Jobs/*`.
No specific bug was given — "apply best practice." Once items 3 and 4 land (overlay pattern + actions menu + document/share-link infra), retrofit Field Service Jobs' Detail page to match: overlay behavior, single actions menu, WhatsApp/email share of job confirmations via the token-link endpoint.

### 9. [ ] Sidebar has redundant/overlapping entries across Catalog & Inventory, Operations, and Service Management — find and merge
Service Management currently lists Tickets, Warranty Claims, Problems, Service Catalog, Service Requests, Assets, Knowledge Base, Changes side by side, while Operations separately owns Field Service. This is the same root cause as item 7a — resolve them together. Once Ticket/ServiceRequest/WarrantyClaim consolidate, the Service Management sidebar shrinks on its own; re-audit the sidebar only after that lands, not before.

### 10. [ ] My Workspace layout should be organized for productivity
**Files:** `Leitor.Erp/Pages/Workspace/Index.cshtml.cs`, `Leitor.Erp/Services/Workspace/MyWorkspaceAppService.cs`.
**Symptom:** the workspace stacks "Pending Deletion Requests / Pending Change Approvals / Pending... / Orders Ready to Invoice / My Reminders / My Open Tickets / My Upcoming Jobs" as full-width sections, several of them empty. Needs a denser, prioritized layout — hide empty sections, group into a grid/dashboard instead of a vertical stack of collapsed accordions.
