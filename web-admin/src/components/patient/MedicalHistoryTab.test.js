import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import MedicalHistoryTab from './MedicalHistoryTab';
import { updateMedicalHistory } from '../../api/patientsApi';

jest.mock('../../api/patientsApi');
jest.mock('../../context/AuthContext', () => ({ useAuth: () => ({ hasRole: (...roles) => roles.includes('Admin') }) }));
const entry = { id: 'history-a', conditionName: 'Demo condition A', diagnosedOn: '2026-09-29', notes: 'First test entry', isChronic: false, isResolved: false, resolvedOn: null, currentMedications: null };
beforeEach(() => jest.clearAllMocks());

test('editing an unresolved entry sends a null resolution date and displays saved notes', async () => {
  const onChanged = jest.fn();
  updateMedicalHistory.mockResolvedValue({ ...entry, notes: 'Updated test entry' });
  render(<MedicalHistoryTab patientId="patient-one" initialHistory={[entry]} onChanged={onChanged} />);
  fireEvent.click(screen.getByRole('button', { name: 'Edit' }));
  fireEvent.change(screen.getByDisplayValue('First test entry'), { target: { value: 'Updated test entry' } });
  fireEvent.click(screen.getByRole('button', { name: 'Save' }));
  await waitFor(() => expect(updateMedicalHistory).toHaveBeenCalledWith('patient-one', 'history-a', expect.objectContaining({ notes: 'Updated test entry', resolvedOn: null, isResolved: false })));
  await waitFor(() => expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument());
  expect(screen.getByText('Updated test entry')).toBeInTheDocument();
  expect(onChanged).toHaveBeenCalledTimes(1);
});

test('a normalized API validation error is shown and the edit stays open', async () => {
  updateMedicalHistory.mockRejectedValue(new Error('The diagnosis date is invalid.'));
  render(<MedicalHistoryTab patientId="patient-one" initialHistory={[entry]} onChanged={jest.fn()} />);
  fireEvent.click(screen.getByRole('button', { name: 'Edit' }));
  fireEvent.click(screen.getByRole('button', { name: 'Save' }));
  expect(await screen.findByText('The diagnosis date is invalid.')).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();
});
