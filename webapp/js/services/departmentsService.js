/** Department service stub: load/save/validate department payloads. */
export const departmentsService = {
  async loadDepartment(sessionId, deptName) {
    void sessionId;
    void deptName;
    throw new Error('departmentsService.loadDepartment is not implemented (Stage 1 stub).');
  },
  async saveDepartment(deptPayload) {
    void deptPayload;
    throw new Error('departmentsService.saveDepartment is not implemented (Stage 1 stub).');
  },
  validateDepartment(deptPayload) {
    void deptPayload;
    return { ok: true, errors: [] };
  }
};
