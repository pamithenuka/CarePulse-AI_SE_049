# Student 4 report review - 8 October 2026

## Outcome

The supplied Word report already contains Student 4's 14 selected results, two AI-assisted additions, evidence paths, honest authorship and PR #11. These sections were not missing. No new tests were executed in this review and no new personal achievement is claimed.

The reviewed DOCX is a copy of the supplied report. Other members' results remain their claims and were not independently verified. Twelve existing paragraphs were corrected; DOCUMENT_CHANGES.json lists every change. Original paragraphs/tables and all other package parts were retained, apart from enabling field refresh in Word settings. Open in Word, review layout and update the contents before exporting the final group PDF. XML/package validation passed; visual pagination has not been verified.

## Corrections completed

- Distinguished the 6 October test date from the 7 October contribution/PR date.
- Identified Student 4's actual macOS/arm64 environment.
- Corrected the token-only case: it checks a token error, not outgoing requests; the added service test checks zero requests.
- Removed the implication that a service error test observes the rendered UI.
- Specified actual low-risk and high-risk fallback inputs/assertions.
- Corrected the evidence statement: all five Student 4 logs/TRX files are tracked in df0bd5d. A general ignore pattern does not untrack committed files.
- Identified existing targeted security and reliability coverage. No new load test is claimed.
- Added SHA-256 hashes and exact source snapshots from the contribution commit for traceability. These do not retroactively establish the original execution-time source state.

## Assessment of the screenshot's gaps

1. Personal implementation: valid concern about strength of evidence, but the rubric specifies no minimum count of newly authored tests. Two guided additions and execution are documented; they do not guarantee full marks for personally implemented tests. More generated code cannot honestly be called independent work. A meaningful student-designed/adapted case and explanation would strengthen this criterion.
2. Defect/fix/retest: the rubric assesses interpretation and fixes where applicable. It does not require inventing a defect for every student. Record no product defect found in these runs. The reruns verify extensions, not a repaired product bug. Be able to explain an actual defect only where you have genuinely reviewed or worked on it.
3. Non-functional work: performance and security are required for the group. The PDF does not say every student must run a separate k6/ZAP test. Student 4 already has session/ownership security and concurrency/failure reliability coverage. Student 1 performance/security claims need that member's supporting evidence, not a duplicate Student 4 load run.
4. Viva: explicitly deferred by the user; still part of individual assessment, not completed by this report edit.
5. Source commit: df0bd5d and PR #11 provide contribution traceability. Original runtime commit metadata was not captured. This historical limitation is retained. A new run from a clean checkout could establish fresh commit-linked results but must be reported as a new run, not a replacement date for the old evidence.

## Remaining actions

- Report author: use the reviewed DOCX or the change list in the latest group report. Do not overwrite newer work by other members. Check layout/contents and export the final PDF.
- Report author: verify other members' evidence, fill their placeholders and the group ID/totals; do not double count INT-01 and S4-BE-01, which are the same execution.
- Student 4: verify the declaration reflects actual assistance, and later practise explaining/modifying the selected tests. Full individual marking cannot be guaranteed from the current guided implementation evidence.
- Team: confirm any revised deadline (the original assignment PDF states 5 October; this report states 8 October) and any module CLEAR instructions. No revised deadline or CLEAR format was invented here.
- No automatic push, merge or submission was performed. Student 4 source/evidence are already represented by PR #11; this reviewed document is a new local artifact for the report author.

## Suggested message to the report author

Please incorporate the Student 4 corrections from SE3110_Group_Testing_Report_STUDENT4_REVIEWED.docx. My result count remains 14, not a new test run. The personal logs/TRX are already tracked in df0bd5d. Security/reliability are covered in my selected scope; I did not personally run the group load test or find a product defect in these runs. Preserve attribution and limitations. Other members' sections still need their verification. See DOCUMENT_CHANGES.json to apply only the corrections if your report has changed since this copy.
