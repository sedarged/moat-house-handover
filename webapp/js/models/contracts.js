/**
 * Canonical application payload contracts for Stage 1.
 * These are intentionally plain JS objects so services can hydrate from Access-backed repositories later.
 */

export const metricDepartments = ['Injection', 'MetaPress', 'Berks', 'Wilts'];

export function createInitialSessionState() {
  return {
    sessionId: null,
    shiftCode: null,
    shiftDate: null,
    userName: null,
    sessionStatus: 'Open',
    departments: [],
    budget: null,
    preview: null,
    attachmentsByDeptRecordId: {},
    reports: []
  };
}

export function createDepartmentModel(deptName) {
  return {
    deptRecordId: null,
    deptName,
    deptStatus: 'Not running',
    downtimeMin: null,
    efficiencyPct: null,
    yieldPct: null,
    deptNotes: '',
    versionNo: 0,
    isMetricDept: metricDepartments.includes(deptName)
  };
}
