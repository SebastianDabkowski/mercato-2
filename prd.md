# Mercato -- Product Requirements Document (PRD)

Version: 1.0 Owner: Sebastian Dąbkowski Status: Draft

## 1. Overview

Mercato is a multi-vendor e-commerce marketplace that connects
independent online stores with buyers. The platform offers a Buyer
Portal, Seller Panel, and Admin Back Office.

## 2. Platform Goals

Enable multi-store selling in one platform. Provide seamless purchase
flow. Support seller operations. Generate marketplace revenue through
commissions. Prepare structure for future integrations.

## 3. User Roles

Buyer, Seller, Admin.

## 4. System Modules (Epics) and Requirements

### Epic 1: Identity & Access Management

Registration and login for buyers and sellers. Social login for buyers.
Email verification, password reset. Session management.

### Epic 2: Seller Management

Seller onboarding. Store profile. Verification. Public store page.
Payout settings.

### Epic 3: Product Catalog Management

Product CRUD. Category tree. Bulk updates. CSV import/export. Product
workflow.

### Epic 4: Product Search & Navigation

Search, filters, sorting. Category pages. Recently viewed.

### Epic 5: Shopping Cart & Checkout

Multi-seller cart. Checkout with shipping and payment. Totals
calculation. Order confirmation.

### Epic 6: Orders & Fulfilment

Order split by seller. Order statuses. Lists and detail views. Returns
initiation.

### Epic 7: Payments & Settlements

Payment provider integration. Escrow model. Payouts. Commission
invoices. Refund flow.

### Epic 8: Shipping & Delivery

Shipping configuration. Tracking numbers. CSV export. Integrations
(Phase 2).

### Epic 9: Returns & Disputes

Return requests. Seller review. Messaging. Admin escalation.

### Epic 10: Reviews & Ratings

Product reviews. Seller ratings. Admin moderation.

### Epic 11: Notifications

Email notifications. In-app notifications. Messaging (Phase 1.5).

### Epic 12: Reporting

Admin KPIs. Seller dashboards. Revenue reports.

### Epic 13: Administration

User management. Moderation. Platform settings.

### Epic 14: Integrations & APIs

Payments, shipping. Public and private APIs (Phase 2). Webhooks.

### Epic 15: Security & Compliance

GDPR support. RBAC. Audit logging.

### Epic 16: UX & Mobile

Responsive UI. Design system. PWA.

## 5. Non-Functional Requirements

Performance, scalability, security, reliability, GDPR compliance.

## 6. Out of Scope for MVP

Advanced analytics, shipping integrations, variants, promo codes, public
API.

## 7. Risks

Payment provider approval, verification complexity, regulatory
constraints.

## 8. MVP Definition

Login, onboarding, catalog, search, cart, checkout, payments, basic
returns, admin.
