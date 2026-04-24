-- Required seed lookup data for Stage 2 bootstrap.
-- IMPORTANT:
-- - This seed file avoids generic/fake department names becoming accidental source of truth.
-- - Only departments explicitly confirmed in source specs are seeded here.
-- - Any additional non-metric departments must be added from approved business-owned list in Stage 2.

-- Canonical metric/closure departments from spec
INSERT INTO tblDepartments (DeptName, DisplayOrder, IsMetricDept, IsClosureDept, IsActive) VALUES ('Injection', 1, TRUE, TRUE, TRUE);
INSERT INTO tblDepartments (DeptName, DisplayOrder, IsMetricDept, IsClosureDept, IsActive) VALUES ('MetaPress', 2, TRUE, TRUE, TRUE);
INSERT INTO tblDepartments (DeptName, DisplayOrder, IsMetricDept, IsClosureDept, IsActive) VALUES ('Berks', 3, TRUE, TRUE, TRUE);
INSERT INTO tblDepartments (DeptName, DisplayOrder, IsMetricDept, IsClosureDept, IsActive) VALUES ('Wilts', 4, TRUE, TRUE, TRUE);

-- Shift rules and email profiles are Stage 1 bootstrap placeholders and must be confirmed/updated in Stage 2.
INSERT INTO tblShiftRules (ShiftCode, ShiftName, EmailProfileKey, DisplayOrder) VALUES ('AM', 'Morning', 'default_am', 1);
INSERT INTO tblShiftRules (ShiftCode, ShiftName, EmailProfileKey, DisplayOrder) VALUES ('PM', 'Afternoon', 'default_pm', 2);
INSERT INTO tblShiftRules (ShiftCode, ShiftName, EmailProfileKey, DisplayOrder) VALUES ('NS', 'Night', 'default_ns', 3);

INSERT INTO tblEmailProfiles (EmailProfileKey, ToList, CcList, SubjectTemplate, BodyTemplate, IsActive)
VALUES ('default_am', 'handover-am@example.com', '', 'AM Handover - {ShiftDate}', 'Please find AM handover attached.', TRUE);
INSERT INTO tblEmailProfiles (EmailProfileKey, ToList, CcList, SubjectTemplate, BodyTemplate, IsActive)
VALUES ('default_pm', 'handover-pm@example.com', '', 'PM Handover - {ShiftDate}', 'Please find PM handover attached.', TRUE);
INSERT INTO tblEmailProfiles (EmailProfileKey, ToList, CcList, SubjectTemplate, BodyTemplate, IsActive)
VALUES ('default_ns', 'handover-ns@example.com', '', 'NS Handover - {ShiftDate}', 'Please find NS handover attached.', TRUE);

-- Paths below are template defaults for local-first/shared deployment and should be environment-specific in Stage 2 packaging.
INSERT INTO tblConfig (ConfigKey, ConfigValue, Notes) VALUES ('accessDatabasePath', 'C:/MOAT-Handover/shared/moat_handover_be.accdb', 'Shared Access backend path');
INSERT INTO tblConfig (ConfigKey, ConfigValue, Notes) VALUES ('attachmentsRoot', 'C:/MOAT-Handover/shared/Attachments', 'Attachment root path');
INSERT INTO tblConfig (ConfigKey, ConfigValue, Notes) VALUES ('reportsOutputRoot', 'C:/MOAT-Handover/shared/Reports', 'Report output root path');
