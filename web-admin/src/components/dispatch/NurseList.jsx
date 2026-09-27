import React from 'react';
import { useDispatchStore } from '../../store/useDispatchStore';
import { Clock, MapPin, CheckCircle, Navigation } from 'lucide-react';
import './NurseList.css';

export const NurseList = () => {
  const { activeDispatches, availableNurses, isLoading } = useDispatchStore();

  return (
    <div className="nurse-list-container">
      <div className="section-header">
        <h2>Active Dispatches</h2>
        {isLoading && <span className="loading-indicator">Live...</span>}
      </div>
      
      <div className="dispatch-cards">
        {activeDispatches.map(dispatch => (
          <div key={dispatch.id} className={`dispatch-card ${dispatch.status.toLowerCase()}`}>
            <div className="card-header">
              <h3>{dispatch.nurseName}</h3>
              <span className={`status-badge ${dispatch.status.toLowerCase()}`}>
                {dispatch.status === 'EnRoute' ? <Navigation size={14} /> : <CheckCircle size={14} />}
                {dispatch.status}
              </span>
            </div>
            <div className="card-body">
              <p><Clock size={14} /> {new Date(dispatch.assignedAt).toLocaleTimeString()}</p>
              <p><MapPin size={14} /> Lat: {dispatch.location.lat.toFixed(3)}, Lng: {dispatch.location.lng.toFixed(3)}</p>
            </div>
          </div>
        ))}
      </div>

      <div className="section-header mt-4">
        <h2>Available Nurses</h2>
      </div>
      
      <div className="available-nurses">
        {availableNurses.map(nurse => (
          <div key={nurse.id} className="nurse-item">
            <div className="nurse-info">
              <span className="availability-dot available"></span>
              <span>{nurse.name}</span>
            </div>
            <button className="assign-btn">Assign</button>
          </div>
        ))}
      </div>
    </div>
  );
};
