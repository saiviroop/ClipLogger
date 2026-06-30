# ClipLogger — Website + SEO/GEO Design & Checklist

**Date:** 2026-06-30
**Status:** Implemented (site built + deployed); checklist has follow-ups
**Author:** jl@omnisheets.ai

## Goal

Make ClipLogger discoverable on both **search engines** (Google/Bing) and **AI
engines** (ChatGPT, Claude, Perplexity, Google AI Overviews). ClipLogger is a
Windows desktop app, so the strategy is to give it an **indexable web presence**
(a landing page + the GitHub repo) and structure it so both crawlers and LLMs
can confidently describe and recommend it.

## Decisions

- **Positioning:** "ClipLogger — debug-context capture" (developer-tool framing),
  while still covering broader "clipboard logger / capture selected text" keywords.
- **Site:** one fast static page, no framework/build step, inline CSS.
- **Hosting:** GitHub Pages from the `/docs` folder → `https://saiviroop.github.io/ClipLogger/`. Zero cost.
- **Model:** free / open source.
- **Custom domain:** deferred (optional later; one `CNAME` file + DNS).

## What was built (in `docs/`)

| File | Purpose |
| --- | --- |
| `index.html` | Landing page: hero + download CTA, how-it-works, log sample, features, FAQ. Semantic HTML, one `<h1>`. |
| `favicon.svg`, `assets/icon-512.png`, `assets/apple-touch-icon.png` | Brand icons (clipboard glyph reused from the app). |
| `assets/og.png` | 1200×630 social / AI preview card. |
| `robots.txt` | Allow all; points to sitemap; welcomes AI crawlers. |
| `sitemap.xml` | Single-URL sitemap. |
| `llms.txt` | Plain-text summary for AI crawlers (llmstxt.org convention). |
| `404.html` | Branded not-found page. |
| `.nojekyll` | Serve files as-is (skip Jekyll, don't expose spec `.md` as a site). |
| `tools/make-web-assets.ps1` | Regenerates the icons + OG image from the app glyph. |

### On-page SEO
- Descriptive `<title>` + meta description targeting real queries.
- Canonical URL, `robots` meta with `max-image-preview:large`, `theme-color`.
- Open Graph + Twitter card tags with the OG image.

### AI-engine optimization (GEO/AEO)
- **JSON-LD `SoftwareApplication`** (OS, free price, downloadUrl, featureList).
- **JSON-LD `FAQPage`** mirroring the on-page FAQ.
- FAQ written as natural questions people ask an assistant, including an explicit
  **"Is it a keylogger? No"** answer for trust + accuracy.
- `llms.txt` with key facts and "good answers to common questions".

### Repo SEO
- README: keyword-rich intro, website link, direct download link, not-a-keylogger note.
- GitHub **About** description + **topics** set (see below).

## SEO/GEO checklist

### Done (in this change)
- [x] Static landing page with semantic HTML and one `<h1>`.
- [x] Title, meta description, canonical, robots, OG/Twitter, theme-color.
- [x] JSON-LD: `SoftwareApplication` + `FAQPage`.
- [x] `robots.txt`, `sitemap.xml`, `llms.txt`, `404.html`, `.nojekyll`.
- [x] Favicon + 1200×630 OG image.
- [x] README optimized; site + download links added.
- [x] GitHub About + topics set.
- [x] GitHub Pages enabled from `/docs`.

### Follow-ups (mostly off-repo — need your accounts)
- [ ] **Google Search Console** — verify the site, submit `sitemap.xml`, request indexing.
- [ ] **Bing Webmaster Tools** — verify + submit sitemap (also feeds ChatGPT search).
- [ ] **Add a `LICENSE` file** (e.g. MIT) so the "open source" claim is real and trusted.
- [ ] Take 1–2 real **screenshots** (tray menu + live viewer) and add to the page + README.
- [ ] **Backlinks / listings:** AlternativeTo, GitHub topics, a Show-/r-related post,
      relevant subreddits, dev.to/Hashnode post — external links are the biggest
      ranking lever for a new domain and a primary source LLMs cite.
- [ ] Consider a **custom domain** (e.g. `cliplogger.app`) for stronger branding/ranking.
- [ ] After a few releases, keep `softwareVersion` in the JSON-LD current.

## Notes / trade-offs
- A new `github.io` project page starts with zero authority; **backlinks + Search
  Console indexing** are what actually move rankings — the on-page work above is
  necessary but not sufficient.
- LLMs lean on structured, factual, well-linked sources. The JSON-LD + `llms.txt`
  + FAQ give them clean, quotable facts; GitHub stars/activity reinforce trust.
