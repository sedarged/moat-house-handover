import { createInitialSessionState } from '../models/contracts.js';

export const appState = {
  currentRoute: 'home',
  selectedShift: null,
  runtimeStatus: null,
  runtimeStatusError: null,
  session: createInitialSessionState()
};

export function setSelectedShift(shiftCode) {
  appState.selectedShift = shiftCode || null;
}

export function setRuntimeStatus(status) {
  appState.runtimeStatus = status || null;
  appState.runtimeStatusError = null;
}

export function setRuntimeStatusError(message) {
  appState.runtimeStatusError = message || 'Status unavailable';
}
