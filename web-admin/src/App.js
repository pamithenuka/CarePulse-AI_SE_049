import React from 'react';
import './App.css';
import TriageDashboard from './components/TriageDashboard';

function App() {
  return (
    <div className="App">
      <header className="app-header glass-panel">
        <div className="logo-container">
          <div className="pulse-dot"></div>
          <h1>CarePulse</h1>
        </div>
        <div className="user-profile">
          <span className="doctor-name">Dr. Sarah Jenkins</span>
          <div className="avatar">SJ</div>
        </div>
      </header>
      
      <main className="main-content">
        <TriageDashboard />
      </main>
    </div>
  );
}

export default App;
