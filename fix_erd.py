import re

# Read the file
with open("specs/002-investment-tracking/data-model.md", "r", encoding="utf-8") as f:
    content = f.read()

# Fix ERD (remove AIAnalysisReport references from diagram)
# Line 17 and 20 need to be removed
lines = content.split('\n')
new_lines = []
skip_next = False
for i, line in enumerate(lines):
    if i == 16:  # Line 17 in 1-indexed (──── (N) AIAnalysisReport)
        continue  # Skip this line
    if i == 19:  # Line 20 in 1-indexed (──── (N) AIAnalysisReport)
        continue  # Skip this line
    new_lines.append(line)

content = '\n'.join(new_lines)

# Write back
with open("specs/002-investment-tracking/data-model.md", "w", encoding="utf-8") as f:
    f.write(content)

print("✅ Updated ERD in data-model.md")
