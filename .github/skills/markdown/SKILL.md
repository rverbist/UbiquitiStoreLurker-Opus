---
name: markdown
description: 'Manage markdown files with PowerShell scripts for TOC generation, character normalization, table formatting, whitespace cleanup, code fence normalization, and mojibake repair. Use when asked to "fix markdown", "normalize tables", "add table of contents", "clean up whitespace", "fix encoding", "repair mojibake", "normalize code fences", "run cleanup pipeline", or batch-process multiple .md files. Supports -WhatIf, -Confirm, -Verbose, -Path, and -Recurse parameters.'
---

# Markdown Scripts

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Index](#index)
- [Common Features](#common-features)
  - [Path + Recurse Pattern](#path--recurse-pattern)

</details>
<!-- toc:end -->
<!-- index:start -->

## Index

| Keyword                                           | Script                      | Use Case                                |
| ------------------------------------------------- | --------------------------- | --------------------------------------- |
| cleanup, pipeline, all, batch, full               | `Invoke-MarkdownCleanup`    | Run all normalization scripts at once   |
| TOC, table of contents, navigation                | `Update-TableOfContents`    | Generate/refresh nested TOCs            |
| table, separator, pipe, formatting                | `Normalize-TableSeparators` | Format table divider lines              |
| unicode, curly quotes, encoding, ASCII            | `Normalize-Characters`      | Replace special chars with ASCII        |
| whitespace, trailing spaces, blank lines, cleanup | `Normalize-Whitespace`      | Remove excess whitespace and trim lines |
| code fence, backticks, language                   | `Normalize-CodeFences`      | Validate/fix code block fences          |
| mojibake, corrupted, double-encoded               | `Fix-Mojibake`              | Repair encoding corruption              |
<!-- index:end -->

## Common Features

All scripts share these PowerShell conventions:
| Feature                 | Description                                     |
| ----------------------- | ----------------------------------------------- |
| `-WhatIf`               | Preview changes without modifying files         |
| `-Confirm`              | Prompt before each change                       |
| `-Verbose`              | Show detailed processing output                 |
| `-Path`                 | Target file (.md) or directory                  |
| `-Recurse`              | Process subdirectories (when Path is directory) |
| `SupportsShouldProcess` | Built-in safety for destructive operations      |

### Path + Recurse Pattern

```powershell
# Single file
Script-Name -Path .\docs\README.md

# Directory (top-level only)
Script-Name -Path .\docs

# Directory (recursive)
Script-Name -Path .\docs -Recurse

# Preview mode
Script-Name -Path .\docs -Recurse -WhatIf

# Detailed output

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Script Location](#script-location)
- [Scripts Reference](#scripts-reference)
  - [Invoke-MarkdownCleanup](#invoke-markdowncleanup)

</details>
<!-- toc:end -->
Script-Name -Path .\docs -Recurse -Verbose
```

### Script Location

All scripts live in:
```code
.github/skills/markdown/scripts/
├── Fix-Mojibake.ps1
├── Invoke-MarkdownCleanup.ps1
├── Normalize-Characters.ps1
├── Normalize-CodeFences.ps1
├── Normalize-TableSeparators.ps1
├── Normalize-Whitespace.ps1
└── Update-TableOfContents.ps1
```
---

## Scripts Reference

### Invoke-MarkdownCleanup

**Purpose:** Run all markdown normalization scripts in a single pipeline.
**When to use:**
- Full cleanup of markdown files before commit
- Batch-process entire documentation folders
- Apply all normalization steps in correct order
**Pipeline order:**
1. `Fix-Mojibake` - Repair encoding corruption first
2. `Normalize-Characters` - Replace Unicode with ASCII
3. `Normalize-Whitespace` - Clean up whitespace
4. `Normalize-TableSeparators` - Format table dividers
5. `Normalize-CodeFences` - Fix code block fences
6. `Update-TableOfContents` - Refresh TOCs last
**Examples:**
```powershell
# Full cleanup of docs folder
Invoke-MarkdownCleanup -Path .\docs -Recurse

# Preview all changes without modifying
Invoke-MarkdownCleanup -Path .\docs -Recurse -WhatIf

# Verbose output showing each step

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Update-TableOfContents](#update-tableofcontents)

</details>
<!-- toc:end -->
Invoke-MarkdownCleanup -Path .\docs -Recurse -Verbose
```
---

### Update-TableOfContents

**Purpose:** Generate/refresh nested Tables of Contents after markdown headings.
**When to use:**
- Add navigation TOC to documentation files
- Refresh TOCs after adding/renaming headings
- Batch-process all docs in a directory
**Key parameters:**
| Parameter        | Default | Description                             |
| ---------------- | ------- | --------------------------------------- |
| `-MinLevel`      | 1       | Heading level to insert TOC after (1-6) |
| `-MaxLevel`      | 6       | Deepest heading level to include        |
| `-Plain`         | false   | Plain list without collapsible wrapper  |
| `-OutputPath`    | -       | Write to different file                 |
| `-OutputConsole` | -       | Output to stdout instead of file        |
**Examples:**
```powershell
# Add TOCs to all markdown in docs/
Update-TableOfContents -Path .\docs -Recurse

# TOC after H2 headings only, up to H4
Update-TableOfContents -Path .\README.md -MinLevel 2 -MaxLevel 4

# Preview changes

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Normalize-TableSeparators](#normalize-tableseparators)

</details>
<!-- toc:end -->
Update-TableOfContents -Path .\docs -Recurse -WhatIf
```
**TOC markers:** `<!-- toc:start -->` and `<!-- toc:end -->` - existing TOCs are replaced.
---

### Normalize-TableSeparators

**Purpose:** Format markdown table separator lines with consistent spacing.
**When to use:**
- Tables have inconsistent separator formatting
- Want uniform `| ---- |` style instead of `|------|`
- Preparing docs for linting/review
**Transformation:**
```markdown
Before: |------|------|
After: | ---- | ---- |

Before: |:----:|-----:|
After: |: -- :| --- :|
```
**Examples:**
```powershell
# Format tables in one file
Normalize-TableSeparators -Path .\docs\api.md

# All markdown files recursively
Normalize-TableSeparators -Path .\docs -Recurse

# Verbose output showing each change

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Normalize-Characters](#normalize-characters)

</details>
<!-- toc:end -->
Normalize-TableSeparators -Path .\docs -Recurse -Verbose
```
---

### Normalize-Characters

**Purpose:** Replace Unicode special characters with ASCII equivalents.
**When to use:**
- Files contain curly quotes (`"` `"` `'` `'`)
- Em/en dashes need to be hyphens
- Arrow symbols should be ASCII (`->` `=>`)
- Preparing for systems that don't handle Unicode well
- Detecting problematic/corrupted characters
**Categories controlled:**
| Category | Characters         | ASCII Replacement |
| -------- | ------------------ | ----------------- |
| Quotes   | `"` `"` `'` `'`    | `"` `'`           |
| Dashes   | `-` `-`            | `-`               |
| Spaces   | non-breaking space | regular space     |
| Arrows   | `->` `<-` `=>`        | `->` `<-` `=>`    |
| Math     | `<=` `>=` `!=`        | `<=` `>=` `!=`    |
| Lists    | `-` `>`            | `-` `>`           |
| Ellipsis | `...`                | `...`             |
**Key parameters:**
| Parameter             | Description                                    |
| --------------------- | ---------------------------------------------- |
| `-PreserveQuotes`     | Don't replace curly quotes                     |
| `-PreserveDashes`     | Don't replace em/en dashes                     |
| `-PreserveArrows`     | Don't replace arrow symbols                    |
| `-Inverse`            | Flip to "preserve all, replace only specified" |
| `-ReplaceQuotes`      | (with -Inverse) Only replace quotes            |
| `-Backup`             | Create .bak before modifying                   |
| `-Warn`               | Report problematic chars as warnings           |
| `-ErrorOnProblematic` | Report problematic chars as errors             |
**Examples:**
```powershell
# Replace all special chars
Normalize-Characters -Path .\docs -Recurse

# Replace all except arrows
Normalize-Characters -Path .\docs -Recurse -PreserveArrows

# Only replace quotes and spaces (inverse mode)
Normalize-Characters -Path .\docs -Inverse -ReplaceQuotes -ReplaceSpaces

# Scan for problematic characters without replacing
Normalize-Characters -Path .\docs -Recurse -Warn -Inverse

# Create backups before changes

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Normalize-CodeFences](#normalize-codefences)

</details>
<!-- toc:end -->
Normalize-Characters -Path .\docs -Recurse -Backup
```
---

### Normalize-CodeFences

**Purpose:** Validate and normalize markdown code fence formatting.
**When to use:**
- Code blocks missing language specifier
- Trailing whitespace on fence lines
- Verify all code fences are properly closed
- Standardize code block formatting
**Behavior:**
- Validates balanced opening/closing fences
- Adds default language `code` when omitted
- Trims trailing whitespace from fence lines
- Warns about unclosed code fences
**Examples:**
```powershell
# Normalize code fences in one file
Normalize-CodeFences -Path .\docs\api.md

# All markdown files recursively
Normalize-CodeFences -Path .\docs -Recurse

# Preview changes with verbose output

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Normalize-Whitespace](#normalize-whitespace)

</details>
<!-- toc:end -->
Normalize-CodeFences -Path .\docs -Recurse -WhatIf -Verbose
```
---

### Normalize-Whitespace

**Purpose:** Normalize all whitespace in markdown files.
**When to use:**
- Trim trailing whitespace from lines
- Files have inconsistent blank line usage
- Multiple consecutive blank lines need cleanup
- Want exactly one trailing blank line
- Preparing for consistent git diffs
**Behavior:**
- Trims trailing whitespace from every line
- Removes whitespace-only lines except around headings and anchor tags
- Collapses multiple consecutive blank lines into single blank lines
- Preserves code block content exactly
- Output: UTF-8 (no BOM), CRLF, ends with exactly one newline
- Empty files left untouched
**Examples:**
```powershell
# Normalize one file
Normalize-Whitespace -Path .\docs\README.md

# All markdown recursively
Normalize-Whitespace -Path .\docs -Recurse

# Preview changes

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Fix-Mojibake](#fix-mojibake)

</details>
<!-- toc:end -->

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Fix-Mojibake](#fix-mojibake)

</details>
<!-- toc:end -->

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Fix-Mojibake](#fix-mojibake)

</details>
<!-- toc:end -->

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Fix-Mojibake](#fix-mojibake)

</details>
<!-- toc:end -->
Normalize-Whitespace -Path .\docs -Recurse -WhatIf
```
---

### Fix-Mojibake

**Purpose:** Repair double-encoded UTF-8 (mojibake) sequences.
**When to use:**
- See garbled text like `â€"` instead of `-`
- Files were saved with wrong encoding
- Copy-paste introduced encoding corruption
**Common replacements:**
| Mojibake | Original            | Result |
| -------- | ------------------- | ------ |
| `â€'`    | non-breaking hyphen | `-`    |
| `â€"`    | em-dash             | `-`    |
| `â†'`    | arrow               | `->`   |
| `âœ...`    | checkmark           | `[x]`  |
**Examples:**
```powershell
# Fix one file
Fix-Mojibake -Path .\docs\corrupted.md

# Scan entire docs directory
Fix-Mojibake -Path .\docs -Recurse

# Preview what would be fixed

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Common Use Cases](#common-use-cases)
  - ["Clean up all markdown files"](#clean-up-all-markdown-files)

</details>
<!-- toc:end -->
Fix-Mojibake -Path .\docs -Recurse -WhatIf
```
---

## Common Use Cases

### "Clean up all markdown files"

```powershell
# Full cleanup pipeline (runs all scripts in correct order)
Invoke-MarkdownCleanup -Path .\docs -Recurse

# Or run scripts manually if you need specific control:

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- ["Preview all changes before applying"](#preview-all-changes-before-applying)

</details>
<!-- toc:end -->
$path = ".\docs"
Fix-Mojibake -Path $path -Recurse
Normalize-Characters -Path $path -Recurse
Normalize-Whitespace -Path $path -Recurse
Normalize-TableSeparators -Path $path -Recurse
Normalize-CodeFences -Path $path -Recurse
Update-TableOfContents -Path $path -Recurse
```

### "Preview all changes before applying"

```powershell
# Preview entire cleanup pipeline
Invoke-MarkdownCleanup -Path .\docs -Recurse -WhatIf

# Or preview individual scripts

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- ["Only fix quotes and dashes"](#only-fix-quotes-and-dashes)
- ["Find encoding problems without fixing"](#find-encoding-problems-without-fixing)
- ["Refresh TOCs for all docs"](#refresh-tocs-for-all-docs)
- ["Fix corrupted files from bad encoding"](#fix-corrupted-files-from-bad-encoding)
- [Troubleshooting](#troubleshooting)
- [Keywords](#keywords)

</details>
<!-- toc:end -->
Normalize-Whitespace -Path .\docs -Recurse -WhatIf
Normalize-Characters -Path .\docs -Recurse -WhatIf
```

### "Only fix quotes and dashes"

```powershell
Normalize-Characters -Path .\docs -Recurse -Inverse -ReplaceQuotes -ReplaceDashes
```

### "Find encoding problems without fixing"

```powershell
Normalize-Characters -Path .\docs -Recurse -Warn -Inverse
```

### "Refresh TOCs for all docs"

```powershell
Update-TableOfContents -Path .\docs -Recurse
```

### "Fix corrupted files from bad encoding"

```powershell
Fix-Mojibake -Path .\docs -Recurse
```
---

## Troubleshooting

| Problem                   | Solution                                                     |
| ------------------------- | ------------------------------------------------------------ |
| Script not found          | Run from repo root or use full path                          |
| No files processed        | Check `-Path` points to .md file or directory with .md files |
| `-Recurse` ignored        | Only works when `-Path` is a directory                       |
| Changes not saved         | Remove `-WhatIf` parameter                                   |
| TOC not appearing         | Ensure heading levels match `-MinLevel`                      |
| Wrong encoding after save | Scripts output UTF-8 without BOM                             |
---

## Keywords

markdown, md, documentation, TOC, table of contents, navigation, tables, formatting, unicode, ASCII, encoding, mojibake, corruption, blank lines, whitespace, cleanup, normalization, curly quotes, smart quotes, em dash, en dash, arrows, batch processing, recursive, WhatIf, PowerShell, code fence, code block, backticks, language specifier, pipeline, full cleanup, all scripts
