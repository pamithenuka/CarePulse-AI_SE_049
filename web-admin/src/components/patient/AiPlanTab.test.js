import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import AiPlanTab from './AiPlanTab';
import { AuthProvider } from '../../context/AuthContext';
import * as aiApi from '../../api/aiApi';

jest.mock('../../api/aiApi');

const renderAsRole = (roles) => {
  localStorage.setItem('carepulse_token', 'test-token');
  localStorage.setItem('carepulse_user', JSON.stringify({ userId: 'u1', fullName: 'Dr. Test', email: 'doc@carepulse.dev', roles }));
  return render(
    <AuthProvider>
      <AiPlanTab patientId="p1" />
    </AuthProvider>
  );
};

const createdPlan = {
  id: 'w1',
  patientProfileId: 'p1',
  objective: 'Patient reports chest pain',
  summary: '58-year-old, reports chest pain',
  steps: [
    { agent: 'DomainAnalysis', task: 'Score cardiac risk', status: 'Pending' },
    { agent: 'ActionTool', task: 'Find nearest cardiologist', status: 'Pending' },
  ],
  status: 'PlanCreated',
  errorMessage: null,
  modelUsed: 'llama3.2',
  toolCallSummary: 'GetPatientContextAsync(p1) -> age=58',
  reviewStatus: 'NotReviewed',
  reviewedByName: null,
  reviewNotes: null,
  createdAt: new Date().toISOString(),
};

describe('AiPlanTab', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
  });

  test('shows an empty state when no plans exist yet', async () => {
    aiApi.getAiPlans.mockResolvedValueOnce([]);
    renderAsRole(['Doctor']);

    expect(await screen.findByText(/no ai plans have been generated/i)).toBeInTheDocument();
  });

  test('shows an error state when loading fails', async () => {
    aiApi.getAiPlans.mockRejectedValueOnce({ response: { data: { message: 'Failed to load AI plans.' } } });
    renderAsRole(['Doctor']);

    expect(await screen.findByRole('alert')).toHaveTextContent('Failed to load AI plans.');
  });

  test('rejects generating a plan with too short an objective', async () => {
    aiApi.getAiPlans.mockResolvedValueOnce([]);
    renderAsRole(['Doctor']);
    await screen.findByText(/no ai plans have been generated/i);

    fireEvent.change(screen.getByLabelText(/patient-reported symptoms/i), { target: { value: 'hi' } });
    fireEvent.click(screen.getByRole('button', { name: /generate ai plan/i }));

    expect(await screen.findByText(/describe the objective in a bit more detail/i)).toBeInTheDocument();
    expect(aiApi.createAiPlan).not.toHaveBeenCalled();
  });

  test('generates a plan and displays its steps', async () => {
    aiApi.getAiPlans.mockResolvedValueOnce([]);
    aiApi.createAiPlan.mockResolvedValueOnce(createdPlan);
    renderAsRole(['Doctor']);
    await screen.findByText(/no ai plans have been generated/i);

    fireEvent.change(screen.getByLabelText(/patient-reported symptoms/i), {
      target: { value: 'Patient reports chest pain' },
    });
    fireEvent.click(screen.getByRole('button', { name: /generate ai plan/i }));

    expect(await screen.findByText(/58-year-old, reports chest pain/i)).toBeInTheDocument();
    expect(screen.getByText(/score cardiac risk/i)).toBeInTheDocument();
  });

  test('lets an Admin approve a plan', async () => {
    aiApi.getAiPlans.mockResolvedValueOnce([createdPlan]);
    aiApi.reviewAiPlan.mockResolvedValueOnce({ ...createdPlan, reviewStatus: 'Approved', reviewedByName: 'Admin User' });
    renderAsRole(['Admin']);

    await screen.findByText(/58-year-old, reports chest pain/i);
    fireEvent.click(screen.getByRole('button', { name: /review this plan/i }));
    fireEvent.click(screen.getByRole('button', { name: /^approve$/i }));

    await waitFor(() => expect(aiApi.reviewAiPlan).toHaveBeenCalledWith('p1', 'w1', expect.objectContaining({ approved: true })));
    expect(await screen.findByText(/approved by admin user/i)).toBeInTheDocument();
  });

  test('does not show review controls to a Doctor', async () => {
    aiApi.getAiPlans.mockResolvedValueOnce([createdPlan]);
    renderAsRole(['Doctor']);

    await screen.findByText(/58-year-old, reports chest pain/i);
    expect(screen.queryByRole('button', { name: /review this plan/i })).not.toBeInTheDocument();
  });
});
