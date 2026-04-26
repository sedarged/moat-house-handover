import { hostRequest } from '../core/hostBridge.js';

export const diagnosticsService = {
  async run(userName = '') {
    return hostRequest('diagnostics.run', { userName });
  },
  async openLogsFolder() {
    return hostRequest('shell.openLogsFolder');
  }
};
