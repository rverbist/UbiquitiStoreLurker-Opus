# Ubiquiti EU Store — Parser Discovery Report

**Date:** 2026-03-24
**Task:** task-phase-3-discovery
**Author:** gem-orchestrator (automated Playwright capture + analysis)

---

## Scope

Real HTML fixtures were captured from the Ubiquiti EU store using Playwright 1.51.1
(headless Chromium, `networkidle` wait + 3 s extra settle). Two product pages were
captured representing the two required stock states:

| File | URL | Expected state |
|------|-----|---------------|
| `real-unas-pro-4-outofstock.html` | `https://eu.store.ui.com/eu/en/category/network-storage/products/unas-pro-4` | OutOfStock |
| `real-unas-pro-8-instock.html` | `https://eu.store.ui.com/eu/en/category/network-storage/products/unas-pro-8` | InStock |

HAR files in `har/` document all network requests made during capture. Raw discovery
data is in `discovery-raw.json`.

---

## Finding 1 — JSON-LD is server-side rendered (SSR)

The `<script id="product-jsonld" ...>` block **is present in the raw HTTP response**
before any JavaScript executes. This was confirmed by fetching the page with a plain
HTTPS GET (equivalent to what `HttpClient` / AngleSharp does in production) and
finding the script tag inline in the server-sent HTML.

### Exact element attributes

```html
<script id="product-jsonld" type="application/ld+json" data-next-head="">
  { ... }
</script>
```

Both attributes (`id="product-jsonld"` and `data-next-head=""`) are **extra but
harmless** for CSS attribute selector matching. The selector used in
`JsonLdStockParser`:

```csharp
document.QuerySelectorAll("script[type='application/ld+json']")
```

**will match this element** because the `type` attribute selector is satisfied
regardless of additional attributes like `id` or `data-next-head`.

### JSON-LD structure — UNAS Pro 4 (OutOfStock)

```json
{
  "@context": "https://schema.org",
  "@type": "Product",
  "brand": { "@type": "Brand", "name": "Ubiquiti" },
  "name": "UNAS Pro 4",
  "sku": "UNAS-Pro-4",
  "description": "...",
  "url": "https://eu.store.ui.com/eu/en/products/unas-pro-4",
  "image": { "@type": "ImageObject", "url": "...cdn...", "width": "3000", "height": "3000" },
  "offers": {
    "@type": "Offer",
    "availability": "https://schema.org/OutOfStock",
    "priceSpecification": {
      "@type": "PriceSpecification",
      "price": 449,
      "priceCurrency": "EUR",
      "valueAddedTaxIncluded": false
    }
  }
}
```

### JSON-LD structure — UNAS Pro 8 (InStock)

Identical shape, with:

```json
"offers": {
  "@type": "Offer",
  "availability": "https://schema.org/InStock",
  "priceSpecification": { "price": 719, "priceCurrency": "EUR", ... }
}
```

### Key structural observations

- `offers.availability` uses the **full schema.org URL format** — matches exactly
  what `JsonLdStockParser.InStockValues` and `OutOfStockValues` contain.
- `offers` is a **single object** (not an array) — the parser's
  `offers.ValueKind == JsonValueKind.Array ? offers[0] : offers` branch takes the
  `else` path correctly.
- There is **no `offers.price`** property directly on `offers` — price is nested
  inside `priceSpecification`. This does not affect stock detection (parser only
  reads `availability`).
- `@type` is `"Product"` — `IsProductType()` returns `true` correctly.

**Conclusion: `JsonLdStockParser` should detect stock state correctly against real fixtures.**

---

## Finding 2 — ButtonStateParser returns Indeterminate (expected)

All buttons on the Ubiquiti EU store pages use **Styled Components hash class names**
(e.g., `sc-9g0maw-7 VuqIa`, `sc-plhx78-2 udEvt`). There are no semantic class names
such as `add-to-cart`, `sold-out`, `out-of-stock`, or `notify-me`.

Sample buttons observed:

| Button text | Classes | Disabled |
|-------------|---------|---------|
| "Open" | `sc-9g0maw-7 VuqIa` | false |
| "Continue to Europe Store" | `sc-plhx78-2 udEvt sc-pyqe1m-5 fMrPpV` | false |
| "Select Other Country / Region" | `sc-pyqe1m-6 ixFFKn` | false |
| *(empty)* | `sc-12saxh8-0 iYcxjP sc-ziu8v3-1 llYmhi` | false |

**Conclusion: `ButtonStateParser` returning `Indeterminate` for real Ubiquiti pages
is correct behaviour, not a bug.** This must be validated by a test asserting
`Indeterminate` (not a failure).

---

## Finding 3 — TextContentParser can detect real stock states

The following visible text nodes were present in the rendered DOM:

### UNAS Pro 4 (OutOfStock)

| Text node | Parser interpretation |
|-----------|-----------------------|
| `"Sold Out"` | → OutOfStock ✓ |
| `"To subscribe to back in stock emails."` | → Could reinforce OutOfStock |
| `"Add to Cart"` | → In a "notify me" context — would need care if used |

> The "Add to Cart" text appearing on an OutOfStock page is from the notify/subscribe
> form section, not the primary purchase CTA. TextContentParser must prioritise
> "Sold Out" over "Add to Cart" when both are present. A confidence weighting or
> early-return on "Sold Out" is needed.

### UNAS Pro 8 (InStock)

| Text node | Parser interpretation |
|-----------|-----------------------|
| `"Add to Cart"` | → InStock (only relevant stock text visible) |

No "In Stock" text node was observed — InStock detection relies on absence of
out-of-stock signals combined with "Add to Cart".

**Conclusion: `TextContentParser` can improve detection but requires "Sold Out"
to take priority over "Add to Cart" when both are present.**

---

## Finding 4 — Anti-bot and rate-limiting signals

### Response headers (main page request)

```
Content-Type:     text/html; charset=utf-8
Content-Encoding: gzip
Cache-Control:    private, no-cache, no-store, max-age=0, must-revalidate
Vary:             Accept-Encoding
Set-Cookie:       eu_has_ui_care=true; Path=/; Secure; SameSite=strict; Max-Age=300
```

- **Content-Encoding: gzip** — `HttpClient` decompresses this automatically via
  `AutomaticDecompression`. No issue.
- **Cache-Control: no-store** — Every request hits origin. Caching is not available.
- **Set-Cookie** — Session tracking present. Observed max-age is 300 s (5 minutes).
- **No Cloudflare WAF / Bot detection headers observed** in the HAR for the page
  itself. OneTrust cookie consent JS is loaded but only runs in the browser.

### Rate limiting observed during live polling

During the 2026-03-24 discovery session, simultaneous polling of two products
triggered a **429 Too Many Requests** response on the second product. The parsing
engine then ran against the 429 error page HTML (which contains no product JSON-LD)
and returned Indeterminate.

**The token-bucket rate limiter in `ClientSideRateLimitHandler` was in place but
the initial poll burst still fired both products concurrently.** The per-product
poll interval setting does not prevent the bootstrap burst.

**Conclusion: The root cause of both products showing `Indeterminate` after first
boot is most likely 429 rate-limiting on the burst, not a parser bug.**

---

## Finding 5 — Geo-location dialog delays DOM hydration

During Playwright capture, `waitForSelector('script[type="application/ld+json"]')`
timed out at 15 seconds. A **geo-location/region selection dialog** appears on first
load ("Continue to Europe Store" / "Select Other Country / Region"). This dialog is
rendered before the product page content is hydrated in the browser.

However, the JSON-LD IS present in the **server-rendered HTML response body** from
the start (confirmed by plain HTTPS GET). The dialog does not affect `HttpClient`
fetches in the production polling service.

The Playwright timeout warning is a **false alarm** specific to the capture script's
browser session. The `page.content()` call after the additional 3 s wait correctly
captured the fully rendered DOM including JSON-LD.

---

## Parser Assessment Summary

| Parser | Expected result (real page) | Assessment |
|--------|----------------------------|------------|
| `JsonLdStockParser` | InStock / OutOfStock at confidence 0.95 | ✅ Should work — JSON-LD structure matches parser expectations |
| `ButtonStateParser` | Indeterminate | ✅ Correct — no semantic button classes |
| `TextContentParser` | Partial — "Sold Out" detectable; "Add to Cart" ambiguous | ⚠️ Needs priority fix: "Sold Out" must outweigh "Add to Cart" |

---

## Recommended Actions for task-phase-3-realworld

1. **Replace synthetic fixtures** with the real HTML files captured here.
2. **Add `JsonLdStockParser` tests** against real fixtures — expect InStock / OutOfStock.
3. **Add `ButtonStateParser` test** against real fixture — expect Indeterminate (pass).
4. **Update `TextContentParser`** to give "Sold Out" precedence over "Add to Cart"
   when both are present on the same page. Add tests.
5. **Add `CompositeStockParser` integration tests** against both real fixtures.
6. **Do NOT change `JsonLdStockParser` selector** — it already matches the real tag.
7. **Investigate bootstrap burst rate-limiting** in `PollSchedulerService` to prevent
   simultaneous first-poll for all products.

---

## Files produced by this task

| File | Description |
|------|-------------|
| `real-unas-pro-4-outofstock.html` | Full rendered HTML — UNAS Pro 4 (OutOfStock) |
| `real-unas-pro-8-instock.html` | Full rendered HTML — UNAS Pro 8 (InStock) |
| `har/real-unas-pro-4-outofstock.har` | HAR — all network requests for Pro 4 capture |
| `har/real-unas-pro-8-instock.har` | HAR — all network requests for Pro 8 capture |
| `discovery-raw.json` | Raw JSON from Playwright DOM analysis |
| `DISCOVERY.md` | This document |
