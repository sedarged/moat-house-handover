/** Session service stub: open/create/clear handover session. */
export const sessionService = {
  async openSession(shiftCode, shiftDate, userName) {
    void shiftCode;
    void shiftDate;
    void userName;
    throw new Error('sessionService.openSession is not implemented (Stage 1 stub).');
  },
  async createBlankSession(shiftCode, shiftDate, userName) {
    void shiftCode;
    void shiftDate;
    void userName;
    throw new Error('sessionService.createBlankSession is not implemented (Stage 1 stub).');
  },
  async clearDay(sessionId, userName) {
    void sessionId;
    void userName;
    throw new Error('sessionService.clearDay is not implemented (Stage 1 stub).');
  }
};
