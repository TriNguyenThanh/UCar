using System;
using System.Collections.Generic;

namespace UCar.TempModels;

public partial class Customer
{
    public Guid CustomerId { get; set; }

    public Guid UserId { get; set; }

    public string FullName { get; set; } = null!;

    public DateTime? Dob { get; set; }

    public string? AddressText { get; set; }

    public string? RiskLevel { get; set; }

    public bool IsBlacklisted { get; set; }

    public DateTime CreatedAt { get; set; }
}
