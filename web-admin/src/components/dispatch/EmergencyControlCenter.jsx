import React from 'react';
import { DispatchMap } from './DispatchMap';
import { NurseList } from './NurseList';
import './EmergencyControlCenter.css';
import { Activity } from 'lucide-react';

export const EmergencyControlCenter = () => {
  return (
    <div className="ecc-dashboard">
      <header className="ecc-header">
        <div className="header-brand">
          <div className="icon-wrapper">
            <Activity className="pulse-icon" size={28} />
          </div>
          <h1>Emergency Control Center <span>Live Dispatch</span></h1>
        </div>
        <div className="header-status">
          <span className="live-badge">LIVE</span>
        </div>
      </header>
      
      <div className="ecc-content">
        <aside className="ecc-sidebar">
          <NurseList />
        </aside>
        
        <main className="ecc-map-area">
          <DispatchMap />
        </main>
      </div>
    </div>
  );
};
