using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CashFlowManager.Models;

namespace CashFlowManager.Services
{
    /// <summary>
    /// Manages all transaction data and business logic.
    /// Owns the three core collections required by the assignment.
    /// </summary>
    public class TransactionService
    {
        // Master list of all transactions
        private readonly List<Transaction> _transactions = new List<Transaction>();

        // Transactions grouped by month , the key is first day of each month
        private readonly Dictionary<DateTime, List<Transaction>> _monthlyTransactions
            = new Dictionary<DateTime, List<Transaction>>();

        // Unique category names enforced via HashSet 
        private readonly HashSet<string> _categoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

       
        
        // -Read-Only Access ───

      
        // Returns a read-only view of all transactions.
        public IReadOnlyList<Transaction> GetAllTransactions()
        {
            return _transactions.AsReadOnly();
        }

      
        // Returns all unique category names currently in use.
        public IReadOnlyCollection<string> GetCategoryNames()
        {
            return _categoryNames;
        }

        // ─Add / Remove ────

      
        // Adds a transaction to both the master list and the monthly dictionary.
        public void AddTransaction(Transaction transaction)
        {
            _transactions.Add(transaction);

            // This normalize to first day of month so all days in same month share one key
            DateTime monthKey = new DateTime(transaction.Date.Year, transaction.Date.Month, 1);

            if (!_monthlyTransactions.ContainsKey(monthKey))
                _monthlyTransactions[monthKey] = new List<Transaction>();

            _monthlyTransactions[monthKey].Add(transaction);

            // HashSet silently ignores duplicates
            _categoryNames.Add(transaction.Category.Name);
        }

        
        // Removes a specific transaction from all collections.
        public void RemoveTransaction(Transaction transaction)
        {
            _transactions.Remove(transaction);

            DateTime monthKey = new DateTime(transaction.Date.Year, transaction.Date.Month, 1);

            if (_monthlyTransactions.ContainsKey(monthKey))
            {
                _monthlyTransactions[monthKey].Remove(transaction);

                // Clean up empty month entries so the dictionary stays tidy
                if (_monthlyTransactions[monthKey].Count == 0)
                    _monthlyTransactions.Remove(monthKey);
            }
        }

        // Monthly Queries ─────

       
        // Returns all transactions for a given month, or empty list if none exist.
        public IReadOnlyList<Transaction> GetTransactionsForMonth(DateTime monthKey)
        {
            DateTime normalizedKey = new DateTime(monthKey.Year, monthKey.Month, 1);

            if (_monthlyTransactions.TryGetValue(normalizedKey, out List<Transaction>? transactions))
                return transactions.AsReadOnly();

            return new List<Transaction>().AsReadOnly();
        }

        
        // Returns all months that have at least one transaction, sorted newest first.
        public IReadOnlyList<DateTime> GetAvailableMonths()
        {
            List<DateTime> months = new List<DateTime>(_monthlyTransactions.Keys);
            months.Sort((a, b) => b.CompareTo(a));
            return months.AsReadOnly();
        }

        // ─── Cash-Flow Calculation 

        /// <summary>
        /// Calculates cash-flow totals for a given month.
        /// Uses a tuple to return three related values from a single method call.
        /// </summary>
        /// <param name="monthKey">First day of the target month.</param>
        /// <returns>
        /// A named tuple with TotalRevenue, TotalExpense, and NetCashFlow.
        /// </returns>
        public (decimal TotalRevenue, decimal TotalExpense, decimal NetCashFlow)
            CalculateMonthlyCashFlow(DateTime monthKey)
        {
            IReadOnlyList<Transaction> monthTransactions = GetTransactionsForMonth(monthKey);

            decimal totalRevenue = 0m;
            decimal totalExpense = 0m;

            foreach (Transaction t in monthTransactions)
            {
                if (t.Category.Type == CategoryType.Revenue)
                    totalRevenue += t.Amount;
                else
                    totalExpense += t.Amount;
            }

            decimal netCashFlow = totalRevenue - totalExpense;
            return (totalRevenue, totalExpense, netCashFlow);
        }

        // ─── Report Generation 

        /// <summary>
        /// Generates a monthly report with top 3 expense categories,
        /// top 3 revenue sources, and net cash-flow.
        /// Uses a tuple to bundle all report data into one return value.
        /// </summary>
        /// <param name="monthKey">First day of the target month.</param>
        /// <returns>
        /// A named tuple containing TopExpenseCategories, TopRevenueCategories,
        /// and the full cash-flow summary.
        /// </returns>
        public (
            List<(string CategoryName, decimal Total)> TopExpenseCategories,
            List<(string CategoryName, decimal Total)> TopRevenueCategories,
            (decimal TotalRevenue, decimal TotalExpense, decimal NetCashFlow) CashFlow)
            GenerateMonthlyReport(DateTime monthKey)
        {
            IReadOnlyList<Transaction> monthTransactions = GetTransactionsForMonth(monthKey);

            // Group by category name and sum amounts for expenses
            Dictionary<string, decimal> expenseTotals = new Dictionary<string, decimal>();
            Dictionary<string, decimal> revenueTotals = new Dictionary<string, decimal>();

            foreach (Transaction t in monthTransactions)
            {
                if (t.Category.Type == CategoryType.Expense)
                {
                    if (!expenseTotals.ContainsKey(t.Category.Name))
                        expenseTotals[t.Category.Name] = 0m;
                    expenseTotals[t.Category.Name] += t.Amount;
                }
                else
                {
                    if (!revenueTotals.ContainsKey(t.Category.Name))
                        revenueTotals[t.Category.Name] = 0m;
                    revenueTotals[t.Category.Name] += t.Amount;
                }
            }

            List<(string, decimal)> topExpenses = GetTopThreeCategories(expenseTotals);
            List<(string, decimal)> topRevenues = GetTopThreeCategories(revenueTotals);
            (decimal, decimal, decimal) cashFlow = CalculateMonthlyCashFlow(monthKey);

            return (topExpenses, topRevenues, cashFlow);
        }

        
        // Sorts a category-total dictionary and returns the top 3 entries.
        private List<(string CategoryName, decimal Total)> GetTopThreeCategories(
            Dictionary<string, decimal> totals)
        {
            List<(string CategoryName, decimal Total)> result
                = new List<(string, decimal)>();

            foreach (KeyValuePair<string, decimal> entry in totals)
                result.Add((entry.Key, entry.Value));

            // Sort descending by total amount
            result.Sort((a, b) => b.Total.CompareTo(a.Total));

            // Return at most 3
            return result.Count > 3 ? result.GetRange(0, 3) : result;
        }

        // ─── Search and Filter 

        /// <summary>
        /// Searches transactions by date, category name, or description keyword.
        /// All comparisons are case-insensitive.
        /// </summary>
        /// <param name="dateFilter">Optional date to match exactly.</param>
        /// <param name="categoryFilter">Optional category name substring.</param>
        /// <param name="descriptionFilter">Optional description keyword.</param>
        public IReadOnlyList<Transaction> SearchTransactions(
            DateTime? dateFilter,
            string? categoryFilter,
            string? descriptionFilter)
        {
            List<Transaction> results = new List<Transaction>(_transactions);

            if (dateFilter.HasValue)
                results = results.FindAll(t => t.Date.Date == dateFilter.Value.Date);

            if (!string.IsNullOrWhiteSpace(categoryFilter))
                results = results.FindAll(t =>
                    t.Category.Name.Contains(categoryFilter, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(descriptionFilter))
                results = results.FindAll(t =>
                    t.Description.Contains(descriptionFilter, StringComparison.OrdinalIgnoreCase));

            return results.AsReadOnly();
        }

        /// <summary>
        /// Filters transactions by category type and/or month.
        /// </summary>
        /// <param name="typeFilter">Optional type (Expense or Revenue).</param>
        /// <param name="monthFilter">Optional month to restrict results to.</param>
        public IReadOnlyList<Transaction> FilterTransactions(
            CategoryType? typeFilter,
            DateTime? monthFilter)
        {
            List<Transaction> results = new List<Transaction>(_transactions);

            if (typeFilter.HasValue)
                results = results.FindAll(t => t.Category.Type == typeFilter.Value);

            if (monthFilter.HasValue)
                results = results.FindAll(t =>
                    t.Date.Year == monthFilter.Value.Year &&
                    t.Date.Month == monthFilter.Value.Month);

            return results.AsReadOnly();
        }

        // ─── Budget Tracking 

        // Maps category name to its monthly budget cap
        private readonly Dictionary<string, decimal> _categoryBudgets
            = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Sets a monthly spending budget for a category.
        /// </summary>
        /// <param name="categoryName">The category to budget.</param>
        /// <param name="budgetAmount">The maximum allowed spend per month.</param>
        public void SetCategoryBudget(string categoryName, decimal budgetAmount)
        {
            _categoryBudgets[categoryName] = budgetAmount;
        }

        /// <summary>
        /// Checks whether spending in a category has exceeded its budget for the given month.
        /// Returns a tuple indicating if the budget is exceeded and by how much.
        /// </summary>
        /// <param name="categoryName">The category to check.</param>
        /// <param name="monthKey">First day of the target month.</param>
        /// <returns>IsExceeded flag and the overspend amount (0 if within budget).</returns>
        public (bool IsExceeded, decimal OverspendAmount) CheckBudget(
            string categoryName, DateTime monthKey)
        {
            if (!_categoryBudgets.TryGetValue(categoryName, out decimal budget))
                return (false, 0m);

            IReadOnlyList<Transaction> monthTransactions = GetTransactionsForMonth(monthKey);

            decimal spent = 0m;
            foreach (Transaction t in monthTransactions)
            {
                if (t.Category.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase)
                    && t.Category.Type == CategoryType.Expense)
                    spent += t.Amount;
            }

            decimal overspend = spent - budget;
            return overspend > 0 ? (true, overspend) : (false, 0m);
        }

        // ─── Bulk Load (used by FileService after deserializing)

        /// <summary>
        /// Replaces all current transactions with a loaded set.
        /// Called by FileService after reading from disk.
        /// </summary>
        /// <param name="transactions">The transactions to load in.</param>
        public void LoadTransactions(IEnumerable<Transaction> transactions)
        {
            _transactions.Clear();
            _monthlyTransactions.Clear();
            _categoryNames.Clear();

            foreach (Transaction t in transactions)
                AddTransaction(t);
        }
    }
}