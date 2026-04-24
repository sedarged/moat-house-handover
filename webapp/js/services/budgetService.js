/** Budget service stub: load/save rows and variance calculations. */
export const budgetService = {
  async loadBudget(sessionId) {
    void sessionId;
    throw new Error('budgetService.loadBudget is not implemented (Stage 1 stub).');
  },
  async saveBudget(sessionId, rows, userName) {
    void sessionId;
    void rows;
    void userName;
    throw new Error('budgetService.saveBudget is not implemented (Stage 1 stub).');
  },
  calculateVariance(planned, used) {
    return Number(planned || 0) - Number(used || 0);
  }
};
