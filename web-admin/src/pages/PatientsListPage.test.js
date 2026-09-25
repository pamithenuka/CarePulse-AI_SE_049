import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import PatientsListPage from './PatientsListPage';
import { AuthProvider } from '../context/AuthContext';
import * as patientsApi from '../api/patientsApi';

jest.mock('../api/patientsApi');

const renderPage = () =>
  render(
    <MemoryRouter>
      <AuthProvider>
        <PatientsListPage />
      </AuthProvider>
    </MemoryRouter>
  );

describe('PatientsListPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
  });

  test('shows an empty state when there are no patients', async () => {
    patientsApi.getPatients.mockResolvedValueOnce({
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
    });

    renderPage();

    expect(await screen.findByText(/no patients match the current filters/i)).toBeInTheDocument();
  });

  test('shows an error state when the request fails', async () => {
    patientsApi.getPatients.mockRejectedValueOnce({
      response: { data: { message: 'Failed to load patients.' } },
    });

    renderPage();

    expect(await screen.findByRole('alert')).toHaveTextContent('Failed to load patients.');
  });

  test('renders patient rows returned by the API', async () => {
    patientsApi.getPatients.mockResolvedValueOnce({
      items: [
        {
          id: 'p1',
          fullName: 'Jane Perera',
          dateOfBirth: '1990-05-01',
          age: 36,
          gender: 'Female',
          bloodType: 'O+',
          phoneNumber: '0771234567',
          nationalId: '199012345V',
          mainConditions: null,
          status: 'Active',
          createdAt: new Date().toISOString(),
        },
      ],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1,
    });

    renderPage();

    expect(await screen.findByText('Jane Perera')).toBeInTheDocument();
  });
});
