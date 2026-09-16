import React, { useState, useEffect } from 'react';
import { triageApi } from '../services/triageApi';
import './DoctorApprovalModal.css';

const DoctorApprovalModal = ({ triageCase, onClose, onApprove }) => {
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [notes, setNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);

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

  const handleApprove = async () => {
    if (!notes.trim()) {
      alert("Please enter approval notes.");
      return;
    }
    
    setSubmitting(true);
    try {
      await triageApi.approveTriage(triageCase.id, notes);
      onApprove();
    } catch (err) {
      console.error(err);
      alert("Failed to approve. See console.");
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
            <label>Doctor's Notes (Required for Dispatch)</label>
            <textarea 
              className="input-field" 
              rows="3" 
              placeholder="Enter clinical rationale for approval..."
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            ></textarea>
          </div>
        </div>

        <div className="modal-footer">
          <button className="btn btn-secondary" onClick={onClose} disabled={submitting}>
            Cancel
          </button>
          <button className="btn btn-danger" onClick={handleApprove} disabled={submitting}>
            {submitting ? 'Approving...' : 'Approve for Dispatch'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default DoctorApprovalModal;
