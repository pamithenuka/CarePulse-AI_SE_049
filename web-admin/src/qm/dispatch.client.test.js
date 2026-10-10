/** @jest-environment node */
import fs from 'fs';
import axios from 'axios';
import client from '../api/apiClient';
import { triageApi } from '../services/triageApi';
import { useDispatchStore } from '../store/useDispatchStore';

const enabled = !!process.env.CAREPULSE_CLIENT_SESSION;
(enabled ? test : test.skip)('QM real API: React services approve, assign and observe completion', async () => {
  const session = JSON.parse(fs.readFileSync(process.env.CAREPULSE_CLIENT_SESSION, 'utf8'));
  // Only browser storage is substituted; the production API interceptor and store make real HTTP requests.
  global.localStorage = { getItem: () => session.doctorToken, removeItem: () => {} };
  client.defaults.adapter = axios.getAdapter('http');
  client.defaults.baseURL = 'http://127.0.0.1:5516/api/v1';
  if (process.env.QM_PHASE === 'doctor') {
    const pending = await triageApi.getPendingApprovals();
    expect(pending.some(t => t.id === session.triageId)).toBe(true);
    await triageApi.approveTriage(session.triageId, 'Synthetic client integration doctor review');
    await useDispatchStore.getState().fetchDispatchData();
    expect(useDispatchStore.getState().error).toBeNull();
    expect(useDispatchStore.getState().waitingCases.some(t => t.id === session.triageId)).toBe(true);
    const { data } = await client.post('/dispatch/assign', { triageTicketId: session.triageId,
      nurseId: session.nurseId, acknowledgeSafetyFlags: true });
    session.dispatchId = data.id;
    expect(session.dispatchId).toBeTruthy();
    await useDispatchStore.getState().fetchDispatchData();
    expect(useDispatchStore.getState().activeDispatches.some(d => d.id === data.id)).toBe(true);
    fs.writeFileSync(process.env.CAREPULSE_CLIENT_SESSION, JSON.stringify(session), { mode: 0o600 });
  } else {
    expect(process.env.QM_PHASE).toBe('verify');
    await useDispatchStore.getState().fetchDispatchData();
    const state = useDispatchStore.getState();
    expect(state.error).toBeNull();
    expect(state.activeDispatches.some(d => d.id === session.dispatchId)).toBe(false);
    expect(state.availableNurses.some(n => n.id === session.nurseId)).toBe(true);
    expect((await triageApi.getStatus(session.triageId)).status).toBe('VISIT_COMPLETED');
  }
}, 120000);
