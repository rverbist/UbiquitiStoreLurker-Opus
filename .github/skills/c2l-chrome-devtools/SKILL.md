---
name: c2l-chrome-devtools
description: 'Use Chrome DevTools MCP to load, inspect, and interact with the Coach2Lead web app running under IIS Express. Use when verifying local site startup, capturing screenshots, debugging JS errors, or automating browser flows against https://localhost:44300/.'
---

# Coach2Lead Chrome DevTools

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Objective](#objective)
- [Read First](#read-first)
- [Core Anchors](#core-anchors)
- [Standard Workflow](#standard-workflow)
- [Guardrails](#guardrails)
- [Validation](#validation)
- [Golden Example](#golden-example)
- [Output Contract](#output-contract)
- [Companion Skills](#companion-skills)
- [Skill-Specific Topics](#skill-specific-topics)
  - [Troubleshooting: "ServiceProvider is not configured"](#troubleshooting-serviceprovider-is-not-configured)
  - [Troubleshooting: `new_page` timeout](#troubleshooting-new_page-timeout)
  - [Element interaction pattern](#element-interaction-pattern)
  - [Keeping this skill current](#keeping-this-skill-current)

</details>
<!-- toc:end -->

## Objective
- Verify that the Coach2Lead web app is reachable and functioning after an IIS Express launch.
- Support browser-based debugging, screenshot capture, JS console inspection, and form automation against `https://localhost:44300/`.

## Read First
- `.github/skills/c2l-build-run-debug/SKILL.md` - build and IIS Express launch must be done before using this skill.
- `.github/skills/c2l-chrome-devtools/SKILL.md` - upstream generic Chrome DevTools skill with full tool catalogue.

## Core Anchors
- IIS Express local URL: `https://localhost:44300/`
- Chrome DevTools MCP tool prefix: `mcp_io_github_chr_`
- MCP tool discovery: `tool_search_tool_regex` with pattern `mcp_io_github_chr`

## Standard Workflow
1. Ensure IIS Express is running (`c2l-build-run-debug` skill, `iisexpress:start` task).
2. Load Chrome DevTools MCP tools via `tool_search_tool_regex` (pattern: `mcp_io_github_chr`) - they are **deferred** and must be loaded before use.
3. Open a new page with `mcp_io_github_chr_new_page(url="https://localhost:44300/", timeout=30000)`.
   - Use **30 000 ms** minimum - the first cold-start request through the OWIN pipeline takes longer than the default 15 000 ms.
4. Call `mcp_io_github_chr_list_pages` to confirm the page was created and note its ID.
5. Call `mcp_io_github_chr_select_page(page_id=<id>)` if the new page is not already selected.
6. Call `mcp_io_github_chr_take_snapshot` or `mcp_io_github_chr_take_screenshot` to inspect the loaded state.
7. If the page shows a 500 / error page, proceed to the **Troubleshooting** section below.

## Guardrails
- **Always** search for `mcp_io_github_chr_*` tools with `tool_search_tool_regex` before calling them - they are deferred and will fail silently without loading.
- **Never** use the default 15 000 ms timeout for `new_page` against localhost:44300 on a cold start - it will time out; use 30 000 ms or higher.
- Prefer `take_snapshot` over `take_screenshot` when you need element `uid` values for clicks or form fills; both can be used for visual verification.
- Do not close IIS Express while Chrome pages remain open - navigation will hang.
- A 500 "ServiceProvider is not configured" error on first load indicates an OWIN / DI container startup failure in the local environment, **not** an IIS Express problem. IIS Express is working correctly in this case.

## Validation
- `list_pages` shows the expected `https://localhost:44300/` URL.
- Screenshot or snapshot confirms the page rendered (even if an error page - that still proves the pipeline is live).
- Console messages and network requests can be inspected to triage further.

## Golden Example
Verified Coach2Lead launch session (2026-02-24):
```code
1. msbuild:build  ->  Build succeeded. 0 Errors.
2. iisexpress:ensure-config  ->  "Updated IIS Express site 'Coach2Lead.Web'."
3. iisexpress:start  ->  iisexpress.exe running (PID 60508).
4. Invoke-WebRequest https://localhost:44300/  ->  HTTP 500 (ASP.NET pipeline active, ServiceProvider DI error).
5. tool_search_tool_regex("mcp_io_github_chr")  ->  Chrome DevTools tools loaded.
6. new_page(url="https://localhost:44300/", timeout=30000)  ->  SUCCESS (timed out at 15000ms, succeeded at 30000ms).
7. list_pages  ->  page 3 is https://localhost:44300/ [selected].
8. take_screenshot  ->  ASP.NET yellow-screen 500 rendered - IIS Express confirmed working.
```
Key lesson: **first attempt at 15 000 ms timed out; 30 000 ms succeeded.** IIS Express cold-start across the OWIN pipeline requires extra time on first request.

## Output Contract
- IIS Express state (running / not running).
- Chrome page ID and URL loaded.
- Screenshot or snapshot of the loaded state.
- Console errors or network failures if page content is unexpected.

## Companion Skills
- `c2l-build-run-debug`
- `chrome-devtools`

## Skill-Specific Topics

### Troubleshooting: "ServiceProvider is not configured"

This 500 error originates from `Coach2Lead.Web/App/Services/ServiceLocator.cs` (line 22) during `Startup.Configuration`. It means the DI container (`_rootServiceProvider`) was never initialized - typically because a required environment config (connection string, app setting) is missing or the app startup threw before registering services.
**This does not mean IIS Express failed.** The ASP.NET/OWIN pipeline reached the request handler - that is a green signal for the web server itself. Fix the application configuration to resolve the DI error.

### Troubleshooting: `new_page` timeout

If `new_page` times out:
1. Increase timeout to 45 000 ms.
2. Verify IIS Express is still running (`Get-Process iisexpress`).
3. Check SSL: the self-signed cert must be trusted. Run `iisexpress:setup-ssl` task if not done.
4. Try `Invoke-WebRequest -Uri "https://localhost:44300/" -SkipCertificateCheck` from PowerShell first to confirm the site responds at all.

### Element interaction pattern

When automating forms or clicking UI elements:
1. `take_snapshot` to get the accessibility tree and element `uid` values.
2. `click(uid=<value>)` or `fill(uid=<value>, value=<text>)`.
3. Re-`take_snapshot` after each major navigation - `uid` values are not stable across DOM mutations.

### Keeping this skill current

> **Note for maintainers**: This skill should be updated after each new Chrome DevTools MCP usage session against Coach2Lead. Document edge cases, new timeouts, newly discovered tool behaviors, and any Coach2Lead-specific quirks encountered. The upstream `chrome-devtools` skill covers generic patterns; this skill covers **C2L-specific learnings only**.
Last verified: 2026-02-24 - IIS Express cold-start, `new_page`, `take_screenshot`.
