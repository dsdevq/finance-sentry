#!/usr/bin/env python3
"""
Script to defer AI integration to a future phase across documentation files.
Updates 5 key files to remove/defer AI features from current implementation.
"""

import re
import os
from pathlib import Path

def update_data_model():
    """Update data-model.md to remove AIAnalysisReport entity"""
    path = Path("specs/002-investment-tracking/data-model.md")
    with open(path, 'r') as f:
        content = f.read()
    
    # Remove AIAnalysisReport from ERD
    content = re.sub(
        r'InvestmentAccount \(1\) ──── \(N\) InvestmentAccount ──── \(N\) AssetHolding\n\s+──── \(N\) SyncJob\n\s+──── \(N\) PortfolioMetric\n\s+──── \(N\) AIAnalysisReport\n\nAssetHolding \(1\) ──── \(N\) PriceHistory\n\s+──── \(N\) AIAnalysisReport',
        'InvestmentAccount (1) ──── (N) InvestmentAccount ──── (N) AssetHolding\n               ──── (N) SyncJob\n               ──── (N) PortfolioMetric\n\nAssetHolding (1) ──── (N) PriceHistory',
        content
    )
    
    # Remove entire AIAnalysisReport section (lines 240-296)
    content = re.sub(
        r'### 6\. AIAnalysisReport\n\nAI-generated analysis for assets or portfolio-level\.\n\n```sql\nCREATE TABLE ai_analysis_reports \([\s\S]*?\);[\s\S]*?```\n\n\*\*Validation Rules\*\* \(from FR-009, FR-010\):[\s\S]*?\n- `expires_at` set to created_at \+ 24 hours; stale analysis flagged in UI\n- User-scoped queries only \(no cross-user analysis visible\)\n\n---\n\n',
        '',
        content
    )
    
    # Remove RLS policy for ai_analysis_reports
    content = re.sub(
        r'CREATE POLICY ai_analysis_user_isolation ON ai_analysis_reports\n    USING \(user_id = current_user_id\)\n    WITH CHECK \(user_id = current_user_id\);\n\n',
        '',
        content
    )
    
    # Update baseline migration to remove ai_analysis_reports
    content = re.sub(
        r'CREATE TABLE portfolio_metrics \(\.\.\.\);\nCREATE TABLE ai_analysis_reports \(\.\.\.\);',
        'CREATE TABLE portfolio_metrics (...);',
        content
    )
    
    # Add note about future AI implementation
    insert_pos = content.find("## Migration & Versioning")
    note = """---

## Future Phases: AI Integration

**Phase 2 (Deferred)**: AI-powered analysis using LLMs (OpenAI GPT-4, Claude 3) will be implemented in a future phase. This includes:
- `AIAnalysisReport` entity and `ai_analysis_reports` table
- Asset-level analysis endpoint
- Portfolio-level analysis endpoint
- Risk assessment and forecasting capabilities

The schema, prompts, and integration details are documented in `ai-integration-future.md` for reference.

"""
    
    content = content[:insert_pos] + note + content[insert_pos:]
    
    with open(path, 'w') as f:
        f.write(content)
    
    return "data-model.md"

def update_research():
    """Update research.md to mark LLM decision as deferred"""
    path = Path("specs/002-investment-tracking/research.md")
    with open(path, 'r') as f:
        content = f.read()
    
    # Add DEFERRED marker to title
    content = content.replace(
        "## Decision 1: LLM Provider for AI-Powered Analysis",
        "## Decision 1: LLM Provider for AI-Powered Analysis [DEFERRED to Phase 2]"
    )
    
    # Add note after the decision section
    insert_pos = content.find("### Rationale")
    note = "**STATUS**: This decision is DEFERRED to Phase 2. The research is documented for future reference when AI integration is planned.\n\n"
    content = content[:insert_pos] + note + content[insert_pos:]
    
    # Add note at the end of the LLM section
    end_marker = "---\n\n## Decision 2: Market Data Feed Source"
    note_end = "\n\n> **Note**: LLM integration and research is deferred to Phase 2. Revisit this decision when planning AI features.\n\n"
    content = content.replace(
        "---\n\n## Decision 2: Market Data Feed Source",
        note_end + "---\n\n## Decision 2: Market Data Feed Source"
    )
    
    with open(path, 'w') as f:
        f.write(content)
    
    return "research.md"

def update_llm_integration():
    """Rename/mark llm-integration.md as future"""
    old_path = Path("specs/002-investment-tracking/llm-integration.md")
    new_path = Path("specs/002-investment-tracking/ai-integration-future.md")
    
    # Read the original file
    with open(old_path, 'r') as f:
        content = f.read()
    
    # Add deferred marker at the top
    header = """# [DEFERRED TO PHASE 2] LLM / AI Integration Contract

> **Status**: This document describes AI integration features that are DEFERRED to Phase 2. 
> This is provided as reference for future implementation planning.
> **Current Phase (Phase 1)** focuses on holdings management and aggregation only.

---

"""
    
    # Insert header after the title
    title_end = content.find("---")
    content = content[:title_end+3] + "\n" + header + content[title_end+3:]
    
    # Write to new file
    with open(new_path, 'w') as f:
        f.write(content)
    
    # Delete old file
    old_path.unlink()
    
    return "llm-integration.md → ai-integration-future.md"

def update_portfolio_endpoints():
    """Update portfolio-endpoints.md to remove AI endpoints"""
    path = Path("specs/002-investment-tracking/portfolio-endpoints.md")
    with open(path, 'r') as f:
        content = f.read()
    
    # Remove endpoints 10-12 (AI analysis endpoints)
    # Endpoint 10: Request AI Analysis (Asset-Level)
    content = re.sub(
        r'### 10\. Request AI Analysis \(Asset-Level\)[\s\S]*?---\n\n### 11\. Get AI Analysis Report[\s\S]*?---\n\n### 12\. Request Portfolio Analysis[\s\S]*?---\n\n### 13\. Disconnect Account',
        '### 10. Disconnect Account',
        content
    )
    
    # Renumber Disconnect Account back to 10 (was 13)
    content = content.replace(
        "### 10. Disconnect Account\n\n**Endpoint**: `DELETE /accounts/{accountId}`",
        "### 10. Disconnect Account\n\n**Endpoint**: `DELETE /accounts/{accountId}`"
    )
    
    # Update endpoint count in documentation
    content = content.replace(
        "This document defines the REST API contract for portfolio management. The backend exposes endpoints for connecting investment accounts, fetching holdings, calculating metrics, and requesting AI analysis.",
        "This document defines the REST API contract for portfolio management. The backend exposes endpoints for connecting investment accounts, fetching holdings, calculating metrics, and managing portfolios."
    )
    
    # Add note about deferred AI endpoints
    insert_pos = content.find("## Error Codes Reference")
    note = """---

## Deferred: AI Analysis Endpoints

The following endpoints are deferred to Phase 2 (AI Integration phase):
- `POST /holdings/{holdingId}/analysis` - Request asset-level AI analysis
- `GET /analysis/{analysisId}` - Retrieve AI analysis report
- `POST /analysis/portfolio` - Request portfolio-level AI analysis

These endpoints will be added when AI features are implemented.

"""
    
    content = content[:insert_pos] + note + "\n" + content[insert_pos:]
    
    with open(path, 'w') as f:
        f.write(content)
    
    return "portfolio-endpoints.md"

def update_quickstart():
    """Update quickstart.md to remove LLM mock service section"""
    path = Path("specs/002-investment-tracking/quickstart.md")
    with open(path, 'r') as f:
        content = f.read()
    
    # Remove the entire "Setting Up LLM Mock" section
    content = re.sub(
        r'---\n\n## Setting Up LLM Mock\n\nFor development without using real OpenAI/Claude API calls:[\s\S]*?---\n\n## Testing Multi-Source Aggregation',
        '---\n\n## Testing Multi-Source Aggregation',
        content
    )
    
    # Update Table of Contents to remove LLM Mock section
    content = re.sub(
        r'5\. \[Setting Up LLM Mock\]\(#setting-up-llm-mock\)\n',
        '',
        content
    )
    
    # Renumber remaining sections in TOC
    content = re.sub(
        r'6\. \[Testing Multi-Source Aggregation\]\(#testing-multi-source-aggregation\)',
        '5. [Testing Multi-Source Aggregation](#testing-multi-source-aggregation)',
        content
    )
    
    content = re.sub(
        r'7\. \[Running Tests\]\(#running-tests\)',
        '6. [Running Tests](#running-tests)',
        content
    )
    
    content = re.sub(
        r'8\. \[Troubleshooting\]\(#troubleshooting\)',
        '7. [Troubleshooting](#troubleshooting)',
        content
    )
    
    # Remove testing LLM responses from troubleshooting
    content = re.sub(
        r'#### 4\. \*\*OpenAI Rate Limit Exceeded\*\*[\s\S]*?\n\n#### 5\. \*\*Redis Connection Error\*\*',
        '#### 4. **Redis Connection Error**',
        content
    )
    
    # Update "Next Steps" section to remove LLM reference
    content = re.sub(
        r"3\. \*\*Explore API Contracts\*\*: Read binance-api\.md, interactive-brokers-api\.md, llm-integration\.md",
        "3. **Explore API Contracts**: Read binance-api.md, interactive-brokers-api.md",
        content
    )
    
    content = re.sub(
        r"3\. \*\*Study Research\*\*: Read research\.md for architectural decisions",
        "3. **Study Research**: Read research.md for architectural decisions (excluding deferred AI decisions)",
        content
    )
    
    with open(path, 'w') as f:
        f.write(content)
    
    return "quickstart.md"

def main():
    """Run all updates"""
    print("🔄 Deferring AI Integration to Phase 2...\n")
    
    updates = [
        ("data-model.md", update_data_model),
        ("research.md", update_research),
        ("llm-integration.md", update_llm_integration),
        ("portfolio-endpoints.md", update_portfolio_endpoints),
        ("quickstart.md", update_quickstart),
    ]
    
    results = []
    for name, update_func in updates:
        try:
            result = update_func()
            results.append(f"✅ {result}")
        except Exception as e:
            results.append(f"❌ {name}: {str(e)}")
    
    print("\n📋 Summary of Changes:\n")
    for result in results:
        print(result)
    
    print("\n✨ AI integration successfully deferred to Phase 2!")
    print("\n📝 Documentation updated:")
    print("   - AIAnalysisReport removed from data model (deferred reference added)")
    print("   - LLM provider decision marked as DEFERRED in research.md")
    print("   - llm-integration.md renamed to ai-integration-future.md")
    print("   - AI endpoints removed from portfolio-endpoints.md")
    print("   - LLM mock service removed from quickstart.md")
    print("\n🎯 Current Phase 1 now focuses on:")
    print("   • Holdings management and aggregation")
    print("   • Account synchronization")
    print("   • Portfolio metrics calculation")
    print("   • Multi-source data aggregation")

if __name__ == "__main__":
    main()
