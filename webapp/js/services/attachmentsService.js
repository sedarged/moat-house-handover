/** Attachment service stub: list/add/remove and viewer payloads. */
export const attachmentsService = {
  async listAttachments(deptRecordId) {
    void deptRecordId;
    throw new Error('attachmentsService.listAttachments is not implemented (Stage 1 stub).');
  },
  async addAttachment(deptRecordId, sourceFilePath, userName) {
    void deptRecordId;
    void sourceFilePath;
    void userName;
    throw new Error('attachmentsService.addAttachment is not implemented (Stage 1 stub).');
  },
  async removeAttachment(attachmentId, userName) {
    void attachmentId;
    void userName;
    throw new Error('attachmentsService.removeAttachment is not implemented (Stage 1 stub).');
  }
};
