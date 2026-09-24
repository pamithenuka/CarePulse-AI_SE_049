import React, { useState, useEffect } from 'react';
import { triageApi } from '../services/triageApi';
import './DoctorApprovalModal.css';

const DoctorApprovalModal = ({ triageCase, onClose, onApprove, onReject }) => {
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [notes, setNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [confirmingReject, setConfirmingReject] = useState(false);

  useEffect(() => {
    const fetchLogs = async () => {
      try {
        const data = await triageApi.getAuditLog(triageCase.id);
        setLogs(data);
      } catch (err) {
        console.error("Failed to fetch logs", err);
      } finally {
        setLoading(false);
      }
    };
    fetchLogs();
  }, [triageCase.id]);

  const requireNotes = (actionLabel) => {
    if (!notes.trim()) {
      alert(`Please enter doctor's notes before ${actionLabel}.`);
      return false;
    }
    return true;
  };

  const handleApprove = async () => {
    if (!requireNotes('approval')) {
      return;
    }

    setSubmitting(true);
    try {
      await triageApi.approveTriage(triageCase.id, notes);
      onApprove();
    } catch (err) {
      console.error(err);
      alert(err.message || 'Failed to approve this triage case. Please try again.');
      setSubmitting(false);
    }
  };

  const handleRejectClick = () => {
    if (!requireNotes('rejection')) {
      return;
    }
    setConfirmingReject(true);
  };

  const handleConfirmReject = async () => {
    if (!requireNotes('rejection')) {
      return;
    }

    setSubmitting(true);
    try {
      await triageApi.rejectTriage(triageCase.id, notes);
      alert('Triage case rejected successfully.');
      if (onReject) {
        onReject();
      } else {
        onApprove();
      }
    } catch (err) {
      console.error(err);
      alert(err.message || 'Failed to reject this triage case. Please try again.');
      setSubmitting(false);
    }
  };

  return (
    <div className="modal-backdrop">
      <div className="modal-content glass-panel">
        <div className="modal-header">
          <h3>Clinical Review: Triage Case</h3>
          <button className="close-btn" onClick={onClose}>&times;</button>
        </div>

        <div className="modal-body">
          <div className="case-details">
            <div className="detail-group">
              <label>Patient ID</label>
              <div className="detail-value font-mono">{triageCase.patientId}</div>
            </div>
            
            <div className="detail-group">
              <label>Risk Assessment</label>
              <div className="detail-value risk-display">
                <span className={`badge badge-${triageCase.riskLevel.toLowerCase()}`}>
                  {triageCase.riskLevel}
                </span>
                <span className="score">Score: {triageCase.riskScore}/10</span>
              </div>
            </div>

            {triageCase.recommendedSpecialty && (
              <div className="detail-group">
                <label>Recommended Specialty</label>
                <div className="detail-value">
                  {triageCase.recommendedSpecialty.replace(/_/g, ' ')}
                </div>
              </div>
            )}

            <div className="detail-group full-width">
              <label>Patient Symptoms</label>
              <div className="detail-value symptoms-box">
                {triageCase.symptoms}
              </div>
            </div>
          </div>

          <div className="audit-section">
            <h4>AI Audit Log</h4>
            <div className="audit-list">
              {loading ? (
                <div className="loading-text">Loading logs...</div>
              ) : logs.length === 0 ? (
                <div className="loading-text">No logs available.</div>
              ) : (
                logs.map(log => (
                  <div key={log.id} className="audit-item">
                    <div className="audit-time">{new Date(log.createdAt).toLocaleString()}</div>
                    <div className="audit-msg">{log.logMessage}</div>
                  </div>
                ))
              )}
            </div>
          </div>

          <div className="approval-section">
            <label>Doctor's Notes (Required)</label>
            <textarea 
              className="input-field" 
              rows="3" 
              placeholder="Enter clinical rationale for approval or rejection..."
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              disabled={submitting}
            ></textarea>
          </div>

          {confirmingReject && (
            <div className="reject-confirm">
              <p className="reject-confirm-title">Are you sure you want to reject this triage case?</p>
              <p className="reject-confirm-copy">This will not dispatch a nurse. The clinical notes below will be saved on the audit log.</p>
              <div className="detail-value symptoms-box reject-notes-preview">
                {notes.trim()}
              </div>
            </div>
          )}
        </div>

        <div className="modal-footer">
          {confirmingReject ? (
            <>
              <button
                className="btn btn-secondary"
                onClick={() => setConfirmingReject(false)}
                disabled={submitting}
              >
                Back
              </button>
              <button
                className="btn btn-danger"
                onClick={handleConfirmReject}
                disabled={submitting}
              >
                {submitting ? 'Rejecting...' : 'Confirm Reject'}
              </button>
            </>
          ) : (
            <>
              <button className="btn btn-secondary" onClick={onClose} disabled={submitting}>
                Cancel
              </button>
              <button className="btn btn-danger" onClick={handleRejectClick} disabled={submitting}>
                Reject
              </button>
              <button className="btn btn-primary" onClick={handleApprove} disabled={submitting}>
                {submitting ? 'Approving...' : 'Approve for Dispatch'}
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default DoctorApprovalModal;
