import { render, screen, fireEvent } from '@testing-library/react';
import NurseVisitsTab from './NurseVisitsTab';
import api from '../../api/apiClient';
jest.mock('../../api/apiClient', () => ({ get: jest.fn() }));
afterEach(() => jest.resetAllMocks());
test('shows saved measurements and completion in the patient record', async () => {
  api.get.mockResolvedValue({ data: [{ id: 'visit', nurseName: 'Nurse One', status: 'Completed', assignedAt: '2026-10-15T10:00:00Z', completedAt: '2026-10-15T11:00:00Z', vitals: [{ id: 'v', recordedAt: '2026-10-15T10:50:00Z', heartRate: 82, bloodPressure: '120/80', bodyTempC: 37, oxygenSaturation: 98, clinicalNotes: 'Follow-up recorded' }] }] });
  render(<NurseVisitsTab patientId="patient-a" />);
  expect((await screen.findByText('Heart rate')).nextElementSibling).toHaveTextContent('82 bpm');
  expect(screen.getByText('Blood pressure').nextElementSibling).toHaveTextContent('120/80 mmHg');
  expect(screen.getByText('Follow-up recorded')).toBeInTheDocument();
  expect(screen.getAllByText('Completed').length).toBeGreaterThan(0);
  expect(api.get).toHaveBeenCalledWith('/dispatch/patients/patient-a/visits');
});
test('can retry a failed history request', async () => {
  api.get.mockRejectedValueOnce(new Error()).mockResolvedValueOnce({ data: [] });
  render(<NurseVisitsTab patientId="patient-a" />);
  expect(await screen.findByRole('alert')).toHaveTextContent('Could not load');
  fireEvent.click(screen.getByText('Refresh visits'));
  expect(await screen.findByText('No nurse visits recorded.')).toBeInTheDocument();
});
test('filters inclusive local assignment dates, validates ranges and clears filters', async () => {
  api.get.mockResolvedValue({ data: [
    { id: 'a', nurseName: 'Earlier nurse', status: 'Completed', assignedAt: '2026-10-14T23:59:00', vitals: [] },
    { id: 'b', nurseName: 'Selected nurse', status: 'Completed', assignedAt: '2026-10-15T23:59:00', vitals: [] },
    { id: 'c', nurseName: 'Later nurse', status: 'Assigned', assignedAt: '2026-10-16T00:00:00', vitals: [] }
  ] });
  render(<NurseVisitsTab patientId="patient-a" />);
  await screen.findByText('Selected nurse');
  fireEvent.change(screen.getByLabelText('From date'), { target: { value: '2026-10-15' } });
  expect(screen.queryByText('Earlier nurse')).not.toBeInTheDocument();
  expect(screen.getByText('Later nurse')).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText('To date'), { target: { value: '2026-10-15' } });
  expect(screen.getByText('Selected nurse')).toBeInTheDocument();
  expect(screen.queryByText('Later nurse')).not.toBeInTheDocument();
  fireEvent.change(screen.getByLabelText('To date'), { target: { value: '2026-10-14' } });
  expect(screen.getByRole('alert')).toHaveTextContent('To date must be');
  fireEvent.click(screen.getByText('Clear filters'));
  expect(screen.getAllByRole('article')).toHaveLength(3);
  expect(screen.getAllByRole('article')[0]).toHaveAccessibleName('Visit by Later nurse');
  fireEvent.change(screen.getByLabelText('To date'), { target: { value: '2026-10-13' } });
  expect(screen.getByText(/No visits match/)).toBeInTheDocument();
});
