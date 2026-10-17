import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import DocumentsTab from './DocumentsTab';
import * as patientsApi from '../../api/patientsApi';

jest.mock('../../api/patientsApi');

const doc = { id: 'd1', fileName: 'scan.pdf', documentType: 'ImagingScan', uploadedAt: '2026-10-10T04:00:00Z', fileUrl: '/x' };

const renderTab = (props = {}) =>
  render(<DocumentsTab patientId="p1" initialDocuments={[doc]} isAdmin={false} onChanged={jest.fn()} {...props} />);

describe('DocumentsTab permissions', () => {
  beforeEach(() => jest.clearAllMocks());

  test('a Doctor (canUpload) sees the upload form but not rename or remove', () => {
    renderTab({ isAdmin: false, canUpload: true });

    expect(screen.getByRole('heading', { name: /upload document/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^upload$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /download/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /rename/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /remove/i })).not.toBeInTheDocument();
  });

  test('an Admin sees upload, rename and remove', () => {
    renderTab({ isAdmin: true, canUpload: true });

    expect(screen.getByRole('button', { name: /^upload$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /rename/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /remove/i })).toBeInTheDocument();
  });

  test('without upload permission the form is hidden', () => {
    renderTab({ isAdmin: false, canUpload: false });

    expect(screen.queryByRole('heading', { name: /upload document/i })).not.toBeInTheDocument();
  });

  test('an older caller that only passes isAdmin still works (Admin gets the form)', () => {
    renderTab({ isAdmin: true });

    expect(screen.getByRole('button', { name: /^upload$/i })).toBeInTheDocument();
  });

  test('uploading as a Doctor sends the file and type, and adds it to the list', async () => {
    const uploaded = { id: 'd2', fileName: 'new-lab.pdf', documentType: 'LabReport', uploadedAt: '2026-10-11T04:00:00Z', fileUrl: '/y' };
    patientsApi.uploadDocument.mockResolvedValueOnce(uploaded);
    const onChanged = jest.fn();
    const { container } = renderTab({ canUpload: true, onChanged });
    const file = new File(['%PDF-1.4 test'], 'new-lab.pdf', { type: 'application/pdf' });

    fireEvent.change(container.querySelector('input[type="file"]'), { target: { files: [file] } });
    fireEvent.change(container.querySelector('select'), { target: { value: 'LabReport' } });
    fireEvent.click(screen.getByRole('button', { name: /^upload$/i }));

    await waitFor(() => expect(patientsApi.uploadDocument).toHaveBeenCalledWith('p1', file, 'LabReport'));
    expect(await screen.findByText('new-lab.pdf')).toBeInTheDocument();
    expect(onChanged).toHaveBeenCalled();
  });

  test('uploading with no file chosen shows a message and calls nothing', () => {
    renderTab({ canUpload: true });

    fireEvent.click(screen.getByRole('button', { name: /^upload$/i }));

    expect(screen.getByText(/choose a file to upload/i)).toBeInTheDocument();
    expect(patientsApi.uploadDocument).not.toHaveBeenCalled();
  });
});
