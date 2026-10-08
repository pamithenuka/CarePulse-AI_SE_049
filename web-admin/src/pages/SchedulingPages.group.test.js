import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import SlotsPage from './SlotsPage';
import ConsultationsPage from './ConsultationsPage';
import { api } from '../api/client';

// GROUP tests (not an individual student's contribution): component tests for the scheduling pages.

jest.mock('../api/client', () => ({
  api: {
    getSlots: jest.fn(),
    bookAppointment: jest.fn(),
    getBooked: jest.fn(),
    completeConsultation: jest.fn(),
  },
}));
jest.mock('../components/DoctorPicker', () => () => null);
jest.mock('../components/DoctorContext', () => ({
  useDoctor: () => ({ selectedDoctor: { fullName: 'Dr. Test' }, selectedDoctorId: 'doc-1' }),
}));

const slot = (id, hour) => ({
  slotId: id, doctorId: 'doc-1', doctorName: 'Dr. Test', specialty: 'CARDIOLOGY',
  slotStart: `2026-10-20T0${hour}:00:00Z`, slotEnd: `2026-10-20T0${hour}:30:00Z`,
});

describe('SlotsPage', () => {
  beforeEach(() => jest.clearAllMocks());

  test('lists the open slots returned by the API', async () => {
    api.getSlots.mockResolvedValueOnce([slot('s1', 3), slot('s2', 4)]);
    render(<SlotsPage />);

    expect(await screen.findAllByRole('button', { name: /book appointment/i })).toHaveLength(2);
    expect(api.getSlots).toHaveBeenCalledWith({ date: undefined, doctorId: 'doc-1' });
  });

  test('shows an empty state with the doctor name when there are no slots', async () => {
    api.getSlots.mockResolvedValueOnce([]);
    render(<SlotsPage />);

    expect(await screen.findByText(/no slots found for dr\. test/i)).toBeInTheDocument();
  });

  test('shows an error message when loading slots fails', async () => {
    api.getSlots.mockRejectedValueOnce(new Error('Network down'));
    render(<SlotsPage />);

    expect(await screen.findByText('Network down')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /book appointment/i })).not.toBeInTheDocument();
  });

  test('booking is disabled until a patient profile ID is entered', async () => {
    api.getSlots.mockResolvedValueOnce([slot('s1', 3)]);
    render(<SlotsPage />);
    const book = await screen.findByRole('button', { name: /book appointment/i });
    expect(book).toBeDisabled();

    fireEvent.change(screen.getByPlaceholderText(/patient profile id/i), { target: { value: 'patient-1' } });

    expect(book).toBeEnabled();
  });

  test('booking sends the slot and patient, shows success, and reloads the slots', async () => {
    api.getSlots.mockResolvedValue([slot('s1', 3)]);
    api.bookAppointment.mockResolvedValueOnce({});
    render(<SlotsPage />);
    fireEvent.change(await screen.findByPlaceholderText(/patient profile id/i), { target: { value: ' patient-1 ' } });

    fireEvent.click(await screen.findByRole('button', { name: /book appointment/i }));

    expect(await screen.findByText('Appointment booked.')).toBeInTheDocument();
    expect(api.bookAppointment).toHaveBeenCalledWith({ slotId: 's1', patientId: 'patient-1' });
    await waitFor(() => expect(api.getSlots).toHaveBeenCalledTimes(2));
  });

  test('a booking conflict is shown as an error and keeps the user on the page', async () => {
    api.getSlots.mockResolvedValue([slot('s1', 3)]);
    api.bookAppointment.mockRejectedValueOnce(new Error('This slot is no longer available.'));
    render(<SlotsPage />);
    fireEvent.change(await screen.findByPlaceholderText(/patient profile id/i), { target: { value: 'patient-1' } });

    fireEvent.click(await screen.findByRole('button', { name: /book appointment/i }));

    expect(await screen.findByText('This slot is no longer available.')).toBeInTheDocument();
    expect(screen.queryByText('Appointment booked.')).not.toBeInTheDocument();
  });
});

describe('ConsultationsPage', () => {
  const booking = { slotId: 's1', patientId: 'p1', patientName: 'Nimal Perera', doctorId: 'doc-1', slotStart: '2026-10-08T03:00:00Z' };
  beforeEach(() => jest.clearAllMocks());

  test('shows the empty message when there are no pending consultations', async () => {
    api.getBooked.mockResolvedValueOnce([]);
    render(<ConsultationsPage />);

    expect(await screen.findByText(/no pending consultations for this doctor/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /complete consultation/i })).toBeDisabled();
  });

  test('shows an error when the booked list cannot be loaded', async () => {
    api.getBooked.mockRejectedValueOnce(new Error('Server unavailable'));
    render(<ConsultationsPage />);

    expect(await screen.findByRole('status')).toHaveTextContent('Server unavailable');
  });

  test('saving sends the notes and prescription, removes the appointment and confirms', async () => {
    api.getBooked.mockResolvedValueOnce([booking]);
    api.completeConsultation.mockResolvedValueOnce({});
    const { container } = render(<ConsultationsPage />);
    expect(await screen.findByRole('option', { name: /nimal perera/i })).toBeInTheDocument();

    fireEvent.change(container.querySelector('select'), { target: { value: 's1' } });
    const [notes, prescription] = container.querySelectorAll('textarea');
    fireEvent.change(notes, { target: { value: 'Recovering well.' } });
    fireEvent.change(prescription, { target: { value: 'Rest' } });
    fireEvent.click(screen.getByRole('button', { name: /complete consultation/i }));

    expect(await screen.findByRole('status')).toHaveTextContent('Consultation saved.');
    expect(api.completeConsultation).toHaveBeenCalledWith({
      slotId: 's1', patientId: 'p1', doctorId: 'doc-1', notes: 'Recovering well.', prescription: 'Rest',
    });
    expect(screen.queryByRole('option', { name: /nimal perera/i })).not.toBeInTheDocument();
  });

  test('an empty prescription is sent as null, and a server error is displayed', async () => {
    api.getBooked.mockResolvedValueOnce([booking]);
    api.completeConsultation.mockRejectedValueOnce(new Error('The appointment has not started yet.'));
    const { container } = render(<ConsultationsPage />);
    await screen.findByRole('option', { name: /nimal perera/i });

    fireEvent.change(container.querySelector('select'), { target: { value: 's1' } });
    fireEvent.change(container.querySelectorAll('textarea')[0], { target: { value: 'Notes' } });
    fireEvent.click(screen.getByRole('button', { name: /complete consultation/i }));

    expect(await screen.findByRole('status')).toHaveTextContent('The appointment has not started yet.');
    expect(api.completeConsultation).toHaveBeenCalledWith(expect.objectContaining({ prescription: null }));
  });

  test('the notes box enforces the 4000-character limit and prescription 2000', async () => {
    api.getBooked.mockResolvedValueOnce([]);
    const { container } = render(<ConsultationsPage />);
    await screen.findByText(/no pending consultations/i);

    const [notes, prescription] = container.querySelectorAll('textarea');

    expect(notes).toHaveAttribute('maxlength', '4000');
    expect(prescription).toHaveAttribute('maxlength', '2000');
  });
});
