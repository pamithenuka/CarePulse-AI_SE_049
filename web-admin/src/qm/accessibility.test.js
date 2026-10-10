import React from 'react';
import fs from 'fs';
import path from 'path';
import axe from 'axe-core';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import LoginPage from '../pages/LoginPage';
import RegisterPatientPage from '../pages/RegisterPatientPage';
import ConsultationsPage from '../pages/ConsultationsPage';
import { NurseList } from '../components/dispatch/NurseList';
import { useAuth } from '../context/AuthContext';
import { useDoctor } from '../components/DoctorContext';
import { useDispatchStore } from '../store/useDispatchStore';
import { api } from '../api/client';

jest.mock('../context/AuthContext');
jest.mock('../components/DoctorContext');
jest.mock('../store/useDispatchStore');
jest.mock('../api/client', () => ({ api: { getBooked: jest.fn() } }));

async function inspect(container) {
  const result = await axe.run(container, { rules: { 'color-contrast': { enabled: false } } });
  if (process.env.QM_AXE_EVIDENCE) {
    const name = expect.getState().currentTestName.replace(/[^a-z0-9]+/gi, '-');
    fs.writeFileSync(path.join(process.env.QM_AXE_EVIDENCE, name + '.json'), JSON.stringify({
      engine: result.testEngine, environment: result.testEnvironment,
      timestamp: result.timestamp, violations: result.violations,
      incomplete: result.incomplete, passedRuleIds: result.passes.map(r => r.id),
      scope: 'Rendered component in jsdom; color contrast disabled; manual review required for incomplete checks'
    }, null, 2));
  }
  expect(result.violations.map(v => ({ id: v.id, impact: v.impact,
    targets: v.nodes.map(n => n.target) }))).toEqual([]);
}
beforeEach(() => {
  jest.clearAllMocks();
  useAuth.mockReturnValue({ login: jest.fn().mockRejectedValue(new Error('Synthetic login failure')) });
  useDoctor.mockReturnValue({ doctors: [{ id: 'd1', fullName: 'Synthetic Doctor', specialty: 'CARDIOLOGY' }],
    selectedDoctorId: 'd1', setSelectedDoctorId: jest.fn(), loading: false });
  api.getBooked.mockResolvedValue([{ slotId: 's1', patientId: 'p1', doctorId: 'd1',
    patientName: 'Synthetic Patient', slotStart: '2026-10-04T04:00:00Z' }]);
  useDispatchStore.mockReturnValue({ activeDispatches: [], availableNurses: [{ id: 'n1', name: 'Synthetic Nurse' }],
    waitingCases: [{ id: 't1', patientName: 'Synthetic Patient', riskScore: 9, latitude: 6.9271, longitude: 79.8612 }],
    fetchDispatchData: jest.fn(), isLoading: false, error: null });
});
test('login form and announced error have accessible controls', async () => {
  const { container } = render(<MemoryRouter><LoginPage /></MemoryRouter>);
  screen.getByLabelText('Email'); screen.getByLabelText('Password');
  fireEvent.submit(screen.getByRole('button', { name: 'Sign in' }).closest('form'));
  await screen.findByRole('alert');
  await inspect(container);
});
test('patient registration form has accessible controls', async () => {
  const { container } = render(<MemoryRouter><RegisterPatientPage /></MemoryRouter>);
  await inspect(container);
});
test('consultation form exposes booking and notes labels', async () => {
  const { container } = render(<ConsultationsPage />);
  await screen.findByRole('option', { name: /Synthetic Patient/ });
  screen.getByLabelText(/Booked appointment/); screen.getByLabelText('Notes');
  await inspect(container);
});
test('dispatch assignment form has accessible nurse and coordinate inputs', async () => {
  const { container } = render(<NurseList />);
  screen.getByLabelText('Nurse'); screen.getByLabelText('Confirmed latitude');
  await inspect(container);
});
