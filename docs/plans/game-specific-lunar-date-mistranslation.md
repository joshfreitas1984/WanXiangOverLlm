# Chinese lunar calendar date mistranslations (game-specific)

Status: idea/parked, not yet implemented. Found 2026-09-16 while investigating a QC false-positive
report (see the "N年春节" achievement-chain fix, already applied — `Translate/GameFileHandling.cs`).

## Problem

While checking whether the "N年春节" (Year N Spring Festival) numeral-parsing bug had siblings
elsewhere in the corpus, found a second, distinct family of date-related mistranslations: Chinese
calendar month/day references (`X月Y[日/初]`) rendered as if they were Gregorian dates using English
month names ("July", "August", etc.) — plus several outright hallucinations unrelated to the
convention question. Full corpus search done via reverse-matching every `<EnglishMonth> <day>`
occurrence in `Files/Converted/*.yaml` back to its Chinese source (see session transcript for the
exact script) turned up two different bug classes:

### Class A — correctly parsed, wrong calendar convention (8 confirmed rows)

Source correctly identifies month+day, but renders it as a solar-calendar English month name, which
asserts a Gregorian equivalence the source never claims (`初`-prefixed/plain Chinese day-numbering is
a traditional lunar-calendar convention, not tied to a specific Gregorian date):

| Source | Currently | File |
|---|---|---|
| 七月十五 (×4) | "July 15th" | `EventDialog.json.yaml` |
| 八月二十五 | "August 25" | `EventDialog.json.yaml` |
| 二月十二 | "February 12th" | `EventDialog.json.yaml` |
| 四月十五 | "April 15th" | `EventDialog.json.yaml` |
| 七月三十 | "July 30th" | `EventDialog.json.yaml` |
| 三月初三 | "March 3rd" | `EventDialog.json.yaml` |
| 十二月十五 | "December 15" | `EventDialog.json.yaml` |
| 五月初五 (×10 total, standalone + embedded) | "May Fifth"/"May 5th" | `News.json.yaml`, `EventDialog.json.yaml`, `Hero.json.yaml` |
| 八月十二 (×6 total) | "August twelfth"/"August 12" | `News.json.yaml`, `EventDialog.json.yaml` |

Unlike the Year-N chain, Chinese calendar day-naming here follows fully regular rules (初一-初十 =
1-10, 十一-十九 = 11-19, 二十 = 20, 二十一-二十九 = 21-29, 三十 = 30, 三十一 = 31; months 一月-十二月
= 1-12), so a real parser is safe here — the Year-N chain needed a hardcoded lookup specifically
*because* its abbreviated shorthand (二一 for 21, not the grammatical 二十一) wasn't a regular rule.

### Class B — hallucinations, not a convention problem (5 confirmed rows)

The month and/or day number itself is invented, not just mis-styled — no amount of "always render
this consistently" fixes these, since there's no consistent underlying value to normalize to:

| Source | Currently | Problem |
|---|---|---|
| `初一，上午。` | "January 1st" | No month in source at all — invented |
| `初二，上午。` | "February 8th" | Invented month; day should be 2nd, not 8th |
| `...第八年七月开始...` (`Hero.json.yaml`) | "July 20" | Source has year+month only, no day — invented |
| `...第六年十二月...` (`Relation.json.yaml`) | "December 20" | Source has year+month only, no day — invented |
| `挂锤庄友好到达40...` (`Relation.json.yaml`) | "April 4" | Source has **no date reference at all** — pure fabrication, likely the model pattern-matching the number 40 into a date shape |

## Why parked rather than fixed now

Two different fixes needed, and the canonical phrasing for Class A isn't decided yet:

1. **Class A**: build a real Chinese month/day parser (safe here, unlike Year-N) and a
   `GameHooks.CustomColumnRepair`/`CustomPostRepair`-style rule that recognizes `X月Y[日/初]`
   substrings in raw text and forces a calendar-neutral rendering wherever they appear in output —
   needs substring replacement within larger sentences, not whole-field replacement like the Year-N
   fix, since most of these are embedded in longer dialogue/description text, not isolated splits.
   **Open question**: canonical phrasing style not yet chosen — options raised in session were "Day
   12, Month 8" (user's suggestion) vs "the 12th day of the 8th month" vs something else. Needs a
   decision before implementation, since it'll be applied to ~20+ rows.
2. **Class B**: no general rule can fix these algorithmically — there's nothing consistent to force-
   correct *to* when the source has no date info at all (`挂锤庄友好到达40`). These need direct,
   individual correction (read the surrounding context, decide the right text by hand) rather than a
   repair hook. Worth checking whether re-running translation on just these specific lines (after
   whatever caused the hallucination is understood) produces something sane, before hand-editing.

## Before implementing, when this gets picked up

- Confirm Class A's canonical phrasing with the user.
- Re-run the corpus-wide search (the reverse-match-English-month-name-back-to-Chinese-source script,
  not preserved as a file — recreate from this doc's description) in case more rows exist that this
  session's pass missed (search was English-side, so any Chinese date rendered without an English
  month name at all wouldn't have been caught even if it's otherwise wrong).
- For Class B, investigate `挂锤庄友好到达40` specifically before assuming the other four hallucinations
  share its exact root cause - it has no date pattern in source at all, which is a qualitatively
  different failure than "correct month, wrong/missing day."
