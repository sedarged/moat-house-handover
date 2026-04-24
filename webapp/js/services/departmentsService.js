import { hostRequest } from '../core/hostBridge.js';
import { metricDepartments } from '../models/contracts.js';

function isMetricDept(deptName) {
  return metricDepartments.includes(deptName);
}

export const departmentsService = {
  async loadDepartment(sessionId, deptName) {
    return hostRequest('department.load', { sessionId, deptName });
  },

  async saveDepartment(deptPayload) {
    const payload = {
      deptRecordId: deptPayload.deptRecordId,
      sessionId: deptPayload.sessionId,
      deptName: deptPayload.deptName,
      deptStatus: deptPayload.deptStatus,
      deptNotes: deptPayload.deptNotes,
      downtimeMin: deptPayload.downtimeMin,
      efficiencyPct: deptPayload.efficiencyPct,
      yieldPct: deptPayload.yieldPct,
      userName: deptPayload.userName
    };

    if (!isMetricDept(payload.deptName)) {
      payload.downtimeMin = null;
      payload.efficiencyPct = null;
      payload.yieldPct = null;
    }

    return hostRequest('department.save', payload);
  },

  validateDepartment(deptPayload) {
    const errors = [];

    if (!deptPayload?.deptStatus?.trim()) {
      errors.push('Department status is required.');
    }

    if (isMetricDept(deptPayload?.deptName)) {
      if (deptPayload.downtimeMin != null && deptPayload.downtimeMin < 0) {
        errors.push('Downtime must be zero or greater.');
      }

      const percentFields = [
        { key: 'efficiencyPct', label: 'Efficiency' },
        { key: 'yieldPct', label: 'Yield' }
      ];

      percentFields.forEach((field) => {
        const value = deptPayload[field.key];
        if (value == null || Number.isNaN(value)) {
          return;
        }

        if (value < 0 || value > 100) {
          errors.push(`${field.label} must be between 0 and 100.`);
        }
      });
    }

    return { ok: errors.length === 0, errors };
  }
};
