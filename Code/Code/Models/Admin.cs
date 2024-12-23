using System;
using System.Collections.Generic;

namespace Code.Models;

public partial class Admin
{
    public int AdminId { get; set; }

    public string? AdminName { get; set; }

    public string? Email { get; set; }

    public string? PasswordHash { get; set; }
}
