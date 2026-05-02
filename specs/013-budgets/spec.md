# Feature Specification: Budgets

**Feature Branch**: `013-budgets`
**Created**: 2026-05-02
**Status**: Draft
**Input**: User description: "Needs budget CRUD + spending-vs-budget calculation by joining transaction categories. Category data from Plaid/Monobank needs to be reliable enough to aggregate against."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create and Manage Budgets (Priority: P1)

A user creates monthly spending budgets for categories (e.g. Food & Drink, Transport), sets a limit for each, and can edit or delete them.

**Why this priority**: No budget management exists without CRUD. All other stories depend on budgets existing.

**Independent Test**: Create a budget for "Food & Drink" with a $400 limit; verify it appears in the budgets list and can be edited and deleted.

**Acceptance Scenarios**:

1. **Given** a user is on the Budgets page, **When** they create a budget for a category with a monthly limit, **Then** the budget appears in the list immediately.
2. **Given** an existing budget, **When** the user edits the limit, **Then** the updated limit is reflected immediately.
3. **Given** an existing budget, **When** the user deletes it, **Then** it is removed from the list and spending data is no longer tracked against it.
4. **Given** a budget already exists for a category, **When** the user tries to create another budget for the same category, **Then** an error is shown preventing duplicates.

---

### User Story 2 - View Spending Progress Against Budgets (Priority: P1)

A user sees how much they have spent versus their budget limit for each category in the current month, with a visual progress indicator.

**Why this priority**: Core purpose of the feature — users need to see progress to take action.

**Independent Test**: With transactions in the "Food & Drink" category this month and a budget set, the progress bar shows the correct spent/limit ratio.

**Acceptance Scenarios**:

1. **Given** a user has a budget and transactions in the matching category this month, **When** they view the Budgets page, **Then** each budget shows the amount spent and the remaining amount.
2. **Given** spending exceeds the budget limit, **When** viewing the budget, **Then** it is visually highlighted as over-budget.
3. **Given** it is the start of a new month, **When** viewing budgets, **Then** all spent amounts reset to zero and reflect only the current month's transactions.
4. **Given** no transactions exist for a budget's category this month, **When** viewing the budget, **Then** spent amount shows $0 and the full limit remains.

---

### User Story 3 - Budget Period and Summary (Priority: P2)

A user can view spending summaries for past months to review how they tracked against their budgets historically.

**Why this priority**: Useful for trend analysis but not required for the core budgeting workflow.

**Independent Test**: Selecting a previous month shows the correct spent/limit for that month's transactions.

**Acceptance Scenarios**:

1. **Given** a user selects a previous month, **When** viewing the Budgets page, **Then** spending figures reflect that month's transactions only.
2. **Given** a budget was created mid-month, **When** viewing that month, **Then** only transactions after the budget creation date are counted.

---

### Edge Cases

- What if a transaction has no category? → It is excluded from all budget calculations.
- What if Plaid/Monobank returns inconsistent category names? → Categories are normalised to a fixed internal taxonomy before comparison.
- What if a user has no transactions this month? → All budgets show $0 spent.
- What currency is used for budget limits? → The user's base currency (from profile settings); no cross-currency conversion in v1.
- What happens to budgets if an account is disconnected? → Budgets remain; only transactions from still-connected accounts are counted.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Users MUST be able to create a monthly budget by selecting a category and entering a spending limit.
- **FR-002**: Users MUST be able to edit the spending limit of an existing budget.
- **FR-003**: Users MUST be able to delete a budget.
- **FR-004**: The system MUST prevent duplicate budgets for the same category.
- **FR-005**: The Budgets page MUST display, for each budget: category name, monthly limit, amount spent in the current month, amount remaining, and a visual progress indicator.
- **FR-006**: Spending amounts MUST be calculated by summing transactions in the matching category for the selected month.
- **FR-007**: Budgets MUST reset automatically at the start of each calendar month (spending recalculated from current-month transactions only).
- **FR-008**: Users MUST be able to browse budget performance for past months.
- **FR-009**: Transaction categories MUST be normalised to a consistent internal taxonomy before matching against budgets.
- **FR-010**: Over-budget categories MUST be visually distinguished from within-budget categories.

### Key Entities *(include if feature involves data)*

- **Budget**: Belongs to a user; has category (normalised), monthly limit amount, currency, and active status.
- **Category Taxonomy**: A fixed internal list of spending categories that Plaid/Monobank categories are mapped to.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create, edit, and delete a budget in under 30 seconds.
- **SC-002**: Spending figures on the Budgets page match the sum of that month's transactions in the matching category with 100% accuracy.
- **SC-003**: Over-budget status is immediately visible without the user needing to calculate anything.
- **SC-004**: Budget progress for the current month reflects all synced transactions within one sync cycle.

## Assumptions

- Budgets are monthly (calendar month); weekly or custom periods are out of scope for v1.
- The budget limit is stored in the user's base currency; no multi-currency conversion.
- Transaction categories from Plaid and Monobank will be normalised to a fixed internal taxonomy (mapping table). The existing category strings in the Transactions table are the raw input.
- At least some transaction history exists for meaningful budget tracking; the feature works with zero history but shows $0 spent.
- Budget CRUD is user-initiated; there is no AI-suggested budget generation in v1.

## Notes

- [DECISION] Category normalisation: a mapping table will translate raw Plaid/Monobank category strings to a fixed internal set. This is required before budget calculations are reliable.
- [OUT OF SCOPE] Budget notifications/alerts when approaching limit — this is deferred to the Alerts feature (budget-vs-actual alert type).
- [OUT OF SCOPE] Multi-currency budget limits.
- [OUT OF SCOPE] Weekly, custom, or annual budget periods — monthly only in v1.
- [DEFERRED] AI-suggested budget amounts based on historical spending.
