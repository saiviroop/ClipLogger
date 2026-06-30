# ClipLogger — Launch / Listing Copy

Ready-to-paste copy for directories, package managers, and communities. Goal:
earn legitimate listings on high-authority sites that Google already crawls —
this gets ClipLogger discovered/indexed fast **and** gives AI assistants sources
to cite. (No paid links, no spam — those hurt rankings.)

**Canonical links to drop everywhere:**
- Website: https://saiviroop.github.io/ClipLogger/
- GitHub: https://github.com/saiviroop/ClipLogger
- Direct download: https://github.com/saiviroop/ClipLogger/releases/latest/download/ClipLogger-Setup.exe
- Install (after winget merge): `winget install saiviroop.ClipLogger`

---

## Reusable descriptions

**One-liner (≤ 60 chars):**
> Log selected text to a timestamped file with one hotkey.

**Short (≤ 160 chars):**
> Free Windows tray app. Press Ctrl+Alt+C to append the selected text to a dated, timestamped log file — great for collecting debug context. Not a keylogger.

**Medium (paragraph):**
> ClipLogger is a free, lightweight Windows tray app for collecting debug
> context. Select any text — an error, a log line, a snippet — press
> Ctrl+Alt+C, and it's appended to a dated log file with a timestamp and the
> source app it came from. Normal copy/paste is untouched. It includes a live
> auto-refreshing viewer with search, and ships as a self-contained installer
> (no .NET needed). Windows 10/11, open source (MIT).

**Tags / keywords:**
clipboard logger, text capture, debugging, developer tools, productivity,
windows, tray app, logging, snippets, open source

---

# A. Software directories (high authority, AI-cited)

> Tip: do these first. They're durable, no-cost, and frequently quoted by
> ChatGPT/Perplexity for "best X" / "alternatives to X" questions.

### AlternativeTo — https://alternativeto.net
Sign in → click the **+** (top bar) / **Add application**.
- **Name:** ClipLogger
- **Tagline:** Capture selected text to a timestamped log with one hotkey.
- **Description:** Medium description (above).
- **License:** Open Source (MIT) · **Cost:** Free
- **Platforms:** Microsoft Windows
- **Official website:** https://saiviroop.github.io/ClipLogger/
- **Features to tag:** Clipboard Manager, Logging, Text Snippets, Hotkey support, System Tray
- **Position vs. similar (Ditto, ClipboardFusion):** ClipLogger is a *logger* — it
  appends your selections to a file, it's not a clipboard-history popup.

### Slant — https://www.slant.co
Search a relevant question (e.g. "best Windows clipboard managers" / "tools to
collect logs while debugging") → **Add option** → add ClipLogger with the Short
description and the website link.

### Softpedia — softpedia.com (footer → **Submit software**)
- **Title:** ClipLogger
- **Category:** Windows → System / Clipboard tools
- **License:** Freeware (Open Source / MIT)
- **Description:** Medium description.
- **Download URL:** the direct download link (above).
- **Homepage:** the website.
> Note: Softpedia editors test and write their own listing; just submit the URL + basics.

### MajorGeeks — majorgeeks.com (footer → **Submit Software**)
- Same fields as Softpedia. Freeware, Windows, with download + homepage URLs.

### SourceForge — https://sourceforge.net/create
Create a project "ClipLogger", add the Medium description, link the GitHub repo
and website, and (optional) mirror the installer as a release. SourceForge pages
rank well and add a strong backlink.

---

# B. Package managers (listing + backlink + real distribution)

### winget — Windows Package Manager
Manifest is built and validated in `packaging/winget/1.1.0/`. Submitting it to
`microsoft/winget-pkgs` (handled separately) makes `winget install
saiviroop.ClipLogger` work and adds a very high-authority listing.

### Scoop (optional later)
Add a manifest to a bucket (or submit to `ScoopInstaller/Extras`). Good for the
developer audience.

### Chocolatey (optional later)
A `.nuspec` + install script published to community.chocolatey.org. More involved
(moderation), but another indexed listing.

---

# C. Communities & content (links + traffic + AI sources)

### Show HN — https://news.ycombinator.com/submit
- **Title:** Show HN: ClipLogger – append selected text to a timestamped log with one hotkey
- **URL:** https://github.com/saiviroop/ClipLogger
- **First comment (post immediately after):**
> I kept collecting debug context — errors, logs, snippets — by pasting into one
> Notepad file and manually spacing entries. ClipLogger does that on a dedicated
> hotkey: select text, press Ctrl+Alt+C, and it appends to a dated file with a
> timestamp and the source app. Normal copy/paste is untouched, and there's a
> live viewer with search. It's a small C#/.NET WinForms tray app, MIT licensed.
> Feedback welcome — especially on the capture approach and any apps where the
> simulated copy doesn't work.

### Reddit
> Most subs limit self-promotion — post where it's on-topic and reply to comments.
> r/SideProject and r/coolgithubprojects are the most self-promo friendly.
> Also consider: r/windows, r/software, r/dotnet, r/csharp, r/programming (strict).
- **Title:** I built ClipLogger — a tiny Windows tray app that logs selected text to a file on a hotkey
- **Body:**
> I made a small free tool for collecting debug context. Select any text, hit
> Ctrl+Alt+C, and it gets appended to a dated log file with a timestamp and the
> source app. Normal copy/paste isn't affected, and there's a live viewer with
> search. Windows 10/11, MIT licensed, self-contained installer.
>
> Site: https://saiviroop.github.io/ClipLogger/
> Code: https://github.com/saiviroop/ClipLogger
>
> Would love feedback on the workflow and edge cases.

### dev.to / Hashnode (a durable, crawlable backlink)
- **Title:** Stop pasting debug context into Notepad — capture it with one hotkey
- **Tags:** windows, productivity, dotnet, opensource
- **Outline:**
  1. The problem: collecting logs/errors/snippets from many windows is tedious.
  2. The idea: a dedicated capture hotkey separate from Ctrl+C.
  3. How ClipLogger works (Ctrl+Alt+C → timestamped append + source tracking).
  4. The live viewer + check-in interval for keeping files organized.
  5. Why it's NOT a keylogger (only captures your selection, on demand).
  6. Build notes (C#/.NET, RegisterHotKey, Inno Setup) + links.

### Lobsters — https://lobste.rs/stories/new  (needs an account/invite)
Same title as Show HN; link the GitHub repo.

### Product Hunt — https://www.producthunt.com (Launch a product)
- **Tagline:** One hotkey to log selected text to a timestamped file.
- **Description:** Medium description. **First comment:** the Show HN comment.

### Optional: "awesome" lists (GitHub PRs — high authority)
Add a one-line entry to relevant curated lists, e.g. `awesome-windows`,
`awesome-dotnet`, clipboard/productivity lists. Suggested line:
> - [ClipLogger](https://github.com/saiviroop/ClipLogger) — Capture selected text to a timestamped log file with one hotkey (Ctrl+Alt+C). Windows, MIT.

---

## Etiquette & sequencing
- Don't blast everything in one hour. Spread posts over days; reply to comments.
- Directories (Section A) + winget first — they're evergreen and low-risk.
- Community posts (Section C) drive a spike of traffic + links; do them when you
  can be around to respond.
- Backlinks take days–weeks to register; pair with Search Console indexing.

## Progress tracker
- [ ] AlternativeTo
- [ ] Slant
- [ ] Softpedia
- [ ] MajorGeeks
- [ ] SourceForge
- [ ] winget (PR merged)
- [ ] Show HN
- [ ] Reddit (which sub: ______)
- [ ] dev.to / Hashnode article
- [ ] Product Hunt
- [ ] Paste each live URL here as you go (for your own tracking)
