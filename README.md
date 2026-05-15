# Cash Flow Manager

A WPF desktop application for tracking personal or business monthly cash-flow,
built with C# and the MVVM pattern as part of the Programming in C# II course
at Malmö University.

## Screenshots

### Main View
![Main View](screenshots/main-view.png)

### Report View
![Report View](screenshots/report-view.png)

## Features

- **Add & manage transactions** — log expenses and revenues with date, amount,
  category, and description
- **Monthly cash-flow summary** — automatically calculates total revenue, total
  expenses, and net surplus/deficit per month
- **Search** — find transactions by date, category name, or description keyword
- **Filter** — filter transactions by type (Expense/Revenue) or month
- **Monthly reports** — generate a report showing top 3 expense categories,
  top 3 revenue sources, and net cash-flow
- **Save & Load** — persist transactions to a JSON file and reload them between
  sessions using a file dialog
- **Budget tracking** — set monthly spending limits per category and get warned
  when the budget is exceeded

## Project Structure
CashFlowManager/
├── Helpers/
│   ├── InverseBoolToVisibilityConverter.cs
│   └── RelayCommand.cs
├── Models/
│   ├── Category.cs
│   ├── CategoryType.cs
│   └── Transaction.cs
├── Services/
│   ├── FileService.cs
│   └── TransactionService.cs
├── ViewModels/
│   ├── BaseViewModel.cs
│   ├── MainViewModel.cs
│   └── ReportViewModel.cs
├── Views/
│   ├── MainWindow.xaml
│   └── ReportWindow.xaml
└── screenshots/
├── main-view.png
└── report-view.png

## Author

Richmond Boakye  
Malmö University — Programming in C# II
