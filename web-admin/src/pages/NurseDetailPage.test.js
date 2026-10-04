import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import NurseDetailPage from './NurseDetailPage';
import { getNurse, setNurseAvailability } from '../api/staffApi';

jest.mock('../api/staffApi');
jest.mock('../context/AuthContext', () => ({ useAuth: () => ({ hasRole: role => role === 'Admin' }) }));
const nurse = { id: 'n1', fullName: 'Demo nurse', specialization: 'Emergency', status: 'Active', isAvailable: false, licenseNumber: 'TEST-1' };
function show() {
  render(<MemoryRouter initialEntries={['/nurses/n1']}><Routes><Route path="/nurses/:id" element={<NurseDetailPage />} /></Routes></MemoryRouter>);
}
beforeEach(() => jest.resetAllMocks());

test('admin can mark a new nurse available and sees persisted state', async () => {
  getNurse.mockResolvedValueOnce(nurse).mockResolvedValueOnce({ ...nurse, isAvailable: true });
  setNurseAvailability.mockResolvedValue({ id: 'n1', isAvailable: true });
  show();
  fireEvent.click(await screen.findByRole('button', { name: 'Mark available' }));
  expect(await screen.findByRole('button', { name: 'Mark unavailable' })).toBeInTheDocument();
  expect(setNurseAvailability).toHaveBeenCalledWith('n1', true);
});

test('active dispatch conflict is shown without pretending availability changed', async () => {
  getNurse.mockResolvedValue(nurse);
  setNurseAvailability.mockRejectedValue({ response: { data: { message: 'This nurse has an active dispatch.' } } });
  show();
  fireEvent.click(await screen.findByRole('button', { name: 'Mark available' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('This nurse has an active dispatch.');
  await waitFor(() => expect(screen.getByRole('button', { name: 'Mark available' })).toBeEnabled());
});
