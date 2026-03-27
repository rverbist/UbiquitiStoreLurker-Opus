---
description: 'Start a Frontend Studio project — the agent will interview you for your design choices'
agent: Frontend Studio
---

The user wants to start a new frontend design project. Before doing any work, you MUST interview them by presenting the menu below. Show it exactly as formatted — the user picks from the options or writes their own.

Do NOT start building until the user has confirmed their choices. If the user's choices contain obvious conflicts, flag them before proceeding.

◉ / ○ pick one *(◉ = default)* · ☑ / ☐ pick any or skip *(☑ = preselected)*

Present this menu:

---

|  |  |
|---|---|
| | |
| | **Context** |
| **What** | *What are you building? Describe pages, sections, purpose.* <br/><br/> ☐ `single-page-landing` *hero, features, testimonials, pricing, footer* ☐ `multi-page-site` *home, about, services, contact* ☐ `portfolio` *projects grid, case studies, contact* ☐ `dashboard` *sidebar nav, metric cards, charts, settings* ☐ `event-page` *RSVP, countdown, gallery, map* ☐ `documentation` *sidebar TOC, code blocks, search, versioning* ☐ `e-commerce` *product grid, detail, cart, checkout* ☐ `blog-magazine` *article listing, detail, categories, authors* ☐ `admin-panel` *data tables, forms, user management* ☐ `email-template` *inline CSS, table layout* |
| **Audience** | *Who sees this and what should they do?* <br/><br/> ☐ `b2b-executives` *build trust, drive demo signups* ☐ `consumers` *browse, buy, sign up* ☐ `developers` *find docs, copy code* ☐ `creatives` *be impressed, make contact* ☐ `internal-team` *make decisions from data* ☐ `general-public` *learn, engage, share* |
| **Tone** | ◉ **`professional`** ○ `casual-friendly` ○ `playful-whimsical` ○ `authoritative-serious` ○ `warm-personal` ○ `edgy-provocative` ○ `luxurious-exclusive` ○ `techy-nerdy` ○ `inspirational` |
| | |
| | **Aesthetic** |
| **Theme** | ◉ **`auto-only`** *follows OS, no toggle* ○ `auto-toggle` *follows OS + user switch* ○ `light` ○ `dark` ○ `split` *dark hero, light body* |
| **Style** | ◉ **`brutally-minimal`** ○ `maximalist-chaos` ○ `retro-futuristic` ○ `organic-natural` ○ `luxury-refined` ○ `playful-toy-like` ○ `editorial-magazine` ○ `brutalist-raw` ○ `art-deco-geometric` ○ `soft-pastel` ○ `industrial-utilitarian` ○ `dark-moody` ○ `cinematic` ○ `cyber-technical` ○ `neo-vintage` ○ `scandinavian-clean` ○ `vaporwave` ○ `cottagecore` ○ `glassmorphism` ○ `neubrutalism` ○ `memphis` ○ `y2k-retro` ○ `swiss-grid` ○ `japanese-minimal` |
| **Colors** | ◉ **`corporate-navy-white`** ○ `warm-earth-tones` ○ `cool-ocean-blues` ○ `neon-on-black` ○ `muted-pastels` ○ `monochrome-grayscale` ○ `jewel-tones` ○ `sunset-gradient` ○ `forest-green` ○ `terracotta-sand` ○ `lavender-mist` ○ `rust-charcoal` ○ `candy-pop` ○ `gold-on-black` ○ `duotone` *specify 2 hues* ○ `brand-colors` *provide hex values* |
| **Typography** | ◉ **`bold-geometric-sans`** ○ `elegant-serif` ○ `handwritten-script` ○ `monospace-tech` ○ `editorial-display` ○ `retro-slab` ○ `rounded-friendly` ○ `condensed-industrial` ○ `calligraphic` ○ `pixel-art` ○ `variable-weight-fluid` ○ `brutalist-mono` ○ `art-nouveau-decorative` ○ `swiss-international` ○ `typewriter` ○ `stencil` ○ `mixed-pair` *specify display + body* |
| **Density** | ◉ **`comfortable`** *generous whitespace* ○ `compact` *tight, data-dense* ○ `airy` *extreme whitespace, editorial feel* |
| **Border&nbsp;radius** | ◉ **`subtle`** *4-8 px* ○ `sharp` *0 px* ○ `fully-rounded` *pill shapes* ○ `mixed` *sharp cards, round buttons* |
| **Iconography** | ◉ **`none`** ○ `line-icons` *Lucide / Feather* ○ `filled-icons` *Material / FA* ○ `duotone-icons` ○ `emoji` ○ `custom-illustrations` |
| **Imagery** | ◉ **`placeholder`** *generated placeholder boxes* ○ `stock-photo-urls` *provide Unsplash/Pexels links* ○ `geometric-abstract` *CSS/SVG shapes* ○ `illustrations` ○ `none` |
| | |
| | **Layout** |
| **Width** | ◉ **`boxed`** *max-width container* ○ `full-width` *edge to edge* ○ `hybrid` *full-width hero/sections, boxed content* |
| **Navigation** | ◉ **`top-navbar`** ○ `sidebar-left` ○ `sidebar-right` ○ `hamburger-mobile-only` ○ `full-screen-overlay` ○ `tab-bar-bottom` *mobile-app style* ○ `breadcrumb-only` ○ `none` |
| **Header&nbsp;behavior** | ◉ **`static`** ○ `sticky` *always visible* ○ `sticky-shrink` *shrinks on scroll* ○ `hide-on-scroll` *reappears on scroll-up* |
| **Hero** | ◉ **`full-viewport`** *100 vh, headline + CTA* ○ `split-image-text` *50/50* ○ `video-background` ○ `carousel-slider` ○ `text-only` ○ `none` |
| **Footer** | ◉ **`standard`** *links, copyright* ○ `mega-footer` *multi-column, newsletter* ○ `minimal` *single line* ○ `sticky-cta` *persistent action bar* ○ `none` |
| **Footer&nbsp;behavior** | ◉ **`static`** ○ `sticky` *always visible at bottom* ○ `reveal` *slides up when scrolled to end* |
| **Grid** | ◉ **`auto`** *Grid + Flexbox as needed* ○ `12-column` ○ `masonry` ○ `single-column` ○ `asymmetric` |
| | |
| | **Technical** |
| **Framework** | ◉ **`vanilla-html-css-js`** ○ `react` ○ `react-next` ○ `vue` ○ `vue-nuxt` ○ `svelte` ○ `sveltekit` ○ `astro` ○ `angular-1.8` ○ `angular` ○ `lit` ○ `alpine-js` ○ `htmx` |
| **CSS&nbsp;approach** | ◉ **`vanilla-css`** *custom properties* ○ `tailwind` ○ `scss-sass` ○ `css-modules` ○ `styled-components` ○ `emotion` |
| **Responsive** | ☑ **`desktop`** ☐ `tablet` ☐ `phone` ☐ `print` *device-first layout that is also acceptable when printed* ☐ `print-only` *markup targets print media exclusively — no device styling* |
| **Accessibility** | ◉ **`none`** ○ `basic` *alt text, contrast, keyboard nav* ○ `wcag-aa` *4.5 : 1 contrast, focus indicators, skip links, ARIA landmarks* ○ `wcag-aaa` *7 : 1 contrast, sign language for media, extended descriptions* |
| **Browsers** | ◉ **`evergreen`** *Chrome, Edge, Firefox, Safari latest* ○ `safari-compat` *+ Safari 15.x fallbacks* ○ `IE9` *DENIED — we will refuse this* |
| **Performance** | ◉ **`standard`** ○ `optimized` *lazy loading, critical CSS, font preload, srcset* ○ `extreme` *skeleton screens, intersection observers, prefetch, service worker* |
| **SEO** | ◉ **`basic`** *meta title, description, OG tags* ○ `full` *structured data / JSON-LD, sitemap hints, canonical URLs, Twitter cards* ○ `none` |
| **i18n&nbsp;/&nbsp;RTL** | ◉ `ltr-only` ○ `rtl-ready` *CSS logical properties, dir attribute* ○ `multi-language` *switcher, translatable strings* |
| | |
| | **Animations** |
| **Page&nbsp;load** | ◉ **`none`** ○ `preloader-sequence` ○ `staggered-entrance` ○ `fade-in-on-load` ○ `logo-reveal` ○ `curtain-wipe` |
| **Scroll** | ☐ `parallax-layers` ☐ `scroll-reveal-sections` ☐ `scroll-pinned-panels` ☐ `horizontal-scroll-gallery` ☐ `progress-bar` ☐ `smooth-scroll-anchors` ☐ `scroll-driven-counter` ☐ `scroll-zoom-hero` |
| **Hover** | ☐ `hover-scale` ☐ `hover-lift-shadow` ☐ `hover-glow` ☐ `hover-underline-slide` ☐ `magnetic-buttons` ☐ `card-tilt-3d` ☐ `image-zoom-on-hover` ☐ `hover-color-shift` ☐ `hover-border-draw` |
| **Cursor** | ☐ `custom-cursor` ☐ `cursor-trail` ☐ `magnetic-pull` ☐ `cursor-spotlight` |
| **Transitions** | ◉ **`none`** ○ `page-crossfade` ○ `slide-transitions` ○ `morph-transitions` ○ `view-transitions-api` |
| **Micro-interactions** | ☐ `button-ripple` ☐ `button-bounce` ☐ `toggle-flip` ☐ `input-focus-glow` ☐ `tooltip-fade` ☐ `notification-slide` ☐ `accordion-smooth` ☐ `tab-slide` ☐ `counter-animate` ☐ `progress-fill` ☐ `skeleton-loading` ☐ `checkbox-confetti` ☐ `form-success-animation` |
| **Popups&nbsp;&&nbsp;overlays** | ☐ `appear` *instant* ☐ `modal-fade` ☐ `modal-slide-up` ☐ `lightbox-zoom` ☐ `drawer-slide` ☐ `toast-notifications` ☐ `cookie-banner-slide` ☐ `bottom-sheet` *mobile drawer* |
| **Background&nbsp;FX** | ☐ `gradient-mesh` ☐ `noise-grain-overlay` ☐ `particle-field` ☐ `aurora-gradient` ☐ `animated-blobs` ☐ `geometric-pattern` |
| **Text&nbsp;FX** | ☐ `split-text-reveal` ☐ `typewriter-effect` ☐ `gradient-text` ☐ `text-scramble` ☐ `blur-to-sharp` |
| **3D&nbsp;/&nbsp;WebGL** | ☐ `three-js-scene` ☐ `parallax-3d-cards` ☐ `globe-visualization` ☐ `product-viewer-360` |
| | |
| | **Content & components** |
| **Content&nbsp;approach** | ◉ **`placeholder`** *lorem ipsum + placeholder images* ○ `real-content` *user will provide* ○ `ai-generated` *realistic dummy content matching the brief* |
| **Forms** | ☐ `contact-form` ☐ `newsletter-signup` ☐ `multi-step-wizard` ☐ `search-bar` ☐ `login-register` ☐ `survey-quiz` |
| **Data&nbsp;display** | ☐ `data-table` ☐ `chart-bar` ☐ `chart-line` ☐ `chart-pie` ☐ `stat-cards` ☐ `timeline` ☐ `kanban-board` ☐ `calendar` |
| **Media** | ☐ `image-gallery` ☐ `video-embed` ☐ `audio-player` ☐ `carousel` ☐ `before-after-slider` ☐ `lightbox` |
| **Social** | ☐ `share-buttons` ☐ `social-feed-embed` ☐ `testimonial-cards` ☐ `star-ratings` ☐ `user-avatars` |
| **Loading&nbsp;states** | ☐ `skeleton-screens` ☐ `spinner` ☐ `progress-bar` ☐ `shimmer-effect` |
| **Empty&nbsp;states** | ☐ `friendly-illustration` ☐ `text-with-cta` |
| | |
| | **Meta** |
| **References** | *(optional) URLs of sites whose feel you like* |
| **Path** | *(optional) output directory — leave blank for* `.frontend/design-YYYYMMDDHHmm/` |

---

After the user replies with their choices, parse them into your brief parameters and proceed with Step 1: PLAN.
