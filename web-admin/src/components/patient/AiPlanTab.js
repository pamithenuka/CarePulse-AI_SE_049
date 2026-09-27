import { useCallback, useEffect, useState } from "react";
import { createAiPlan, getAiPlans, reviewAiPlan } from "../../api/aiApi";
import { useAuth } from "../../context/AuthContext";
import { EmptyState, ErrorState, LoadingState } from "../StatusView";
import "../StatusView.css";
import "./PatientTabs.css";
import "./AiPlanTab.css";

const STATUS_LABELS = {
  PlanCreated: "Plan created",
  ValidationFailed: "Validation failed",
  LlmError: "AI service error",
};

export default function AiPlanTab({ patientId }) {
  const { hasRole } = useAuth();
  const isAdmin = hasRole("Admin");

  const [plans, setPlans] = useState([]);
  const [status, setStatus] = useState("loading");
  const [errorMessage, setErrorMessage] = useState("");

  const [objective, setObjective] = useState("");
  const [isGenerating, setIsGenerating] = useState(false);
  const [formError, setFormError] = useState("");

  const [reviewingId, setReviewingId] = useState(null);
  const [reviewNotes, setReviewNotes] = useState("");

  const loadPlans = useCallback(async () => {
    setStatus("loading");
    try {
      const data = await getAiPlans(patientId);
      setPlans(data);
      setStatus("success");
    } catch (err) {
      setErrorMessage(err.response?.data?.message || "Failed to load AI plans.");
      setStatus("error");
    }
  }, [patientId]);

  useEffect(() => {
    loadPlans();
  }, [loadPlans]);

  const handleGenerate = async (event) => {
    event.preventDefault();
    setFormError("");

    if (objective.trim().length < 5) {
      setFormError("Describe the objective in a bit more detail.");
      return;
    }

    setIsGenerating(true);
    try {
      const created = await createAiPlan(patientId, objective.trim());
      setPlans((prev) => [created, ...prev]);
      setObjective("");
    } catch (err) {
      setFormError(err.response?.data?.message || "Failed to generate an AI plan.");
    } finally {
      setIsGenerating(false);
    }
  };

  const handleReview = async (workflowId, approved) => {
    try {
      const updated = await reviewAiPlan(patientId, workflowId, { approved, reviewNotes });
      setPlans((prev) => prev.map((p) => (p.id === workflowId ? updated : p)));
      setReviewingId(null);
      setReviewNotes("");
    } catch (err) {
      setFormError(err.response?.data?.message || "Failed to record the review.");
    }
  };

  return (
    <div className="detail-card">
      <h2>Agent 1 — AI Care Plan</h2>
      <p className="hint-text">
        Log what the patient reported — over the phone, during a visit, or relayed by nursing staff (this stands in
        for the mobile app's symptom submission until that's connected). Agent 1 (Coordinator/Planner) reads this
        patient's record plus that report, then asks the AI model to produce a structured plan delegating to the
        DomainAnalysis, ActionTool and Validation agents for your review.
      </p>

      <form className="inline-add-form" onSubmit={handleGenerate}>
        {formError && (
          <div className="alert alert-error" role="alert">
            {formError}
          </div>
        )}
        <label htmlFor="ai-plan-objective" className="ai-plan-field-label">
          Patient-reported symptoms
        </label>
        <textarea
          id="ai-plan-objective"
          placeholder="What did the patient report? e.g. &quot;Patient called saying they have chest pain and shortness of breath&quot;"
          value={objective}
          onChange={(e) => setObjective(e.target.value)}
        />
        <button type="submit" disabled={isGenerating}>
          {isGenerating ? "Generating plan..." : "Generate AI Plan"}
        </button>
      </form>

      {status === "loading" && <LoadingState label="Loading AI plans..." />}
      {status === "error" && <ErrorState message={errorMessage} onRetry={loadPlans} />}
      {status === "success" && plans.length === 0 && <EmptyState message="No AI plans have been generated for this patient yet." />}

      {status === "success" && plans.length > 0 && (
        <ul className="ai-plan-list">
          {plans.map((plan) => (
            <li key={plan.id} className={`ai-plan-item ai-plan-${plan.status.toLowerCase()}`}>
              <div className="ai-plan-header">
                <span className={`badge ai-status-badge ai-status-${plan.status.toLowerCase()}`}>
                  {STATUS_LABELS[plan.status] || plan.status}
                </span>
                <span className="ai-plan-model">model: {plan.modelUsed}</span>
                <span className="ai-plan-date">{new Date(plan.createdAt).toLocaleString()}</span>
              </div>

              <p className="ai-plan-objective">
                <strong>Objective:</strong> {plan.objective}
              </p>

              {plan.status === "PlanCreated" ? (
                <>
                  <p className="ai-plan-summary">{plan.summary}</p>
                  <ol className="ai-plan-steps">
                    {plan.steps.map((step, index) => (
                      <li key={index}>
                        <span className="badge">{step.agent}</span> {step.task}
                      </li>
                    ))}
                  </ol>
                </>
              ) : (
                <p className="ai-plan-error">{plan.errorMessage}</p>
              )}

              <p className="hint-text ai-plan-tool">{plan.toolCallSummary}</p>

              {plan.status === "PlanCreated" && (
                <div className="ai-plan-review">
                  {plan.reviewStatus === "NotReviewed" ? (
                    isAdmin && (
                      <>
                        {reviewingId === plan.id ? (
                          <div className="ai-review-form">
                            <textarea
                              placeholder="Review notes (optional)"
                              value={reviewNotes}
                              onChange={(e) => setReviewNotes(e.target.value)}
                            />
                            <div className="form-actions">
                              <button type="button" onClick={() => handleReview(plan.id, true)}>
                                Approve
                              </button>
                              <button type="button" className="btn-danger" onClick={() => handleReview(plan.id, false)}>
                                Reject
                              </button>
                              <button type="button" className="btn-secondary" onClick={() => setReviewingId(null)}>
                                Cancel
                              </button>
                            </div>
                          </div>
                        ) : (
                          <button type="button" onClick={() => setReviewingId(plan.id)}>
                            Review this plan
                          </button>
                        )}
                      </>
                    )
                  ) : (
                    <span className={`badge ai-status-badge ai-status-${plan.reviewStatus.toLowerCase()}`}>
                      {plan.reviewStatus} by {plan.reviewedByName}
                      {plan.reviewNotes ? `: "${plan.reviewNotes}"` : ""}
                    </span>
                  )}
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
