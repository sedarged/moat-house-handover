# Windows Runtime Test Checklist (Stage 5B Practical Pass)

Use this checklist on a real Windows workstation.

Prerequisites:
- WebView2 Runtime installed
- Access Database Engine / ACE provider available
- Outlook desktop installed for draft workflow checks
- Packaged app available (from `dist/local-host`)

## Test steps (record Pass/Fail + notes for each)

1. **Launch app**
   - Launch desktop host executable.
   - Confirm Shift screen loads.

2. **Run Diagnostics**
   - Open Diagnostics from Shift or Dashboard.
   - Run diagnostics and record overall status.

3. **Open AM session for today**
   - Choose AM + today's date.
   - Attempt to open session.

4. **Create blank session if needed**
   - If no existing AM session is found, confirm blank session creation.

5. **Save department data**
   - Open one department.
   - Update status/notes (and metrics for metric departments), then save.

6. **Add/remove attachment**
   - Add one attachment to that department.
   - Remove one attachment and confirm refresh.

7. **Open attachment viewer**
   - Open viewer and verify image/metadata navigation behavior.

8. **Edit/save budget**
   - Open budget screen.
   - Edit planned/used/reason and save.

9. **Open preview**
   - Open preview screen.
   - Confirm saved session/budget/department data is shown.

10. **Generate handover report**
    - Generate handover report.
    - Confirm file appears in reports root.

11. **Generate budget report**
    - Generate budget report.
    - Confirm file appears in reports root.

12. **Prepare send package**
    - Open send screen and prepare package.
    - Confirm readiness + validation messages are understandable.

13. **Create Outlook draft**
    - Create Outlook draft.
    - Confirm draft is created with report attachments.

14. **Confirm email is NOT sent**
    - Verify mail is saved as draft only.
    - Confirm no automatic send occurs.

15. **Check audit log entries**
    - Open Diagnostics and refresh recent audit list.
    - Confirm entries exist for tested actions.

16. **Reopen app and confirm persistence**
    - Close app and relaunch.
    - Reopen same session and confirm saved data persists.

17. **Record bugs with exact reproduction steps**
    - For each failure, capture:
      - screenshot
      - diagnostics output
      - log file
      - exact step number + exact user actions

## Regression reminders

During the pass, confirm no regressions in:
- session open/create/clear flow
- department save/load and metric visibility rules
- attachment list/add/remove/viewer
- budget load/recalculate/save
- preview rendering
- report generation
- send package + Outlook draft-only boundary
