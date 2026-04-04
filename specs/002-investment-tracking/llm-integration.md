# LLM Integration Contract

**Document Version**: 1.0  
**Supported Providers**: OpenAI (GPT-4), Anthropic (Claude 3)  
**Last Updated**: 2026-03-21  
**Base URL (OpenAI)**: https://api.openai.com/v1  
**Base URL (Claude)**: https://api.anthropic.com/v1  

---

## Overview

This document defines the contract for Large Language Model (LLM) integration. The system uses LLMs to generate AI-powered analysis for individual assets and portfolio-level insights. Two providers are supported with automatic failover.

### Supported Models

- **Primary**: OpenAI GPT-4 Turbo (`gpt-4-turbo-preview`)
- **Fallback**: Anthropic Claude 3 Sonnet (`claude-3-sonnet-20240229`)

---

## Request/Response Format

### OpenAI API

**Endpoint**: `POST /v1/chat/completions`

**Headers**:
```http
Content-Type: application/json
Authorization: Bearer {api_key}
```

**Request Body**:

```json
{
  "model": "gpt-4-turbo-preview",
  "messages": [
    {
      "role": "system",
      "content": "You are a financial analyst..."
    },
    {
      "role": "user",
      "content": "Analyze this portfolio: ..."
    }
  ],
  "temperature": 0.7,
  "max_tokens": 1500
}
```

**Response**:

```json
{
  "id": "chatcmpl-123456",
  "object": "chat.completion",
  "created": 1622563199,
  "model": "gpt-4-turbo-preview",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "This portfolio demonstrates strong diversification..."
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 250,
    "completion_tokens": 450,
    "total_tokens": 700
  }
}
```

**Success Code**: 200

---

### Anthropic API (Claude)

**Endpoint**: `POST /v1/messages`

**Headers**:
```http
Content-Type: application/json
x-api-key: {api_key}
anthropic-version: 2023-06-01
```

**Request Body**:

```json
{
  "model": "claude-3-sonnet-20240229",
  "max_tokens": 1500,
  "messages": [
    {
      "role": "user",
      "content": "Analyze this portfolio: ..."
    }
  ],
  "system": "You are a financial analyst..."
}
```

**Response**:

```json
{
  "id": "msg-123456",
  "type": "message",
  "role": "assistant",
  "content": [
    {
      "type": "text",
      "text": "This portfolio demonstrates strong diversification..."
    }
  ],
  "model": "claude-3-sonnet-20240229",
  "stop_reason": "end_turn",
  "usage": {
    "input_tokens": 250,
    "output_tokens": 450
  }
}
```

**Success Code**: 200

---

## Analysis Types & Prompts

### 1. Individual Asset Analysis

**Use Case**: User clicks "Analyze" on a specific holding

**Expected Output Length**: 400-600 tokens

**Extracted Fields**:
- `performance_summary`: Current status and recent trends (50-200 words)
- `risk_summary`: Volatility, concentration risk (50-150 words)
- `forecast_summary`: 30-day outlook (50-100 words)
- `key_recommendations`: 3-5 actionable suggestions

---

### 2. Portfolio Analysis

**Use Case**: User views overall portfolio analysis dashboard

**Expected Output Length**: 800-1200 tokens

**Extracted Fields**:
- `performance_summary`: Overall health, trends (100-200 words)
- `risk_summary`: Concentration risk, volatility (100-150 words)
- `allocation_recommendations`: Rebalancing suggestions (100-150 words)
- `forecast_summary`: Portfolio forecast (100-150 words)
- `key_recommendations`: 5 actionable items

---

### 3. Risk Assessment (Detailed)

**Use Case**: User requests detailed risk analysis

**Expected Output Length**: 600-900 tokens

**Categories**:
- Market Risk: Exposure to market movements
- Concentration Risk: Overexposure to single assets
- Liquidity Risk: Ability to exit positions
- Currency Risk: Multi-currency exposure
- Counterparty Risk: Exchanges/platforms exposure
- Operational Risk: Data sync, account access

---

## Prompt Template Versioning

All prompts are versioned in database:

```sql
INSERT INTO llm_prompts (
  id, prompt_type, version, content, model, created_at
) VALUES (
  'asset-analysis-v1', 'asset_analysis', '1.0', '...', 'gpt-4', now()
);
```

### Versioning Strategy

- **v1.0**: Initial templates
- **v1.1**: Minor tweaks to output format
- **v2.0**: Major changes to metrics/sections

---

## Error Handling & Retry Logic

### Error Response (OpenAI)

```json
{
  "error": {
    "message": "Rate limit exceeded",
    "type": "rate_limit_error",
    "code": "rate_limit_exceeded"
  }
}
```

### Error Codes & Handling

| Code | Message | Retry? | Strategy |
|------|---------|--------|----------|
| 401 | Invalid API key | No | Alert admin |
| 429 | Rate limit exceeded | Yes | Exponential backoff (max 5 retries) |
| 500 | Server error | Yes | Exponential backoff (max 3 retries) |
| 503 | Service unavailable | Yes | Exponential backoff (max 3 retries) |
| timeout | Request timeout | Yes | Retry after 5 seconds (max 3 retries) |

### Failover Logic

```
1. Try OpenAI (GPT-4 Turbo)
   ↓ (if error)
2. Try Claude 3 Sonnet
   ↓ (if error)
3. Return cached analysis or unavailable message
```

---

## Rate Limiting

- **OpenAI**: 3500 RPM (requests per minute)
- **Claude**: 50000 tokens per minute

**Client Strategy**:
- Queue analysis requests (max 2 concurrent)
- Cache identical analyses for 24 hours
- Batch analyses for multiple users

---

## Cost Estimation

### Pricing

| Provider | Model | Input | Output |
|----------|-------|-------|--------|
| OpenAI | GPT-4 Turbo | $0.01/1K | $0.03/1K |
| Anthropic | Claude 3 Sonnet | $0.003/1K | $0.015/1K |

### Cost Per Analysis

- **Average analysis**: 250 input + 450 output = 700 tokens
- **OpenAI cost**: (0.250 * $0.01) + (0.450 * $0.03) = **$0.016**
- **Claude cost**: (0.250 * $0.003) + (0.450 * $0.015) = **$0.0075**

### Annual Cost (1000 users, 2 analyses/week)

- **OpenAI**: 1000 * 2 * 52 * $0.016 = **$1,664**
- **Claude**: 1000 * 2 * 52 * $0.0075 = **$780**
- **With caching (80% hit rate)**: ~$200-330/year

---

## Implementation Checklist

- [ ] Securely store API keys (encrypted at rest)
- [ ] Implement failover from OpenAI → Claude
- [ ] Version all prompt templates
- [ ] Track token usage for cost reporting
- [ ] Implement exponential backoff for rate limits
- [ ] Cache identical analyses for 24 hours
- [ ] Validate LLM response structure before mapping
- [ ] Set request timeout to 60 seconds
- [ ] Log all API requests/responses for debugging
- [ ] Implement circuit breaker pattern (if provider down, use cache)
- [ ] Monitor API response time (target: < 30 seconds)
- [ ] Add user-friendly error messages if analysis fails

---

## Testing & QA

### Unit Tests

- Mock OpenAI/Claude API responses
- Test prompt template rendering
- Test error handling and failover logic
- Test cost tracking and caching

### Integration Tests

- Test with live OpenAI API
- Test failover to Claude
- Test caching with repeated analyses

### Manual QA

- [ ] Generate analysis for sample crypto asset (BTC)
- [ ] Generate analysis for sample stock (AAPL)
- [ ] Generate portfolio-level analysis
- [ ] Verify output matches schema
- [ ] Check caching behavior
- [ ] Simulate API failure → verify failover

