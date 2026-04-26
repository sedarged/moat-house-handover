import { hostRequest } from '../core/hostBridge.js';

export const reportsService = {
  async generateHandoverReport(sessionId, userName = '') {
    return hostRequest('reports.generateHandover', { sessionId, userName });
  },

  async generateBudgetReport(sessionId, userName = '') {
    return hostRequest('reports.generateBudget', { sessionId, userName });
  },

  async generateAllReports(sessionId, userName = '') {
    return hostRequest('reports.generateAll', { sessionId, userName });
  },

  async openReportsFolder(sessionId = null) {
    return hostRequest('shell.openReportsFolder', { sessionId });
  }
};
