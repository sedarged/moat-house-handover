import { hostRequest } from '../core/hostBridge.js';

export const sendService = {
  async loadEmailProfileForShift(shiftCode) {
    return hostRequest('emailProfile.loadForShift', { shiftCode });
  },

  async preparePackage(sessionId, userName = '') {
    return hostRequest('send.preparePackage', { sessionId, userName });
  },

  async createOutlookDraft(sessionId, userName = '') {
    return hostRequest('send.createOutlookDraft', { sessionId, userName });
  }
};
