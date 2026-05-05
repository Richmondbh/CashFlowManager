using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CashFlowManager.Models
{
  
    // Represents an immutable transaction category with a name and type.
    public record Category(string Name, CategoryType Type);
}