import { hostRequest } from '../core/hostBridge.js';

export const auditService = {
  async listRecent(limit = 25) {
    return hostRequest('audit.listRecent', { limit });
  },

  async listForSession(sessionId, limit = 25) {
    return hostRequest('audit.listForSession', { sessionId, limit });
  }
};
