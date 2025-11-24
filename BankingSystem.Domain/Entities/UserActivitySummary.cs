using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystem.Domain.Entities;

public class UserActivitySummary
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int TotalActions { get; set; }
    public DateTime? FirstAction { get; set; }
    public DateTime? LastAction { get; set; }
    public Dictionary<string, int> ActionCounts { get; set; } = new();
}
