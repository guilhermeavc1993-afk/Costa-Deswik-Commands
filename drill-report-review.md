# Reviewing probe-drilling campaigns from a drill report workbook

The workflow:
1. Paste the campaign CSV (design holes) into a review template.
2. Fill the printed driller-comments sheet and the data-input sheet from the drilling contractor's daily report export.
3. Flag holes that didn't behave as expected.

## Report export sheets used
- **Drill:** one row per hole drilled. Columns: hole no, date, shift, operator, rig, pit, RL, shot ID, actual depth, angle, voids Y/N, comments. Filter by shot ID (the campaign name).
- **Ground Condition:** one row per 0.5 m interval. Columns: hole no, shot, from, to, condition code. Merge consecutive intervals with the same code into runs, e.g. `0-12 OC, 12-25.5 VO`.
- **QA Data / Shift Comment:** extra context only.

## Typical condition codes (confirm with site)
| Code | Meaning |
|---|---|
| SC | sulphide competent / cover |
| OC | oxide competent / cover |
| BS | broken sulphide |
| BO | broken oxide |
| FS | fill (sulphide) |
| FO | fill (oxide) |
| VO | void |

## Comment format used
`<what the driller found> / <ground log runs> / Expected Void @ <depth>m as per Voids Model as Built`
- Leave the driller's own words in the first part when there are any. Otherwise describe the ground log, e.g. "Fill intercepted @ 17.5m to 27.5m".
- Expected breakthrough is the planned void depth. If it isn't given, a placeholder is hole depth + subdrill, with the sign convention from the CSV.
- **Actual void depth** = the start of the first VO, FO or FS run.
- **Highlight Y** (not as expected) when there's no void intercept. Review large variances once the real expected depths are in.
- Watch for duplicated report rows (a 0 m first attempt) and holes missing from the report. List those in the missing-quality-reports tab.
