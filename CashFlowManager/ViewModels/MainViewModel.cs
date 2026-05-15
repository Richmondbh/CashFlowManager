using CashFlowManager.Helpers;
using CashFlowManager.Models;
using CashFlowManager.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CashFlowManager.ViewModels
{
    /// <summary>
    /// Primary ViewModel for the main window.
    /// Exposes all properties and commands the UI binds to.
    /// All business logic is delegated to TransactionService.
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly TransactionService _transactionService;

        // Exposes the service so ReportWindow can share the same data
        public TransactionService TransactionService => _transactionService;
        private readonly FileService _fileService;

        // ─── Constructor

        // Initializes the ViewModel, services, commands, and default input values.
        public MainViewModel()
        {
            _transactionService = new TransactionService();
            _fileService = new FileService();

            // Wire up all commands
            AddTransactionCommand = new RelayCommand(ExecuteAddTransaction, CanExecuteAddTransaction);
            DeleteTransactionCommand = new RelayCommand(ExecuteDeleteTransaction, CanExecuteDeleteTransaction);
            SaveCommand = new RelayCommand(ExecuteSave);
            LoadCommand = new RelayCommand(ExecuteLoad);
            SearchCommand = new RelayCommand(ExecuteSearch);
            ClearSearchCommand = new RelayCommand(ExecuteClearSearch);
            FilterCommand = new RelayCommand(ExecuteFilter);
            ClearFilterCommand = new RelayCommand(ExecuteClearFilter);
            SetBudgetCommand = new RelayCommand(ExecuteSetBudget, CanExecuteSetBudget);

            // Seed default input date to today
            InputDate = DateTime.Today;

            // Populate category type options for ComboBox binding
            CategoryTypeOptions = new List<CategoryType>
            {
                CategoryType.Expense,
                CategoryType.Revenue
            };

            RefreshTransactionList();
        }

        // ─── Observable Collections

      
        // The list of transactions currently displayed in the UI.
        // Switches between all transactions, search results, and filter results.
   
        public ObservableCollection<Transaction> DisplayedTransactions { get; }
            = new ObservableCollection<Transaction>();

        
        // Available months derived from loaded transactions, for the month picker.
        public ObservableCollection<DateTime> AvailableMonths { get; }
            = new ObservableCollection<DateTime>();

       
        // Category type options bound to the CategoryType ComboBox.
        public List<CategoryType> CategoryTypeOptions { get; }

        // Input Properties (bound to the Add Transaction form)

        private DateTime _inputDate;
        // Gets or sets the date entered by the user for a new transaction.
        public DateTime InputDate
        {
            get => _inputDate;
            set => SetProperty(ref _inputDate, value);
        }

        private decimal _inputAmount;
        // Gets or sets the amount entered for a new transaction
        public decimal InputAmount
        {
            get => _inputAmount;
            set => SetProperty(ref _inputAmount, value);
        }

        private string _inputCategoryName = string.Empty;
        //Gets or sets the category name entered for a new transaction.
        public string InputCategoryName
        {
            get => _inputCategoryName;
            set => SetProperty(ref _inputCategoryName, value);
        }

        private CategoryType _inputCategoryType;
        // Gets or sets the category type selected for a new transaction.
        public CategoryType InputCategoryType
        {
            get => _inputCategoryType;
            set => SetProperty(ref _inputCategoryType, value);
        }

        private string _inputDescription = string.Empty;
        //Gets or sets the description entered for a new transaction
        public string InputDescription
        {
            get => _inputDescription;
            set => SetProperty(ref _inputDescription, value);
        }

        // ─ Selection 

        private Transaction? _selectedTransaction;
        //Gets or sets the transaction currently selected in the list.
        public Transaction? SelectedTransaction
        {
            get => _selectedTransaction;
            set => SetProperty(ref _selectedTransaction, value);
        }

        private DateTime? _selectedMonth;
        //Gets or sets the month selected for cash-flow display
        public DateTime? SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                SetProperty(ref _selectedMonth, value);
                RefreshCashFlow();
            }
        }

        // ─── Cash-Flow Display Properties 

        private decimal _totalRevenue;
        //Gets the total revenue for the selected month.
        public decimal TotalRevenue
        {
            get => _totalRevenue;
            private set => SetProperty(ref _totalRevenue, value);
        }

        private decimal _totalExpense;
        //Gets the total expenses for the selected month.
        public decimal TotalExpense
        {
            get => _totalExpense;
            private set => SetProperty(ref _totalExpense, value);
        }

        private decimal _netCashFlow;
        //Gets the net cash-flow (revenue minus expenses) for the selected month.
        public decimal NetCashFlow
        {
            get => _netCashFlow;
            private set => SetProperty(ref _netCashFlow, value);
        }

        private string _cashFlowLabel = "Net Cash-Flow";

         
        // Gets a contextual label for net cash-flow.
        // Displays Surplus/Deficit based on whether the value is positive or negative.
        public string CashFlowLabel
        {
            get => _cashFlowLabel;
            private set => SetProperty(ref _cashFlowLabel, value);
        }

        // ─── Search Properties

        private string _searchCategory = string.Empty;
        //Gets or sets the category keyword used for searching.
        public string SearchCategory
        {
            get => _searchCategory;
            set => SetProperty(ref _searchCategory, value);
        }

        private string _searchDescription = string.Empty;
        //Gets or sets the description keyword used for searching.
        public string SearchDescription
        {
            get => _searchDescription;
            set => SetProperty(ref _searchDescription, value);
        }

        private DateTime? _searchDate;
        //Gets or sets the date used for searching. Null means no date filter.
        public DateTime? SearchDate
        {
            get => _searchDate;
            set => SetProperty(ref _searchDate, value);
        }

        // ─── Filter Properties 

        private CategoryType? _filterType;
        //Gets or sets the category type filter. Null means show all.
        public CategoryType? FilterType
        {
            get => _filterType;
            set => SetProperty(ref _filterType, value);
        }

        private DateTime? _filterMonth;
        //Gets or sets the month filter. Null means show all months.
        public DateTime? FilterMonth
        {
            get => _filterMonth;
            set => SetProperty(ref _filterMonth, value);
        }

        // ─── Budget Properties 

        private string _budgetCategoryName = string.Empty;
        //Gets or sets the category name for budget assignment.
        public string BudgetCategoryName
        {
            get => _budgetCategoryName;
            set => SetProperty(ref _budgetCategoryName, value);
        }

        private decimal _budgetAmount;
        //Gets or sets the budget amount to assign to a category.
        public decimal BudgetAmount
        {
            get => _budgetAmount;
            set => SetProperty(ref _budgetAmount, value);
        }

        // ─── Status Bar 

        private string _statusMessage = "Ready.";
        //Gets or sets the status bar message shown at the bottom of the UI.
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // String options for the filter type ComboBox
        public List<string> FilterTypeOptions { get; } = new List<string> { "All", "Expense", "Revenue" };

        private string _filterTypeString = "All";
        // Converts the string selection to a nullable CategoryType for the service
        public string FilterTypeString
        {
            get => _filterTypeString;
            set
            {
                SetProperty(ref _filterTypeString, value);
                FilterType = value switch
                {
                    "Expense" => CategoryType.Expense,
                    "Revenue" => CategoryType.Revenue,
                    _ => null
                };
            }
        }

        // ─── Commands 

        //Command to add a new transaction from the input form.
        public RelayCommand AddTransactionCommand { get; }

        //<summary>Command to delete the currently selected transaction.
        public RelayCommand DeleteTransactionCommand { get; }

        //Command to save all transactions to disk.
        public RelayCommand SaveCommand { get; }

        //Command to load transactions from disk.
        public RelayCommand LoadCommand { get; }

        //Command to search transactions by date, category, or description.
        public RelayCommand SearchCommand { get; }

        //Command to clear search and restore the full transaction list.
        public RelayCommand ClearSearchCommand { get; }

        //Command to filter transactions by type or month.
        public RelayCommand FilterCommand { get; }

        //Command to clear all filters and restore the full transaction list.
        public RelayCommand ClearFilterCommand { get; }

        //Command to assign a monthly budget to a category.
        public RelayCommand SetBudgetCommand { get; }

        // ─── Command Implementations ─

        
        // Validates that the add form has sufficient data before allowing execution.
        private bool CanExecuteAddTransaction(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(InputCategoryName)
                && InputAmount > 0;
        }

       
        // Builds a Transaction from the input form and passes it to the service.
        // Clears the form and refreshes the UI on success.
        private void ExecuteAddTransaction(object? parameter)
        {
            try
            {
                Category category = new Category(InputCategoryName.Trim(), InputCategoryType);
                Transaction transaction = new Transaction(InputDate, InputAmount, category, InputDescription.Trim());

                _transactionService.AddTransaction(transaction);

                // Captures the data before ClearInputForm resets them
                string categoryName = InputCategoryName.Trim();
                decimal amount = InputAmount;

                // Check budget immediately after adding an expense
                if (InputCategoryType == CategoryType.Expense)
                    CheckAndNotifyBudget(InputCategoryName.Trim(), InputDate);

                ClearInputForm();
                RefreshTransactionList();
                RefreshAvailableMonths();
                RefreshCashFlow();

                StatusMessage = $"Transaction added: {category.Name} — {InputAmount:C}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding transaction: {ex.Message}";
            }
        }

        // This validates that a transaction is selected before allowing deletion.
        private bool CanExecuteDeleteTransaction(object? parameter)
        {
            return SelectedTransaction != null;
        }

  
        // Removes the selected transaction and refreshes all UI state.
        private void ExecuteDeleteTransaction(object? parameter)
        {
            if (SelectedTransaction == null)
                return;

            try
            {
                Transaction toDelete = SelectedTransaction;
                _transactionService.RemoveTransaction(toDelete);

                SelectedTransaction = null;
                RefreshTransactionList();
                RefreshAvailableMonths();
                RefreshCashFlow();

                StatusMessage = "Transaction deleted.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting transaction: {ex.Message}";
            }
        }

  
        // Saves all transactions to disk via FileService and reports the outcome.
        private void ExecuteSave(object? parameter)
        {
            (bool isSuccess, string message) = _fileService.SaveTransactions(
                _transactionService.GetAllTransactions());

            StatusMessage = message;

            if (!isSuccess)
                MessageBox.Show(message, "Save Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

    
        // Loads transactions from disk, passes them to the service, and refreshes the UI.
        private void ExecuteLoad(object? parameter)
        {
            (bool isSuccess, string message, List<Transaction> transactions) =
                _fileService.LoadTransactions();

            StatusMessage = message;

            if (isSuccess)
            {
                _transactionService.LoadTransactions(transactions);
                RefreshTransactionList();
                RefreshAvailableMonths();
                RefreshCashFlow();
            }
            else
            {
                MessageBox.Show(message, "Load Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

       
        // Searches transactions using the current search field values.
        private void ExecuteSearch(object? parameter)
        {
            IReadOnlyList<Transaction> results = _transactionService.SearchTransactions(
                SearchDate,
                SearchCategory,
                SearchDescription);

            UpdateDisplayedTransactions(results);
            StatusMessage = $"Search returned {results.Count} result(s).";
        }

  
        // Clears all search fields and restores the full transaction list.
        private void ExecuteClearSearch(object? parameter)
        {
            SearchDate = null;
            SearchCategory = string.Empty;
            SearchDescription = string.Empty;
            RefreshTransactionList();
            StatusMessage = "Search cleared.";
        }

 
        // Filters transactions using the current filter field values.
        private void ExecuteFilter(object? parameter)
        {
            IReadOnlyList<Transaction> results = _transactionService.FilterTransactions(
                FilterType,
                FilterMonth);

            UpdateDisplayedTransactions(results);
            StatusMessage = $"Filter returned {results.Count} result(s).";
        }

    
        // Clears all filter fields and restores the full transaction list.
        private void ExecuteClearFilter(object? parameter)
        {
            FilterType = null;
            FilterMonth = null;
            RefreshTransactionList();
            StatusMessage = "Filter cleared.";
        }

      
        // Validates budget input before allowing the command to execute.
        private bool CanExecuteSetBudget(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(BudgetCategoryName)
                && BudgetAmount > 0;
        }


        // Assigns the entered budget amount to the entered category name.
        private void ExecuteSetBudget(object? parameter)
        {
            _transactionService.SetCategoryBudget(BudgetCategoryName.Trim(), BudgetAmount);
            StatusMessage = $"Budget of {BudgetAmount:C} set for '{BudgetCategoryName}'.";
            BudgetCategoryName = string.Empty;
            BudgetAmount = 0;
        }

        // ─── Private Helpers 

       
        // Reloads DisplayedTransactions from the full master transaction list.
        private void RefreshTransactionList()
        {
            UpdateDisplayedTransactions(_transactionService.GetAllTransactions());
        }

        
        /// Rebuilds the AvailableMonths collection from the service.
        private void RefreshAvailableMonths()
        {
            AvailableMonths.Clear();
            foreach (DateTime month in _transactionService.GetAvailableMonths())
                AvailableMonths.Add(month);
        }

     
        // Recalculates and updates the cash-flow summary for the selected month.
        // Falls back to all-time totals if no month is selected.
        private void RefreshCashFlow()
        {
            DateTime targetMonth = SelectedMonth ?? DateTime.Today;

            (decimal revenue, decimal expense, decimal net) =
                _transactionService.CalculateMonthlyCashFlow(targetMonth);

            TotalRevenue = revenue;
            TotalExpense = expense;
            NetCashFlow = net;
            CashFlowLabel = net >= 0 ? "Surplus" : "Deficit";
        }

        // Replaces the contents of DisplayedTransactions with a new result set.
        // Using Clear + Add keeps the ObservableCollection's change notifications intact.
        private void UpdateDisplayedTransactions(IEnumerable<Transaction> transactions)
        {
            DisplayedTransactions.Clear();
            foreach (Transaction t in transactions)
                DisplayedTransactions.Add(t);
        }

        // Resets all input form fields back to their default values after a successful add.
        private void ClearInputForm()
        {
            InputDate = DateTime.Today;
            InputAmount = 0;
            InputCategoryName = string.Empty;
            InputCategoryType = CategoryType.Expense;
            InputDescription = string.Empty;
        }

        // Checks whether a category has exceeded its budget and shows a warning if so.
        private void CheckAndNotifyBudget(string categoryName, DateTime transactionDate)
        {
            (bool isExceeded, decimal overspend) =
                _transactionService.CheckBudget(categoryName, transactionDate);

            if (isExceeded)
            {
                MessageBox.Show(
                    $"Warning: '{categoryName}' has exceeded its budget by {overspend:C} this month.",
                    "Budget Exceeded",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}