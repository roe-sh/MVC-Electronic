using System.Diagnostics;
using Code.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Code.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MyDbContext _db;

        public HomeController(ILogger<HomeController> logger, MyDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Shop(string searchTerm = "", int categoryId = 0, decimal minPrice = 0, decimal maxPrice = 10000, int page = 1, int pageSize = 8)
        {
            var categories = _db.Categories.ToList();
            var filteredProducts = _db.Products
                .Include(p => p.Category)
                .Where(p =>
                    (string.IsNullOrEmpty(searchTerm) || p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm)) &&
                    (categoryId == 0 || p.CategoryId == categoryId) &&
                    p.Price >= minPrice && p.Price <= maxPrice)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var model = new Dictionary<Category, List<Product>>();
            foreach (var category in categories)
            {
                var categoryProducts = filteredProducts.Where(p => p.CategoryId == category.Id).ToList();
                model.Add(category, categoryProducts);
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)_db.Products.Count() / pageSize);
            ViewBag.Categories = categories;

            return View(model);
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult ProductDetails(int id)
        {
            var product = _db.Products
                .Include(p => p.Category)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        public IActionResult Cart()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var shoppingCart = _db.ShoppingCarts.Include(c => c.ShoppingCartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefault(m => m.UserId == userId.Value);

            if (shoppingCart == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var totalAmount = shoppingCart.ShoppingCartItems.Sum(item => item.Quantity * item.Product.Price);
            ViewBag.TotalAmount = totalAmount;

            return View(shoppingCart.ShoppingCartItems);
        }

        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                return RedirectToAction("Shop");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var cart = _db.ShoppingCarts.FirstOrDefault(c => c.UserId == userId.Value);
            if (cart == null)
            {
                cart = new ShoppingCart { UserId = userId.Value, CreatedAt = DateTime.Now };
                _db.ShoppingCarts.Add(cart);
                _db.SaveChanges();
            }

            var cartItem = _db.ShoppingCartItems
                .FirstOrDefault(ci => ci.CartId == cart.CartId && ci.ProductId == productId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cartItem = new ShoppingCartItem
                {
                    CartId = cart.CartId,
                    ProductId = productId,
                    Quantity = quantity,
                    CreatedAt = DateTime.Now
                };
                _db.ShoppingCartItems.Add(cartItem);
            }

            _db.SaveChanges();
            return RedirectToAction("Cart");
        }

        [HttpPost]
        public IActionResult DeleteItem(int id)
        {
            var item = _db.ShoppingCartItems.SingleOrDefault(i => i.StoreId == id);
            if (item != null)
            {
                _db.ShoppingCartItems.Remove(item);
                _db.SaveChanges();
            }
            return RedirectToAction("Cart");
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int id, string operation)
        {
            var item = _db.ShoppingCartItems.FirstOrDefault(ci => ci.StoreId== id);

            if (item != null)
            {
                if (operation == "increase")
                {
                    item.Quantity++;
                }
                else if (operation == "decrease" && item.Quantity > 1)
                {
                    item.Quantity--;
                }

                _db.SaveChanges();
            }

            return RedirectToAction("Cart");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
