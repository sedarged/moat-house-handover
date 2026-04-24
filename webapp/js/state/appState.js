import { createInitialSessionState } from '../models/contracts.js';

export const appState = {
  currentRoute: 'shift',
  session: createInitialSessionState()
};

export function applySessionPayload(sessionPayload) {
  appState.session = {
    ...createInitialSessionState(),
    sessionId: sessionPayload.sessionId,
    shiftCode: sessionPayload.shiftCode,
    shiftDate: sessionPayload.shiftDate,
    sessionStatus: sessionPayload.sessionStatus,
    departments: sessionPayload.departments || [],
    userName: sessionPayload.updatedBy || sessionPayload.createdBy || null,
    createdAt: sessionPayload.createdAt,
    createdBy: sessionPayload.createdBy,
    updatedAt: sessionPayload.updatedAt,
    updatedBy: sessionPayload.updatedBy
  };
}
