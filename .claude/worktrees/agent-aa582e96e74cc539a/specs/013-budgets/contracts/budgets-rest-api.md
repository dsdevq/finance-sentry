# REST API Contract: Budgets (013)

**Base path**: `/api/v1/budgets`
**Auth**: All endpoints require `Authorization: Bearer <token>`
**Controller**: `BudgetsController` in `FinanceSentry.Modules.Budgets.API.Controllers`

---

## GET /api/v1/budgets

List the authenticated user's budgets (limits only, no spending data).

### Response `200 OK`

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "category": "food_and_drink",
      "categoryLabel": "Food & Drink",
      "monthlyLimit": 400.00,
      "currency": "USD",
      "createdAt": "2026-05-01T10:00:00Z"
    }
  ],
  "totalCount": 3
}
```

### Error Responses

| Status | `errorCode` | Condition |
|---|---|---|
| `401` | `UNAUTHORIZED` | Missing or invalid token |

---

## POST /api/v1/budgets

Create a new budget for a category.

### Request Body

```json
{
  "category": "food_and_drink",
  "monthlyLimit": 400.00
}
```

| Field | Type | Required | Notes |
|---|---|---|---|
| `category` | string | Yes | Must be a valid internal taxonomy key (see data-model.md) |
| `monthlyLimit` | number | Yes | Must be > 0 |

### Response `201 Created`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "category": "food_and_drink",
  "categoryLabel": "Food & Drink",
  "monthlyLimit": 400.00,
  "currency": "USD",
  "createdAt": "2026-05-01T10:00:00Z"
}
```

### Error Responses

| Status | `errorCode` | Condition |
|---|---|---|
| `400` | `BUDGET_INVALID_CATEGORY` | Category not in internal taxonomy |
| `400` | `BUDGET_INVALID_LIMIT` | Limit ≤ 0 |
| `409` | `BUDGET_DUPLICATE_CATEGORY` | Budget for this category already exists |
| `401` | `UNAUTHORIZED` | Missing or invalid token |

---

## PUT /api/v1/budgets/{id}

Update a budget's monthly limit.

### Path Parameters

| Param | Type | Description |
|---|---|---|
| `id` | UUID | Budget identifier |

### Request Body

```json
{
  "monthlyLimit": 500.00
}
```

### Response `200 OK`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "category": "food_and_drink",
  "categoryLabel": "Food & Drink",
  "monthlyLimit": 500.00,
  "currency": "USD",
  "createdAt": "2026-05-01T10:00:00Z"
}
```

### Error Responses

| Status | `errorCode` | Condition |
|---|---|---|
| `400` | `BUDGET_INVALID_LIMIT` | New limit ≤ 0 |
| `404` | `BUDGET_NOT_FOUND` | Budget not found or belongs to different user |
| `401` | `UNAUTHORIZED` | Missing or invalid token |

---

## DELETE /api/v1/budgets/{id}

Delete a budget. Hard delete.

### Path Parameters

| Param | Type | Description |
|---|---|---|
| `id` | UUID | Budget identifier |

### Response `204 No Content`

### Error Responses

| Status | `errorCode` | Condition |
|---|---|---|
| `404` | `BUDGET_NOT_FOUND` | Budget not found or belongs to different user |
| `401` | `UNAUTHORIZED` | Missing or invalid token |

---

## GET /api/v1/budgets/summary

Get budgets with spending totals for a given calendar month.

### Query Parameters

| Param | Type | Required | Default | Notes |
|---|---|---|---|---|
| `year` | int | No | Current year | 4-digit year |
| `month` | int | No | Current month | 1–12 |

### Response `200 OK`

```json
{
  "year": 2026,
  "month": 5,
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "category": "food_and_drink",
      "categoryLabel": "Food & Drink",
      "monthlyLimit": 400.00,
      "spent": 312.50,
      "remaining": 87.50,
      "isOverBudget": false,
      "currency": "USD"
    }
  ],
  "totalLimit": 1200.00,
  "totalSpent": 750.00
}
```

**Calculation**: `spent` = sum of debit transaction amounts where `NormalizedCategory(MerchantCategory) == budget.Category` for transactions in the requested calendar month. Excludes pending and inactive transactions.

### Error Responses

| Status | `errorCode` | Condition |
|---|---|---|
| `400` | `BUDGET_INVALID_PERIOD` | Month not in 1–12 or year < 2020 |
| `401` | `UNAUTHORIZED` | Missing or invalid token |

---

## Error Body Schema

```json
{
  "errorCode": "BUDGET_NOT_FOUND",
  "message": "Budget not found."
}
```

---

## New `errorCode` Values (add to `error-messages.registry.ts`)

| Code | Frontend Message |
|---|---|
| `BUDGET_NOT_FOUND` | "Budget not found." |
| `BUDGET_DUPLICATE_CATEGORY` | "A budget for this category already exists." |
| `BUDGET_INVALID_CATEGORY` | "Invalid budget category." |
| `BUDGET_INVALID_LIMIT` | "Budget limit must be greater than zero." |
| `BUDGET_INVALID_PERIOD` | "Invalid budget period." |
