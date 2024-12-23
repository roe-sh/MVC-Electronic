using Code.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Code.Controllers
{
    public class ShopController : Controller
    {
        private readonly MyDbContext _db;

        public ShopController(MyDbContext db)
        {
            _db = db;
        }

        // Categories
        public IActionResult Categories()
        {
            var categories = _db.Categories.ToList();
            return View(categories);
        }

        // Products by Category
        public IActionResult ProductsByCategory(int categoryId)
        {
            var products = _db.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId)
                .ToList();
            return View(products);
        }

        // Product Details
        public IActionResult Details(int id)
        {
            var product = _db.Products
                .Include(p => p.Category)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.StoreName = _db.Stores.FirstOrDefault(s => s.Id == product.StoreId)?.Name;

            // Fetch related products
            ViewBag.RelatedProducts = _db.Products
                .Where(p => p.CategoryId == product.CategoryId && p.StoreId != product.StoreId)
                .Take(4)
                .ToList();

            return View("~/Views/Home/Details.cshtml", product);
        }

        // Cart
        public IActionResult Cart()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "User");
            }

            var cart = _db.ShoppingCarts
    .Include(c => c.ShoppingCartItems)
    .ThenInclude(ci => ci.Product)
    .ThenInclude(p => p.Category)
    .Include(c => c.ShoppingCartItems)
    .ThenInclude(ci => ci.Product)
    .ThenInclude(p => p.Store)
    .FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                ViewBag.TotalAmount = 0;
                return View("~/Views/Home/Cart.cshtml", new List<ShoppingCartItem>());
            }

            // Ensure ViewBag.TotalAmount is properly set
            ViewBag.TotalAmount = cart.ShoppingCartItems
                .Sum(item => item.Quantity * (item.Product?.Price ?? 0));

            return View("~/Views/Home/Cart.cshtml", cart.ShoppingCartItems);

        }

        [HttpPost]
        public IActionResult AddToCart(int productId, int storeId, int quantity)
        {
            // Validate the quantity
            if (quantity <= 0)
            {
                TempData["Error"] = "Quantity must be greater than 0.";
                return RedirectToAction("Cart");
            }

            // Get the logged-in user ID
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "User");
            }

            // Ensure the product and store exist
            var product = _db.Products.FirstOrDefault(p => p.Id == productId && p.StoreId == storeId);
            if (product == null)
            {
                TempData["Error"] = "Invalid product or store.";
                return RedirectToAction("Cart");
            }

            // Retrieve or create the user's cart
            var cart = _db.ShoppingCarts.FirstOrDefault(c => c.UserId == userId.Value);
            if (cart == null)
            {
                cart = new ShoppingCart
                {
                    UserId = userId.Value,
                    CreatedAt = DateTime.Now
                };
                _db.ShoppingCarts.Add(cart);
                _db.SaveChanges();
            }

            // Add or update the cart item
            var cartItem = _db.ShoppingCartItems
                .FirstOrDefault(ci => ci.CartId == cart.CartId && ci.ProductId == productId && ci.StoreId == storeId);

            if (cartItem != null)
            {
                // Update the quantity if the item already exists in the cart
                cartItem.Quantity += quantity;
                _db.Entry(cartItem).State = EntityState.Modified;
            }
            else
            {
                // Add a new cart item
                cartItem = new ShoppingCartItem
                {
                    CartId = cart.CartId,
                    ProductId = productId,
                    StoreId = storeId,
                    Quantity = quantity,
                    CreatedAt = DateTime.Now
                };
                _db.ShoppingCartItems.Add(cartItem);
            }

            // Save changes to the database
            _db.SaveChanges();

            TempData["Success"] = "Item added to the cart successfully.";
            return RedirectToAction("Cart");
        }

        [HttpPost]
        public IActionResult UpdateCartItem(int cartItemId, string operation)
        {
            var cartItem = _db.ShoppingCartItems.FirstOrDefault(ci => ci.CartItemId == cartItemId);

            if (cartItem != null)
            {
                if (operation == "increase")
                {
                    cartItem.Quantity++;
                }
                else if (operation == "decrease" && cartItem.Quantity > 1)
                {
                    cartItem.Quantity--;
                }

                _db.SaveChanges();
            }

            return RedirectToAction("Cart");
        }

        [HttpPost]
        public IActionResult RemoveCartItem(int cartItemId)
        {
            var cartItem = _db.ShoppingCartItems.FirstOrDefault(ci => ci.CartItemId == cartItemId);

            if (cartItem != null)
            {
                _db.ShoppingCartItems.Remove(cartItem);
                _db.SaveChanges();
            }

            return RedirectToAction("Cart");
        }

        // Checkout
        [HttpPost]
        public IActionResult Checkout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "User");
            }

            var cart = _db.ShoppingCarts
                .Include(c => c.ShoppingCartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null || !cart.ShoppingCartItems.Any())
            {
                return RedirectToAction("Cart");
            }

            var order = new Order
            {
                UserId = userId.Value,
                OrderDate = DateTime.Now,
                TotalAmount = cart.ShoppingCartItems.Sum(item => item.Quantity * item.Product.Price),
                Status = "Pending"
            };

            _db.Orders.Add(order);
            _db.SaveChanges();

            foreach (var item in cart.ShoppingCartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    StoreId = item.StoreId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                };

                _db.OrderItems.Add(orderItem);
            }

            _db.ShoppingCartItems.RemoveRange(cart.ShoppingCartItems);
            _db.ShoppingCarts.Remove(cart);
            _db.SaveChanges();

            return View("~/Views/Home/Checkout.cshtml", order);
        }

        // Order Details
        public IActionResult OrderDetails(int orderId)
        {
            var order = _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Store)
                .FirstOrDefault(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}
