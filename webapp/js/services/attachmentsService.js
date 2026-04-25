import { hostRequest } from '../core/hostBridge.js';

export const attachmentsService = {
  async pickFile() {
    return hostRequest('file.pickFile', {
      title: 'Choose attachment file',
      filter: 'Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.tif;*.tiff|All files|*.*'
    });
  },

  async listAttachments(sessionId, deptRecordId, deptName) {
    return hostRequest('attachment.list', { sessionId, deptRecordId, deptName });
  },

  async addAttachment(sessionId, deptRecordId, deptName, sourceFilePath, displayName, userName) {
    return hostRequest('attachment.add', {
      sessionId,
      deptRecordId,
      deptName,
      sourceFilePath,
      displayName,
      userName
    });
  },

  async removeAttachment(attachmentId, userName) {
    return hostRequest('attachment.remove', { attachmentId, userName });
  },

  async openViewer(sessionId, deptRecordId, attachmentId) {
    return hostRequest('attachment.openViewer', { sessionId, deptRecordId, attachmentId });
  }
};
