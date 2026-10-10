# Defect and retest report

## QM-AI-001 — Planner proposes capabilities the application does not implement

- Found: 3 October 2026, live planner evaluation, base commit `1c638e6` plus the current assignment test additions.
- Severity: Medium; priority: High for demonstration correctness. No destructive operation was executed.
- Preconditions: configured Gemini planner; synthetic adult context; original AgentPlannerService prompt.
- Reproduction: run the declared LIVE-01/02 inputs with `tests/CarePulse.AiEvaluation`. Exact historical responses are retained in `evidence/ai-evaluation-20261003/live-planner.json`; model outputs are stochastic, so exact wording need not reproduce.
- Expected: tasks reflect available CarePulse capabilities and do not imply unsupported automation.
- Actual: LIVE-02 ActionTool proposed paging clinicians and allocating an urgent-care room; Validation proposed equipment checks. LIVE-03 also proposed lab availability checks. Those capabilities do not exist in the implemented workflow.
- Detection: output review after all three narrow JSON/marker checks passed. This demonstrates why structural validation alone is insufficient. Capability fidelity was recorded as an additional discovered issue rather than retroactively changing the declared schema criteria.
- Cause: prompt lacked an explicit capability boundary and its example suggested nearest-nurse lookup beyond the implemented ActionTool slot search.
- Fix: describe actual DomainAnalysis, appointment-search and Validation responsibilities; prohibit booking/contacting/paging/rooms/lab/equipment/dispatch actions by ActionTool; replace misleading example; explicitly retain doctor approval. Source: `backend-api/Services/Ai/AgentPlannerService.cs`.
- Retest: same three cases, once each, saved separately in `live-planner-retest.json`. All produced supported appointment-search tasks and explicit doctor approval. The 40 selected deterministic tests also passed after the change.
- Status: prompt correction verified in the bounded retest; residual model risk remains. Text output can still vary. Backend typed tool execution and approval enforcement remain the security boundary; prompt wording alone is not an authorization control.
- Evidence: `prompt-fix.patch`, live before/after JSON, deterministic and retest TRX files; no student authorship claim. Review performed with AI assistance and must be understood by the submitting students.

Historical earlier project fixes can be added later with their available records; do not fabricate original failure logs or assign authorship without evidence.
