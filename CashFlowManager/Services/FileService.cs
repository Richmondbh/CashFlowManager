using CashFlowManager.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using JsonException = Newtonsoft.Json.JsonException;
using Formatting = Newtonsoft.Json.Formatting;

namespace CashFlowManager.Services
{
    /// Handles saving and loading transactions to and from a JSON file.
    public class FileService
    {
        // Default file path sits next to the executable 
        private const string DefaultFileName = "transactions.json";

        private readonly string _filePath;

        /// <summary>
        /// Initializes FileService with an optional custom file path.
        /// Defaults to transactions.json in the application directory.
        /// </summary>
        /// <param name="filePath">Optional custom path for the data file.</param>
        public FileService(string? filePath = null)
        {
            _filePath = filePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultFileName);
        }

       
        //Returns the full path of the data file currently in use.
        // Useful for displaying save location in the UI.
      
        public string GetFilePath()
        {
            return _filePath;
        }

        //  Save ───────

        /// <summary>
        /// Serializes all transactions to JSON and writes them to disk.
        /// Returns a result tuple so the ViewModel can react without try/catch everywhere.
        /// </summary>
        /// <param name="transactions">The transactions to save.</param>
        /// <returns>IsSuccess flag and a message describing the outcome.</returns>
        public (bool IsSuccess, string Message) SaveTransactions(IEnumerable<Transaction> transactions)
        {
            try
            {
                // Convert to a serializable DTO list first so records serialize cleanly
                List<TransactionDto> dtoList = new List<TransactionDto>();

                foreach (Transaction t in transactions)
                {
                    dtoList.Add(new TransactionDto
                    {
                        Date = t.Date,
                        Amount = t.Amount,
                        CategoryName = t.Category.Name,
                        CategoryType = t.Category.Type,
                        Description = t.Description
                    });
                }

                string json = JsonConvert.SerializeObject(dtoList, Formatting.Indented);

                // Ensure the directory exists before writing
                string? directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(_filePath, json);

                return (true, $"Saved {dtoList.Count} transactions to {_filePath}");
            }
            catch (IOException ioEx)
            {
                // Separate IO errors from general errors for clearer feedback
                return (false, $"File error while saving: {ioEx.Message}");
            }
            catch (JsonException jsonEx)
            {
                return (false, $"Serialization error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error while saving: {ex.Message}");
            }
        }

        // Load ──────────

        /// <summary>
        /// Reads the JSON file from disk and deserializes it into a list of transactions.
        /// Returns a result tuple so the caller can handle success and failure cleanly.
        /// </summary>
        /// <returns>
        /// IsSuccess flag, a status message, and the loaded transaction list
        /// (empty list on failure so callers never receive null).
        /// </returns>
        public (bool IsSuccess, string Message, List<Transaction> Transactions) LoadTransactions()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return (false, "No saved data file found.", new List<Transaction>());

                string json = File.ReadAllText(_filePath);

                if (string.IsNullOrWhiteSpace(json))
                    return (false, "Data file is empty.", new List<Transaction>());

                List<TransactionDto>? dtoList = JsonConvert.DeserializeObject<List<TransactionDto>>(json);

                if (dtoList == null)
                    return (false, "Failed to parse data file.", new List<Transaction>());

                // Rebuild proper record instances from the DTOs
                List<Transaction> transactions = new List<Transaction>();

                foreach (TransactionDto dto in dtoList)
                {
                    Category category = new Category(dto.CategoryName, dto.CategoryType);
                    Transaction transaction = new Transaction(
                        dto.Date,
                        dto.Amount,
                        category,
                        dto.Description);

                    transactions.Add(transaction);
                }

                return (true, $"Loaded {transactions.Count} transactions.", transactions);
            }
            catch (IOException ioEx)
            {
                return (false, $"File error while loading: {ioEx.Message}", new List<Transaction>());
            }
            catch (JsonException jsonEx)
            {
                return (false, $"Data file is corrupted or invalid: {jsonEx.Message}", new List<Transaction>());
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error while loading: {ex.Message}", new List<Transaction>());
            }
        }

        // ─── File Management Helpers 

        
        // Checks whether a saved data file already exists on disk.
        public bool DataFileExists()
        {
            return File.Exists(_filePath);
        }

      
        //Deletes the saved data file if it exists.
        // Returns a result tuple describing the outcome.
        public (bool IsSuccess, string Message) DeleteDataFile()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return (false, "No data file found to delete.");

                File.Delete(_filePath);
                return (true, "Data file deleted successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Could not delete data file: {ex.Message}");
            }
        }
    }

    // ─── DTO for transactions (Data Transfer Object)

    /// <summary>
    /// A plain serializable class used exclusively for JSON read/write.
    /// Records with constructor parameters don't always serialize cleanly,
    /// </summary>
    internal class TransactionDto
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public CategoryType CategoryType { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}

