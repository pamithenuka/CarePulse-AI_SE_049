# Before submitting the testing assignment

Deadline in the supplied PDF: **5 October 2026**. Submit through CourseWeb. The PDF says SE3090 although its filename says SE3110; confirm the correct submission area with the lecturer.

## Prepared files

- [x] [Software Testing Report PDF](SOFTWARE_TESTING_REPORT.pdf) and [editable version](SOFTWARE_TESTING_REPORT.md).
- [x] [Test plan](TEST_PLAN.md), [case document](TEST_CASES.csv), [defect/retest report](DEFECT_REPORT.md) and [execution summary](EXECUTION_SUMMARY.md).
- [x] [Evidence index](EVIDENCE_INDEX.md), raw reports and rerun instructions.
- [x] Test source and scripts in the repository. Tests used local synthetic data, not shared Neon.

- [x] [Member testing/viva guide](INDIVIDUAL_TESTING_GUIDE.md) and [contribution record](CONTRIBUTION_RECORD.md).

## Team tasks still required

- [ ] Map all names to project/testing responsibilities. All four supplied names/IDs are already in the report. The proposed student areas are not authorship evidence.
- [ ] Each member personally runs, understands and can explain at least one meaningful tool-based test. Record their actual work and relevant commits; do not assign AI-created work to a student who did not do it.
- [ ] Review the remaining manual checks in NONFUNCTIONAL_EVALUATION.md. Run the app-only flow if possible and record real results. Keep INT-02 Not run unless supported by new evidence.
- [ ] Complete the required CLEAR/AI declaration using the lecturer's instructions. Declare AI-assisted tests and report writing honestly.
- [ ] Check TEST_CASES.csv. Unexecuted cases must stay Not run; do not change them to Passed just to remove gaps.
- [ ] Review changes, commit the intended files and record the final GitHub repository, branch and commit/PR links. Current evidence includes uncommitted source hashes and patches.
- [ ] Check CI for the submitted commit. Do not call it green based only on local tests or an older commit.
- [ ] Check that private settings, keys, tokens and patient data are excluded. Keep raw synthetic test reports; exclude caches/build output.
- [ ] Regenerate and read the PDF after any report edits. Confirm names, dates, results and links match the final documents.
- [ ] Submit the required documents, evidence and source/repository links through CourseWeb. Check its file and size rules first.

## Regenerate the PDF

From the project root, using a Python environment with ReportLab installed:

```sh
python3 -m pip install reportlab==5.0.1
python3 scripts/qm-build-report.py
```

The script reads SOFTWARE_TESTING_REPORT.md and replaces SOFTWARE_TESTING_REPORT.pdf. It does not run application tests or access a database.

## Quick viva preparation

For your chosen test, explain: the risk, input, expected result, actual assertion, tool, result and any limitation. Be ready to run the test and change one input when asked. Explain the real AI defect and its retest if it relates to your work. Do not claim that all tests passing proves medical safety or production readiness.
