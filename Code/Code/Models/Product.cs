using System;
using System.Collections.Generic;

namespace Code.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public int Quantity { get; set; }

    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public string? StoreName { get; set; }

    public int? StoreId { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductStore> ProductStores { get; set; } = new List<ProductStore>();

    public virtual ICollection<ShoppingCartItem> ShoppingCartItems { get; set; } = new List<ShoppingCartItem>();

    public virtual Store? Store { get; set; }
}
