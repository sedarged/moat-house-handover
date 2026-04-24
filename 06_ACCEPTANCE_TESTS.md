# MOAT HOUSE HANDOVER v2 — ACCEPTANCE TESTS

## 1. Session Tests
### Test 1 — New Day
- Select shift/date that does not exist
- Confirm blank new-day prompt appears
- Accept prompt
- Verify blank dashboard opens

### Test 2 — Reopen Existing Day
- Save one department
- Close app
- Reopen same shift/date
- Verify saved state is loaded

### Test 3 — Clear Day
- Open existing session
- Click Clear Day
- Confirm all department tiles reset
- Reopen session and confirm cleared state persists

## 2. Department Tests
### Test 4 — Metric Department
- Open Injection
- Verify DT/Eff/Yield fields are visible
- Save values
- Reopen and verify values persist

### Test 5 — Non-Metric Department
- Open a non-metric department
- Verify DT/Eff/Yield fields are hidden
- Save notes/status
- Reopen and verify values persist

## 3. Attachment Tests
### Test 6 — Add Attachments
- Add 3 images to one department
- Verify list count is correct
- Verify thumbnail preview updates

### Test 7 — Prev/Next
- Navigate through attachments in department screen
- Open full viewer
- Navigate in viewer
- Verify image and metadata always change correctly

### Test 8 — Remove Attachment
- Remove middle attachment
- Verify list order remains valid
- Verify preview updates correctly
- Reopen and confirm deletion persists

## 4. Budget Tests
### Test 9 — Budget Save/Reopen
- Enter planned/used values
- Save budget
- Reopen and verify values persist

### Test 10 — Variance Logic
- Verify variance reflects planned minus used
- Verify totals summary refreshes after save

## 5. Preview Tests
### Test 11 — Preview Accuracy
- Save at least two departments and one budget
- Open preview
- Verify preview shows correct status, notes, closure, and budget summary

## 6. Report Tests
### Test 12 — Report Output
- Generate handover report
- Generate budget report
- Verify files are created in expected output folder

## 7. Send Tests
### Test 13 — Draft Creation
- Open send screen
- Verify validation summary is correct
- Create draft
- Verify recipients and subject are correct for selected shift

## 8. Multi-User Tests
### Test 14 — Parallel Edit Safety
- User A opens Injection
- User B edits and saves Injection
- User A attempts save
- Verify stale-data handling path appears
