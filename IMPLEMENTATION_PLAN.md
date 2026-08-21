# Implementation Plan (Updated from chat backlog and repo state)

Date: 2026-08-21

## 1. Planning assumptions

This plan is based on the decisions captured in the project chat and confirmed by the codebase:

- Keep the app on ABP Framework + ASP.NET Core Razor Pages / MVC.
- Do not run a platform rewrite or a Blazor migration.
- Extend the current system incrementally instead of introducing a new generalized SaaS abstraction.
- Prioritize real Laitor operating needs first: managed IT services, recurring contracts, field service, and accounting fidelity.
- Generalize only when a second real business need is proven.

This matches the project direction in [AGENCY_PLATFORM_MIGRATION_REFERENCE.md](AGENCY_PLATFORM_MIGRATION_REFERENCE.md), [MODULES.md](MODULES.md), and the implemented phase-based test set in the repo, including the inheritance and relationship-field work.

---

## 2. Current backlog, synthesized from repo + chat decisions

### Priority A: Security and operational-data gap

1. Asset credential / configuration store
   - Close a real gap for managed IT clients: per-asset credentials and device configuration data.
   - Requirements:
     - encrypted-at-rest storage
     - separation of credentials from normal CMDB metadata
     - admin-only reveal workflow with explicit permissioning
     - safe audit trail without exposing secrets
   - Why now: this is one of the few clear business gaps for Laitor's actual managed-services model.

2. Asset management hardening
   - Finish the CMDB pattern around ConfigurationItem and relationship graph use.
   - Extend with operational fields needed for client infrastructure and security posture tracking.

### Priority B: Data integrity and inheritance completion

3. Phase 2 / relationship-field completion
   - Ensure cross-module references remain consistent and resolved cleanly across optional modules.
   - Confirm partner/agent names resolve correctly on service catalog and project task records.
   - Keep loose foreign-key references consistent with DependencyGuard / approval patterns.

4. Phase 3 / CRM and sales inheritance fill-up
   - Price-list-aware pricing inheritance on sales lines.
   - Customer-change repricing behavior for existing quote lines.
   - Credit-limit enforcement when confirming orders.
   - Salesperson attribution carried through Quote → Order conversion.
   - Outcome: sales workflows match operational expectations and protect margin discipline.

5. Phase 4 / procurement inheritance fill-up
   - ProductVendor-based unit-cost inheritance for purchase order lines.
   - Default warehouse inheritance for purchase orders.
   - Per-line tax modeling on PurchaseOrderLine / SupplierInvoiceLine.
   - Procurement alignment with current tax, warehouse, and supplier pricing data.
   - Outcome: purchasing and supplier accounting remain consistent and tax-compliant.

### Priority C: Workflow, UX, and operational backlog from the latest chat notes

6. Calendar event creation and save reliability
   - Fix the calendar Add Event flow so creation saves properly instead of opening a menu or failing silently.
   - Ensure lead creation from calendar-related flows behaves consistently with the rest of the app.

7. Quotation and detail-action UX
   - Replace fragmented action placement with a single action menu or action button pattern.
   - Standardize actions such as edit, PDF, email, WhatsApp, and related document actions inside a consistent action area.
   - Apply the same behavior to other list/detail screens where actions are currently spread out or awkward.

8. Confirm action and status update integrity
   - Fix the Confirm action so it produces the actual expected effect and persists status changes correctly.
   - Check all confirmation flows for final-state consistency across sales and operational records.

9. Smart document delivery and client messaging
   - Personalize outbound WhatsApp/email messages to use first name only where appropriate.
   - Generate downloadable document links for clients when documents are shared or sent.
   - Apply the document-sharing pattern in any place where a customer-facing document is needed, not just a single workflow.

10. Duplicate-data cleanup and identity alignment
    - Stop duplicate records that arise from overlapping people, contact, and employee roles.
    - Treat employees as system users when appropriate, and use identity/role-management plus HR extensions instead of creating parallel data models.
    - Align the human-resources and identity model so role/JD/employee management is handled once.

11. Assessment-based service packaging
    - Replace a standalone cybersecurity module with assessment templates/checklists that can be selected as needed.
    - Allow users to choose assessment types such as cybersecurity, CCTV, network, digital marketing, and others from a dropdown.
    - Support multiple assessments per customer and ensure the chosen assessment drives the resulting proposal.
    - Keep the assessment library as a reusable best-practice framework rather than a rigid module silo.

12. Unified service and incident handling
    - Consolidate incident, service request, and claim handling so each is captured and managed according to its category, rather than forcing everything into a single ticket model.
    - Treat service catalog entries as shareable marketing assets with smart links for digital distribution and request flows.

13. Field operations and best-practice standardization
    - Apply field-ops best-practice controls across scheduling, service delivery, and completion handling.
    - Reduce operational drift between processes by aligning field execution with the same standard patterns used elsewhere in the ERP.

14. Module coordination and consolidation review
    - Review overlapping functions/modules for possible merging or tighter coordination.
    - Remove unnecessary duplication where multiple modules are effectively serving the same operational need.

15. Workspace productivity and usability
    - Organize the workspace to reduce friction and improve day-to-day productivity.
    - Simplify the user experience around high-frequency operational tasks, especially in sales, service, and field workflows.

### Priority D: Business workflow and operational delivery

16. POS completion and inventory flow
   - Finish the till/register model and stock/GL postings for walk-in transactions.
   - Validate the same inventory-posting and accounting patterns reused elsewhere.

17. Retainer/project conversion workflow
   - Keep the recurring-contract flywheel working: project -> customer contract -> retainer relationship.
   - Ensure project and field-service data can feed the ongoing services relationship without bespoke manual re-entry.

18. Shared team calendar and approval flows
   - Keep calendar integrations as a read-only aggregation layer, not a duplicate source of truth.
   - Continue using the generic Escalation / approval patterns for exceptions and human decisions.

### Priority E: Optional expansion only when justified

19. Partner commission, cybersecurity, HR, and similar optional modules
   - Continue only when there is direct operating demand.
   - Do not build a generalized workflow engine or broad cross-tenant platform layer before a second business use case requires it.

---

## 3. Recommended execution sequence

### Phase 1 — Security and operating data foundation

Goal: close the managed-services gap before broad feature expansion.

Deliverables:
- Asset credential storage design
- encrypted secret handling and permission model
- CMDB asset security and configuration data model
- admin-only credential reveal workflow and audit logging

Exit criteria:
- secrets are not stored as plain text
- credential access is session-scoped and permission-gated
- field-service operations can reference a secure asset inventory without data leakage

### Phase 2 — Sales and account-data integrity

Goal: complete the CRM and sales inheritance work already started.

Deliverables:
- price-list inheritance and repricing rules
- credit-limit validation on order confirmation
- order conversion attribution preservation
- margin gate and approval consistency checks

Exit criteria:
- quote/order creation and confirmation do not drift from customer pricing or credit policy
- no sales workflow depends on manual re-entry of already-known data

### Phase 3 — Procurement and tax accuracy

Goal: fix supplier-side data inheritance and tax correctness.

Deliverables:
- ProductVendor cost inheritance
- default warehouse behavior on purchase orders
- supplier invoice line tax modeling and totals
- purchase-to-stock-to-accounting continuity

Exit criteria:
- PO creation and supplier invoice posting resolve cost and tax consistently
- inventory and ledger data stay aligned with supplier lines

### Phase 4 — POS and workflow completion

Goal: finish the remaining operational flows that feed the business model.

Deliverables:
- POS sessions and sales posting
- inventory/GL reconciliation at point of sale
- void/reversal consistency
- operational reporting tied to real inventory and accounting events

Exit criteria:
- walk-in sales can be processed without breaking the financial controls already used elsewhere

### Phase 5 — Optional modules and opportunistic generalization

Goal: only expand if operating demand is real.

Deliverables:
- partner commission refinement when pipeline volume justifies it
- cybersecurity module maturity as a sales upsell layer
- HR/payroll improvements when the business expands sufficiently
- broader configuration features only if a second deployment genuinely needs them

Exit criteria:
- each optional feature has a direct business case
- no new generalized platform layer is introduced without proven demand

---

## 4. Delivery principles

- Prefer incremental release and verification over broad rework.
- Keep one source of truth: the ERP's existing accounting and inventory logic should remain the canonical system.
- Avoid parallel business workflows or duplicate records unless a genuine requirement demands it.
- Treat permissions, feature flags, and approval patterns as the default controls for new functionality.
- Defer generic productization work until the business has required it in practice.

---

## 5. Immediate next actions

1. Finish the asset credential storage design and security review.
2. Validate and close the remaining sales inheritance edge cases in tests and runtime flows.
3. Confirm the procurement tax and warehouse inheritance coverage is complete and regression-safe.
4. Resume POS completion only after the core accounting and inventory matching rules are stable.
5. Keep a tight backlog: no rewrite, no speculative platform abstraction, only the next operationally necessary increments.

---

## 6. Status summary

Current status by backlog area:

- Security / credential gap: open and important
- CRM / sales inheritance: largely addressed in current phase work
- Procurement tax and cost inheritance: largely addressed in current phase work
- POS / inventory completion: in progress
- Optional generalization: deferred until demand is proven

This keeps the implementation aligned with the actual business model: managed IT services, recurring contracts, field operations, and accounting integrity.
