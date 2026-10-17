import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import ConsultationHistoryTab from './ConsultationHistoryTab';
import { api } from '../../api/client';

jest.mock('../../api/client', () => ({
  api: { getPatientConsultations: jest.fn() },
}));

const record = (id, doctorName, notes, extra = {}) => ({
  id, slotId: `slot-${id}`, doctorId: 'd1', doctorName, specialty: 'CARDIOLOGY',
  appointmentStart: '2026-10-10T04:00:00Z', appointmentEnd: '2026-10-10T04:30:00Z',
  notes, prescription: null, ...extra,
});

describe('ConsultationHistoryTab', () => {
  beforeEach(() => jest.clearAllMocks());

  test('asks the API for this patient and shows the records', async () => {
    api.getPatientConsultations.mockResolvedValueOnce([
      record('c1', 'Dr. Silva', 'Blood pressure is stable.', { prescription: 'Amlodipine 5 mg' }),
      record('c2', 'Dr. Perera', 'Follow-up in two weeks.'),
    ]);

    render(<ConsultationHistoryTab patientId="p1" />);

    expect(await screen.findByText('Dr. Silva')).toBeInTheDocument();
    expect(screen.getByText('Blood pressure is stable.')).toBeInTheDocument();
    expect(screen.getByText(/Amlodipine 5 mg/)).toBeInTheDocument();
    expect(screen.getByText('Dr. Perera')).toBeInTheDocument();
    expect(api.getPatientConsultations).toHaveBeenCalledWith('p1');
  });

  test('shows a loading message first', () => {
    api.getPatientConsultations.mockReturnValueOnce(new Promise(() => {}));

    render(<ConsultationHistoryTab patientId="p1" />);

    expect(screen.getByRole('status')).toHaveTextContent(/loading consultations/i);
  });

  test('shows an empty state when the patient has no consultations', async () => {
    api.getPatientConsultations.mockResolvedValueOnce([]);

    render(<ConsultationHistoryTab patientId="p1" />);

    expect(await screen.findByText('No consultations recorded yet.')).toBeInTheDocument();
  });

  test('does not show a prescription block when there is none', async () => {
    api.getPatientConsultations.mockResolvedValueOnce([record('c1', 'Dr. Silva', 'Notes only.')]);

    render(<ConsultationHistoryTab patientId="p1" />);

    await screen.findByText('Notes only.');
    expect(screen.queryByText(/prescription/i)).not.toBeInTheDocument();
  });

  test('shows the server error and lets the user retry', async () => {
    api.getPatientConsultations
      .mockRejectedValueOnce({ response: { data: { message: 'You cannot access this patient.' } } })
      .mockResolvedValueOnce([record('c1', 'Dr. Silva', 'Loaded after retry.')]);

    render(<ConsultationHistoryTab patientId="p1" />);
    expect(await screen.findByRole('alert')).toHaveTextContent('You cannot access this patient.');

    fireEvent.click(screen.getByRole('button', { name: /retry/i }));

    expect(await screen.findByText('Loaded after retry.')).toBeInTheDocument();
    await waitFor(() => expect(api.getPatientConsultations).toHaveBeenCalledTimes(2));
  });
});
