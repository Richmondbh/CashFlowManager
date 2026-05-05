using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CashFlowManager.Models
{
 
    // Represents an immutable financial transaction.
    public record Transaction(
        DateTime Date,
        decimal Amount,
        Category Category,
        string Description);
}