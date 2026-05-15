using CashFlowManager.Helpers;
using CashFlowManager.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CashFlowManager.ViewModels
{
    // ViewModel for the report window 
    // Receives the shared TransactionService instance from MainViewModel.
    public class ReportViewModel : BaseViewModel
    {
        private readonly TransactionService _transactionService;

        // ─── Constructor 

        public ReportViewModel(TransactionService transactionService)
        {
            _transactionService = transactionService;

            GenerateReportCommand = new RelayCommand(ExecuteGenerateReport, CanExecuteGenerateReport);

            // Populate month options from whatever transactions are already loaded
            RefreshAvailableMonths();

            // Default to current month
            SelectedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        }

        // ─── Observable Collections 

        // Top 3 expense categories shown in the report list
        public ObservableCollection<CategoryReportItem> TopExpenses { get; }
            = new ObservableCollection<CategoryReportItem>();

        // Top 3 revenue sources shown in the report list
        public ObservableCollection<CategoryReportItem> TopRevenues { get; }
            = new ObservableCollection<CategoryReportItem>();

        // Month options for the report month picker
        public ObservableCollection<DateTime> AvailableMonths { get; }
            = new ObservableCollection<DateTime>();

        // ─── Properties 

        private DateTime _selectedMonth;
        // The month the user wants to generate a report for
        public DateTime SelectedMonth
        {
            get => _selectedMonth;
            set => SetProperty(ref _selectedMonth, value);
        }

        private decimal _reportRevenue;
        // Total revenue for the reported month
        public decimal ReportRevenue
        {
            get => _reportRevenue;
            private set => SetProperty(ref _reportRevenue, value);
        }

        private decimal _reportExpense;
        // Total expenses for the reported month
        public decimal ReportExpense
        {
            get => _reportExpense;
            private set => SetProperty(ref _reportExpense, value);
        }

        private decimal _reportNetCashFlow;
        // Net cash-flow for the reported month (Revenue - Expenses)
        public decimal ReportNetCashFlow
        {
            get => _reportNetCashFlow;
            private set => SetProperty(ref _reportNetCashFlow, value);
        }

        private string _cashFlowLabel = "Net Cash-Flow";
        // Surplus or Deficit depending on whether net it is positive or negative
        public string CashFlowLabel
        {
            get => _cashFlowLabel;
            private set => SetProperty(ref _cashFlowLabel, value);
        }

        private string _reportMonth = string.Empty;
        // Formatted month/year string shown as the report heading
        public string ReportMonth
        {
            get => _reportMonth;
            private set => SetProperty(ref _reportMonth, value);
        }

        private bool _hasReportData;
        // Controls visibility of report results in the UI
        public bool HasReportData
        {
            get => _hasReportData;
            private set => SetProperty(ref _hasReportData, value);
        }

        private string _statusMessage = "Select a month and generate a report.";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // ─── Commands 

        public RelayCommand GenerateReportCommand { get; }

        private bool CanExecuteGenerateReport(object? parameter)
        {
            return AvailableMonths.Count > 0;
        }

        // Calls the service, unpacks the nested tuple, and populates all display properties
        private void ExecuteGenerateReport(object? parameter)
        {
            try
            {
                DateTime monthKey = new DateTime(SelectedMonth.Year, SelectedMonth.Month, 1);

                // Unpack the nested tuple returned by GenerateMonthlyReport
                (List<(string CategoryName, decimal Total)> topExpenses,
                 List<(string CategoryName, decimal Total)> topRevenues,
                 (decimal totalRevenue, decimal totalExpense, decimal netCashFlow) cashFlow)
                    = _transactionService.GenerateMonthlyReport(monthKey);

                // Update cash-flow summary
                ReportRevenue = cashFlow.totalRevenue;
                ReportExpense = cashFlow.totalExpense;
                ReportNetCashFlow = cashFlow.netCashFlow;
                CashFlowLabel = cashFlow.netCashFlow >= 0 ? "Surplus" : "Deficit";
                ReportMonth = SelectedMonth.ToString("MMMM yyyy");

                // Rebuild expense list
                TopExpenses.Clear();
                foreach ((string name, decimal total) in topExpenses)
                    TopExpenses.Add(new CategoryReportItem(name, total));

                // Rebuild revenue list
                TopRevenues.Clear();
                foreach ((string name, decimal total) in topRevenues)
                    TopRevenues.Add(new CategoryReportItem(name, total));

                HasReportData = true;
                StatusMessage = $"Report generated for {ReportMonth}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error generating report: {ex.Message}";
                HasReportData = false;
            }
        }

        // Syncs available months from the service so the ComboBox stays up to date
        public void RefreshAvailableMonths()
        {
            AvailableMonths.Clear();
            foreach (DateTime month in _transactionService.GetAvailableMonths())
                AvailableMonths.Add(month);
        }
    }

    // ─── Helper Model 

    // Simple display model for a category name + total pair in the report lists.
    // Kept in this file since it only exists to support ReportViewModel.
    public class CategoryReportItem
    {
        public string CategoryName { get; }
        public decimal Total { get; }

        // Formatted string used directly in the UI list binding
        public string DisplayText => $"{CategoryName}: {Total:C}";

        public CategoryReportItem(string categoryName, decimal total)
        {
            CategoryName = categoryName;
            Total = total;
        }
    }
}
