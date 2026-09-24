import React, { useState, useEffect } from 'react';
import { triageApi } from '../services/triageApi';
import DoctorApprovalModal from './DoctorApprovalModal';
import './TriageDashboard.css';

const TriageDashboard = () => {
  const [cases, setCases] = useState([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [selectedCase, setSelectedCase] = useState(null);

  const fetchCases = async () => {
    try {
      setLoading(true);
      const data = await triageApi.getPendingApprovals();
      setCases(data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCases();
    
    // Auto refresh every 10 seconds (mocking live queue)
    const interval = setInterval(fetchCases, 10000);
    return () => clearInterval(interval);
  }, []);

  const handleReview = (triageCase) => {
    setSelectedCase(triageCase);
  };

  const handleCloseModal = () => {
    setSelectedCase(null);
  };

  const handleApprove = async () => {
    setSelectedCase(null);
    await fetchCases(); // Refresh list
  };

  const filteredCases = cases.filter(c => 
    c.id.toLowerCase().includes(search.toLowerCase()) ||
    c.symptoms.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="dashboard-container glass-panel">
      <div className="dashboard-header">
        <div>
          <h2>Approval Queue</h2>
          <p className="subtitle">High-risk cases requiring immediate clinical review</p>
        </div>
        <div className="dashboard-actions">
          <input 
            type="text" 
            placeholder="Search symptoms or ID..." 
            className="input-field search-box"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <button className="btn btn-secondary" onClick={fetchCases}>
            Refresh
          </button>
        </div>
      </div>

      <div className="table-container">
        {loading && cases.length === 0 ? (
          <div className="loading-state">Loading pending cases...</div>
        ) : (
          <table className="triage-table">
            <thead>
              <tr>
                <th>Case ID</th>
                <th>Symptoms</th>
                <th>Risk Score</th>
                <th>Level</th>
                <th>Recommended Action</th>
                <th>Status</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {filteredCases.length === 0 ? (
                <tr>
                  <td colSpan="7" className="empty-state">No pending approvals found.</td>
                </tr>
              ) : (
                filteredCases.map(c => (
                  <tr key={c.id} className="table-row">
                    <td className="cell-id">{c.id.substring(0, 8)}...</td>
                    <td className="cell-symptoms">{c.symptoms}</td>
                    <td>
                      <div className="score-ring" style={{'--score': c.riskScore}}>
                        {c.riskScore}
                      </div>
                    </td>
                    <td>
                      <span className={`badge badge-${c.riskLevel.toLowerCase()}`}>
                        {c.riskLevel}
                      </span>
                    </td>
                    <td><span className="action-text">{c.recommendedAction ? c.recommendedAction.replace(/_/g, ' ') : ''}</span></td>
                    <td><span className="status-text">{c.status.replace(/_/g, ' ')}</span></td>
                    <td>
                      <button 
                        className="btn btn-primary"
                        onClick={() => handleReview(c)}
                      >
                        Review
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        )}
      </div>

      {selectedCase && (
        <DoctorApprovalModal 
          triageCase={selectedCase} 
          onClose={handleCloseModal}
          onApprove={handleApprove}
          onReject={handleApprove}
        />
      )}
    </div>
  );
};

export default TriageDashboard;
