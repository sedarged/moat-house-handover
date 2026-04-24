import { createInitialSessionState } from '../models/contracts.js';

/**
 * Global client state for Stage 1.
 * Maintains route and a typed session container that Stage 2+ services will populate.
 */
export const appState = {
  currentRoute: 'shift',
  session: createInitialSessionState()
};
