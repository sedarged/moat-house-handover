import { hostRequest } from '../core/hostBridge.js';

export const sessionService = {
  async openSession(shiftCode, shiftDate, userName) {
    return hostRequest('session.open', { shiftCode, shiftDate, userName });
  },

  async createBlankSession(shiftCode, shiftDate, userName) {
    return hostRequest('session.createBlank', { shiftCode, shiftDate, userName });
  },

  async clearDay(sessionId, userName) {
    return hostRequest('session.clearDay', { sessionId, userName });
  }
};
