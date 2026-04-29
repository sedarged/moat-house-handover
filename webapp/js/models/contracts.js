export const DEPARTMENTS = [
  'Injection',
  'MetaPress',
  'Berks',
  'Wilts',
  'Racking',
  'Butchery',
  'Further Processing',
  'Tumblers',
  'Smoke Tumbler',
  'Minimums & Samples',
  'Goods In & Despatch',
  'Dry Goods',
  'Additional'
];

export const metricDepartments = ['Injection', 'MetaPress', 'Berks', 'Wilts'];

export const DEPT_STATUSES = ['Not running', 'Complete', 'Incomplete'];

export const SHIFT_TIMES = {
  AM: '06:00 – 14:00',
  PM: '14:00 – 22:00',
  NS: '22:00 – 06:00'
};

export function isMetricDepartment(deptName) {
  return metricDepartments.includes(deptName);
}

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
    isMetricDept: isMetricDepartment(deptName)
  };
}
