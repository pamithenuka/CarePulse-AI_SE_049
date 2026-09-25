import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import OverviewTab from './OverviewTab';
import * as patientsApi from '../../api/patientsApi';

jest.mock('../../api/patientsApi');

const profile = {
  id: 'p1',
  fullName: 'Jane Perera',
  dateOfBirth: '1990-05-01',
  gender: 'Female',
  bloodType: 'O+',
  phoneNumber: '0771234567',
  address: '1 Main St',
  nationalId: '199012345V',
  allergies: 'Penicillin',
  lastEmergencyBroadcastAt: null,
};

describe('OverviewTab', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('non-admins do not see an Edit button', () => {
    render(<OverviewTab profile={profile} isAdmin={false} onUpdated={jest.fn()} />);
    expect(screen.queryByRole('button', { name: /edit/i })).not.toBeInTheDocument();
  });

  test('rejects an invalid National ID before saving', async () => {
    render(<OverviewTab profile={profile} isAdmin={true} onUpdated={jest.fn()} />);

    fireEvent.click(screen.getByRole('button', { name: /edit/i }));
    fireEvent.change(screen.getByDisplayValue('199012345V'), { target: { value: 'not-a-nic' } });
    fireEvent.click(screen.getByRole('button', { name: /save changes/i }));

    expect(await screen.findByText(/enter 9 digits \+ v\/x, or 12 digits/i)).toBeInTheDocument();
    expect(patientsApi.updatePatientProfile).not.toHaveBeenCalled();
  });

  test('saves valid changes and calls onUpdated', async () => {
    patientsApi.updatePatientProfile.mockResolvedValueOnce({ ...profile, phoneNumber: '0709999999' });
    const onUpdated = jest.fn();

    render(<OverviewTab profile={profile} isAdmin={true} onUpdated={onUpdated} />);

    fireEvent.click(screen.getByRole('button', { name: /edit/i }));
    fireEvent.change(screen.getByDisplayValue('0771234567'), { target: { value: '0709999999' } });
    fireEvent.click(screen.getByRole('button', { name: /save changes/i }));

    await waitFor(() => expect(patientsApi.updatePatientProfile).toHaveBeenCalledWith('p1', expect.objectContaining({ phoneNumber: '0709999999' })));
    await waitFor(() => expect(onUpdated).toHaveBeenCalled());
  });
});
