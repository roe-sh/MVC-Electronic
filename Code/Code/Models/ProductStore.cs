using System;
using System.Collections.Generic;

namespace Code.Models;

public partial class ProductStore
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int StoreId { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Store Store { get; set; } = null!;
}
