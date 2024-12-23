using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Code.Models;

namespace Code.Controllers
{
    public class AdminsController : Controller
    {
        private readonly MyDbContext _context;

        public AdminsController(MyDbContext context)
        {
            _context = context;
        }
        // GET: api/Users
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var Users = await _context.Users.ToListAsync();
            return View(Users);
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> Users(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // Categories CRUD
        public IActionResult CategoriesList()
        {
            var categories = _context.Categories.ToList();
            return View(categories);
        }

        public IActionResult Index()
        {
            return View(Index);
        }

        public IActionResult EditCategory(int? id)
        {
            var category = id == null ? new Category() : _context.Categories.Find(id);
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveCategory(Category category)
        {
            if (!ModelState.IsValid)
                return View("EditCategory", category);

            if (category.Id == 0)
                _context.Categories.Add(category);
            else
                _context.Categories.Update(category);

            _context.SaveChanges();
            return RedirectToAction("CategoriesList");
        }

        public IActionResult DeleteCategory(int id)
        {
            var category = _context.Categories.Find(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
            }
            return RedirectToAction("CategoriesList");
        }

        // Products CRUD
        public IActionResult ProductsList()
        {
            var products = _context.Products.Include(p => p.Category).Include(p => p.Store).ToList();
            return View(products);
        }

        public IActionResult EditProduct(int? id)
        {
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Stores = _context.Stores.ToList();
            var product = id == null ? new Product() : _context.Products.Find(id);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
    
        public IActionResult SaveProduct(Product product, IFormFile Image)
        {
            // Check if the ModelState is valid
            if (!ModelState.IsValid)
            {
                // Re-populate dropdown lists for categories and stores
                ViewBag.Categories = _context.Categories.ToList();
                ViewBag.Stores = _context.Stores.ToList();
                return View("EditProduct", product);
            }

            // Handle the uploaded image file
            if (Image != null && Image.Length > 0)
            {
                // Define the upload path
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder); // Ensure the uploads directory exists
                }

                // Generate a unique file name
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(Image.FileName);

                // Combine the upload path and file name
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file to the server
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    Image.CopyTo(fileStream);
                }

                // Update the product's ImageUrl property
                product.ImageUrl = uniqueFileName;
            }

            // Handle adding or updating the product
            if (product.Id == 0)
            {
                // New product
                _context.Products.Add(product);
            }
            else
            {
                // Existing product
                var existingProduct = _context.Products.AsNoTracking().FirstOrDefault(p => p.Id == product.Id);
                if (existingProduct != null)
                {
                    // Retain the existing image if no new image is uploaded
                    product.ImageUrl ??= existingProduct.ImageUrl;
                }
                _context.Products.Update(product);
            }

            try
            {
                // Save changes to the database
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving product: {ex.Message}");
                // Optionally, add error handling to display a friendly message in the UI
            }

            return RedirectToAction("ProductsList");
        }
        [HttpGet]
public IActionResult EditProduct(int id)
{
    var product = _context.Products.FirstOrDefault(p => p.Id == id);
    if (product == null)
    {
        return NotFound();
    }

    ViewBag.Categories = _context.Categories.ToList();
    ViewBag.Stores = _context.Stores.ToList();
    return View(product);
}


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddProduct(Product product, IFormFile Image)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _context.Categories.ToList();
                ViewBag.Stores = _context.Stores.ToList();
                return View(product);
            }

            // Handle image upload
            if (Image != null && Image.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(Image.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    Image.CopyTo(fileStream);
                }

                product.ImageUrl = uniqueFileName;
            }

            // Add the product to the database
            _context.Products.Add(product);
            _context.SaveChanges();

            return RedirectToAction("ProductsList");
        }
        public IActionResult DeleteProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
            }
            return RedirectToAction("ProductsList");
        }

        // Stores CRUD
        public IActionResult StoresList()
        {
            var stores = _context.Stores.ToList();
            return View(stores);
        }

        public IActionResult EditStore(int? id)
        {
            var store = id == null ? new Store() : _context.Stores.Find(id);
            return View(store);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveStore(Store store)
        {
            if (!ModelState.IsValid)
                return View("EditStore", store);

            if (store.Id == 0)
                _context.Stores.Add(store);
            else
                _context.Stores.Update(store);

            _context.SaveChanges();
            return RedirectToAction("StoresList");
        }

        public IActionResult DeleteStore(int id)
        {
            var store = _context.Stores.Find(id);
            if (store != null)
            {
                _context.Stores.Remove(store);
                _context.SaveChanges();
            }
            return RedirectToAction("StoresList");
        }

        // Orders CRUD
        public IActionResult OrdersList()
        {
            var orders = _context.Orders.Include(o => o.User).Include(o => o.OrderItems).ThenInclude(oi => oi.Product).ToList();
            return View(orders);
        }

        public IActionResult EditOrder(int? id)
        {
            var order = id == null ? new Order() : _context.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.Id == id);
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveOrder(Order order)
        {
            if (!ModelState.IsValid)
                return View("EditOrder", order);

            if (order.Id == 0)
                _context.Orders.Add(order);
            else
                _context.Orders.Update(order);

            _context.SaveChanges();
            return RedirectToAction("OrdersList");
        }

        public IActionResult DeleteOrder(int id)
        {
            var order = _context.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.Id == id);
            if (order != null)
            {
                _context.OrderItems.RemoveRange(order.OrderItems);
                _context.Orders.Remove(order);
                _context.SaveChanges();
            }
            return RedirectToAction("OrdersList");
        }
    }
}
