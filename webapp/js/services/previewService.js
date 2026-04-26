import { hostRequest } from '../core/hostBridge.js';

export const previewService = {
  async loadPreview(sessionId) {
    return hostRequest('preview.load', { sessionId });
  }
};
