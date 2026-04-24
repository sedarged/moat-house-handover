# Proposed Stage 1 File Tree

```text
.
├── backend/
│   └── access/
│       ├── README.md
│       ├── schema/
│       │   └── 001_tables_and_indexes.sql
│       ├── seed/
│       │   └── 001_seed_lookup_data.sql
│       └── setup/
│           └── SPLIT_DATABASE_SETUP.md
├── desktop-host/
│   ├── README.md
│   ├── MoatHouseHandover.Host.csproj
│   └── src/
│       ├── App.xaml
│       ├── App.xaml.cs
│       ├── HostConfig.cs
│       ├── MainWindow.xaml
│       └── MainWindow.xaml.cs
├── docs/
│   ├── PROPOSED_FILE_TREE.md
│   └── STAGE1_FOUNDATION.md
├── webapp/
│   ├── index.html
│   ├── css/
│   │   └── app.css
│   └── js/
│       ├── main.js
│       ├── config/
│       │   ├── app.config.template.json
│       │   └── README.md
│       ├── core/
│       │   ├── router.js
│       │   └── template.js
│       ├── models/
│       │   └── contracts.js
│       ├── screens/
│       │   ├── BudgetScreen.js
│       │   ├── DashboardScreen.js
│       │   ├── DepartmentScreen.js
│       │   ├── ImageViewer.js
│       │   ├── PreviewScreen.js
│       │   ├── SendScreen.js
│       │   └── ShiftScreen.js
│       ├── services/
│       │   ├── attachmentsService.js
│       │   ├── budgetService.js
│       │   ├── departmentsService.js
│       │   ├── previewService.js
│       │   ├── reportsService.js
│       │   ├── sendService.js
│       │   └── sessionService.js
│       └── state/
│           └── appState.js
├── .gitignore
├── BUILD_NOTES.md
└── README.md
```
