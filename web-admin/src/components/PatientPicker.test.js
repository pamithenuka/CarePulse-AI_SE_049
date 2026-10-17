import { useState } from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import PatientPicker from './PatientPicker';
import * as patientsApi from '../api/patientsApi';

jest.mock('../api/patientsApi');

const nimal = { id: 'p1', fullName: 'Nimal Perera', nationalId: '199012345V', age: 35, gender: 'Male' };
const sunil = { id: 'p2', fullName: 'Sunil Silva', nationalId: '200012345678', age: 25, gender: 'Male' };

// A small host so the picker's controlled value behaves as it does on a real page.
function Host({ onPick = () => {} }) {
  const [patient, setPatient] = useState(null);
  return (
    <PatientPicker
      debounceMs={0}
      value={patient}
      onChange={(p) => {
        setPatient(p);
        onPick(p);
      }}
    />
  );
}

describe('PatientPicker', () => {
  beforeEach(() => jest.clearAllMocks());

  test('lists patients when the box is focused and searches as the user types', async () => {
    patientsApi.getPatients.mockResolvedValue({ items: [nimal, sunil] });
    render(<Host />);

    fireEvent.focus(screen.getByRole('combobox'));
    expect(await screen.findByText('Nimal Perera')).toBeInTheDocument();
    expect(screen.getByText('Sunil Silva')).toBeInTheDocument();

    fireEvent.change(screen.getByRole('combobox'), { target: { value: 'nim' } });
    await waitFor(() => expect(patientsApi.getPatients).toHaveBeenLastCalledWith({ search: 'nim', pageSize: 8 }));
  });

  test('choosing a patient reports it and shows a chip with a Change button', async () => {
    patientsApi.getPatients.mockResolvedValue({ items: [nimal] });
    const onPick = jest.fn();
    render(<Host onPick={onPick} />);

    fireEvent.focus(screen.getByRole('combobox'));
    fireEvent.mouseDown(await screen.findByRole('option', { name: /nimal perera/i }));

    expect(onPick).toHaveBeenCalledWith(nimal);
    expect(screen.getByText('Nimal Perera')).toBeInTheDocument();
    expect(screen.getByText(/199012345V/)).toBeInTheDocument();
    expect(screen.queryByRole('combobox')).not.toBeInTheDocument();
  });

  test('Change clears the choice and brings the search box back', async () => {
    patientsApi.getPatients.mockResolvedValue({ items: [nimal] });
    const onPick = jest.fn();
    render(<Host onPick={onPick} />);
    fireEvent.focus(screen.getByRole('combobox'));
    fireEvent.mouseDown(await screen.findByRole('option', { name: /nimal perera/i }));

    fireEvent.click(screen.getByRole('button', { name: /change/i }));

    expect(onPick).toHaveBeenLastCalledWith(null);
    expect(screen.getByRole('combobox')).toBeInTheDocument();
  });

  test('keyboard: arrow down then Enter picks the highlighted patient; Escape closes the list', async () => {
    patientsApi.getPatients.mockResolvedValue({ items: [nimal, sunil] });
    const onPick = jest.fn();
    render(<Host onPick={onPick} />);
    const box = screen.getByRole('combobox');
    fireEvent.focus(box);
    await screen.findByText('Sunil Silva');

    fireEvent.keyDown(box, { key: 'ArrowDown' });
    fireEvent.keyDown(box, { key: 'ArrowDown' });
    fireEvent.keyDown(box, { key: 'Enter' });

    expect(onPick).toHaveBeenCalledWith(sunil);
  });

  test('Escape closes the list without choosing anyone', async () => {
    patientsApi.getPatients.mockResolvedValue({ items: [nimal] });
    const onPick = jest.fn();
    render(<Host onPick={onPick} />);
    const box = screen.getByRole('combobox');
    fireEvent.focus(box);
    await screen.findByText('Nimal Perera');

    fireEvent.keyDown(box, { key: 'Escape' });

    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    expect(onPick).not.toHaveBeenCalled();
  });

  test('shows a message when nobody matches', async () => {
    patientsApi.getPatients.mockResolvedValue({ items: [] });
    render(<Host />);

    fireEvent.focus(screen.getByRole('combobox'));

    expect(await screen.findByText('No patients found.')).toBeInTheDocument();
  });

  test('shows an error message when the search fails', async () => {
    patientsApi.getPatients.mockRejectedValue(new Error('Network down'));
    render(<Host />);

    fireEvent.focus(screen.getByRole('combobox'));

    expect(await screen.findByText(/could not load patients/i)).toBeInTheDocument();
  });
});
