#!/usr/bin/env python3
"""
inject-knowledge.py — Inject knowledge rules into QWEN.md

Usage:
  python3 inject-knowledge.py                        # inject all enabled rules
  python3 inject-knowledge.py angular backend        # inject matching categories only
  python3 inject-knowledge.py --list                 # print rules without writing

Reads:  .specify/knowledge/index.yaml
Writes: QWEN.md  (replaces <!-- KNOWLEDGE RULES START/END --> block, or appends)
"""

import sys
import os
import re
from datetime import date
from pathlib import Path

# Force UTF-8 output on Windows
if sys.stdout.encoding.lower() != "utf-8":
    sys.stdout.reconfigure(encoding="utf-8")


# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------

def find_repo_root(start: Path) -> Path:
    cur = start.resolve()
    while cur != cur.parent:
        if (cur / ".specify").is_dir():
            return cur
        cur = cur.parent
    raise RuntimeError("Could not find repo root (.specify directory not found)")


SCRIPT_DIR = Path(__file__).parent
REPO_ROOT = find_repo_root(SCRIPT_DIR)
INDEX_YAML = REPO_ROOT / ".specify" / "knowledge" / "index.yaml"
QWEN_MD = REPO_ROOT / "QWEN.md"

SECTION_START = "<!-- KNOWLEDGE RULES START -->"
SECTION_END = "<!-- KNOWLEDGE RULES END -->"
MANUAL_START = "<!-- MANUAL ADDITIONS START -->"


# ---------------------------------------------------------------------------
# YAML parser (no external dependencies)
# ---------------------------------------------------------------------------

def parse_index_yaml(path: Path) -> list[dict]:
    """Parse the flat rules list from index.yaml."""
    rules: list[dict] = []
    current: dict = {}
    in_rules = False
    in_categories = False

    with open(path, encoding="utf-8") as f:
        for raw_line in f:
            line = raw_line.rstrip()

            # Skip comments
            if line.lstrip().startswith("#"):
                continue

            if line == "rules:":
                in_rules = True
                continue

            if not in_rules:
                continue

            # New rule entry
            if re.match(r"^  - id:", line):
                if current:
                    rules.append(current)
                current = {"id": _extract_value(line), "category": []}
                in_categories = False
                continue

            # Category field
            if re.match(r"^    category:", line):
                val = line.split(":", 1)[1].strip()
                if val.startswith("[") and val.endswith("]"):
                    inner = val[1:-1]
                    current["category"] = [
                        v.strip() for v in inner.split(",") if v.strip()
                    ]
                    in_categories = False
                else:
                    current["category"] = []
                    in_categories = True
                continue

            # Category list items
            if in_categories and re.match(r"^      - ", line):
                current["category"].append(line.strip().lstrip("- "))
                continue

            # Other fields — stop category collection
            in_categories = False

            if re.match(r"^    text:", line):
                current["text"] = _extract_value(line)
            elif re.match(r"^    source_file:", line):
                current["source_file"] = _extract_value(line)
            elif re.match(r"^    enabled:", line):
                current["enabled"] = _extract_value(line).lower() == "true"
            elif re.match(r"^    fire_count:", line):
                val = _extract_value(line)
                current["fire_count"] = int(val) if val.isdigit() else 0
            elif re.match(r"^    last_fired:", line):
                current["last_fired"] = _extract_value(line)

    if current:
        rules.append(current)

    return rules


def _extract_value(line: str) -> str:
    """Extract value after the colon, stripping quotes."""
    parts = line.split(":", 1)
    if len(parts) < 2:
        return ""
    return parts[1].strip().strip('"').strip("'")


# ---------------------------------------------------------------------------
# Section builder
# ---------------------------------------------------------------------------

CATEGORY_ORDER = ["angular", "backend", "testing", "versioning", "architecture", "security"]
CATEGORY_LABELS = {
    "angular": "Angular",
    "backend": "Backend (.NET)",
    "testing": "Testing",
    "versioning": "Versioning",
    "architecture": "Architecture",
    "security": "Security",
}


def build_section(rules: list[dict], filter_categories: list[str]) -> str:
    today = date.today().isoformat()
    active = [r for r in rules if r.get("enabled", True)]

    if filter_categories:
        active = [
            r for r in active
            if any(c in r.get("category", []) for c in filter_categories)
        ]

    if not active:
        return ""

    # Group by primary category (first listed)
    grouped: dict[str, list[dict]] = {}
    for rule in active:
        cats = rule.get("category", [])
        primary = cats[0] if cats else "other"
        grouped.setdefault(primary, []).append(rule)

    cat_filter_label = (
        ", ".join(filter_categories) if filter_categories else "all"
    )

    lines = [
        SECTION_START,
        f"## Knowledge Rules",
        f"<!-- Auto-generated by inject-knowledge — {today} | {len(active)} rules | categories: {cat_filter_label} -->",
        f"<!-- Edit rules in .specify/knowledge/ — do not edit this block directly -->",
        "",
    ]

    rendered_cats = set()
    for cat in CATEGORY_ORDER:
        if cat not in grouped:
            continue
        rendered_cats.add(cat)
        label = CATEGORY_LABELS.get(cat, cat.title())
        lines.append(f"### {label}")
        for rule in grouped[cat]:
            lines.append(f"- **{rule['id']}**: {rule.get('text', '')}")
        lines.append("")

    # Any categories not in CATEGORY_ORDER
    for cat, cat_rules in grouped.items():
        if cat in rendered_cats:
            continue
        label = cat.title()
        lines.append(f"### {label}")
        for rule in cat_rules:
            lines.append(f"- **{rule['id']}**: {rule.get('text', '')}")
        lines.append("")

    lines.append(SECTION_END)
    return "\n".join(lines)


# ---------------------------------------------------------------------------
# QWEN.md writer
# ---------------------------------------------------------------------------

def inject_into_qwen(section: str) -> None:
    if not QWEN_MD.exists():
        # Create minimal QWEN.md
        content = f"# Finance Sentry — Qwen Context\n\n{section}\n"
        QWEN_MD.write_text(content, encoding="utf-8")
        print(f"Created {QWEN_MD} with knowledge section.")
        return

    original = QWEN_MD.read_text(encoding="utf-8")

    # Replace existing section
    if SECTION_START in original and SECTION_END in original:
        pattern = re.compile(
            re.escape(SECTION_START) + r".*?" + re.escape(SECTION_END),
            re.DOTALL,
        )
        updated = pattern.sub(section, original)
        QWEN_MD.write_text(updated, encoding="utf-8")
        print(f"Updated knowledge section in {QWEN_MD}.")
        return

    # Insert before manual additions block if present
    if MANUAL_START in original:
        updated = original.replace(
            MANUAL_START,
            section + "\n\n" + MANUAL_START,
        )
        QWEN_MD.write_text(updated, encoding="utf-8")
        print(f"Inserted knowledge section before manual additions in {QWEN_MD}.")
        return

    # Append to end
    separator = "\n\n" if not original.endswith("\n\n") else ""
    QWEN_MD.write_text(original + separator + section + "\n", encoding="utf-8")
    print(f"Appended knowledge section to {QWEN_MD}.")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> None:
    args = sys.argv[1:]

    list_only = "--list" in args
    filter_categories = [a for a in args if not a.startswith("--")]

    if not INDEX_YAML.exists():
        print(f"ERROR: index.yaml not found at {INDEX_YAML}", file=sys.stderr)
        sys.exit(1)

    rules = parse_index_yaml(INDEX_YAML)
    print(f"Loaded {len(rules)} rules from index.yaml.")

    section = build_section(rules, filter_categories)

    if not section:
        print("No enabled rules matched — nothing to inject.")
        return

    if list_only:
        print("\n" + section + "\n")
        return

    inject_into_qwen(section)


if __name__ == "__main__":
    main()
