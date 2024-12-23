using Code.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using MailKit.Net.Smtp;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Mail;

namespace Code.Controllers
{
    public class UserController : Controller
    {
        private readonly MyDbContext _db;

        public UserController(MyDbContext db)
        {
            _db = db;
        }

        // GET: User
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            var user = _db.Users.FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserName", user.Name);

                // Check if the user already has a cart
                var cartExists = _db.ShoppingCarts.Any(c => c.UserId == user.Id);
                if (!cartExists)
                {
                    // Create a new cart for the user
                    var cart = new ShoppingCart
                    {
                        UserId = user.Id,
                        CreatedAt = DateTime.Now
                    };
                    _db.ShoppingCarts.Add(cart);
                    _db.SaveChanges();
                }

                return RedirectToAction("Index", "Home");
            }

            ViewBag.ErrorMessage = "Invalid email or password.";
            return View();
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (_db.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("", "Email already in use.");
                    return View(model);
                }

                var user = new User
                {
                    Name = model.Name,
                    Email = model.Email,
                    Password = model.Password,
                    Image = "Default.png"
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();
                return RedirectToAction("Login", "User");
            }

            return View(model);
        }

        //public ActionResult Logout()
        //{
        //    HttpContext.Session.Clear();
        //    return RedirectToAction("Index", "Home");
        //}

        public ActionResult Profile(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var user = _db.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(User model)
        {
            if (ModelState.IsValid)
            {
                var user = _db.Users.Find(model.Id);
                if (user != null)
                {
                    user.Name = model.Name;
                    user.Email = model.Email;
                    user.Password = model.Password;
                    user.Image = model.Image ?? "Default.png";

                    _db.Entry(user).State = EntityState.Modified;
                    _db.SaveChanges();
                    return RedirectToAction("Index", "Home");
                }
            }

            return View(model);
        }

        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ForgotPassword(string email)
        {
            var user = _db.Users.FirstOrDefault(u => u.Email == email);
            if (user != null)
            {
                HttpContext.Session.SetInt32("UserID", user.Id);

                Random rand = new Random();
                string otp = rand.Next(100000, 1000000).ToString();
                HttpContext.Session.SetString("otp", otp);

                try
                {
                    SendEmail(user.Name, user.Email, otp);
                }
                catch (Exception)
                {
                    ViewBag.emailError = "An error occurred while sending the email. Please try again later.";
                    return View();
                }

                return RedirectToAction("SetNewPassword");
            }
            else
            {
                ViewBag.emailError = "Invalid Email";
                return View();
            }
        }

        public ActionResult SetNewPassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SetNewPassword(string otp, string newPassword, string confirmNewPassword)
        {
            if (HttpContext.Session.GetString("otp") == otp && newPassword == confirmNewPassword)
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                if (userId.HasValue)
                {
                    var user = _db.Users.Find(userId.Value);
                    if (user != null)
                    {
                        user.Password = newPassword;
                        _db.Entry(user).State = EntityState.Modified;
                        _db.SaveChanges();
                        return RedirectToAction("Login", "User");
                    }
                }
            }

            ViewBag.error = "Invalid OTP or passwords don't match!";
            return View();
        }

        private void SendEmail(string userName, string userEmail, string otp)
        {
            string fromEmail = "techlearnhub.contact@gmail.com";
            string fromName = "Support Team";
            string subject = "Your OTP Code";
            string body = $@"
        <html>
        <body>
            <h2>Hello {userName},</h2>
            <p><strong>Your OTP code is {otp}. This code is valid for a short period of time.</strong></p>
            <p>If you have any questions or need additional assistance, please feel free to contact our support team.</p>
            <p>Best wishes,<br>Support Team</p>
        </body>
        </html>";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(userName, userEmail)); // Provide both display name and email
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                try
                {
                    client.Connect("smtp.gmail.com", 465, true); // Use SSL (port 465)
                    client.Authenticate(fromEmail, "your_password_here");
                    client.Send(message);
                    client.Disconnect(true);
                }
                catch (Exception ex)
                {
                    // Handle the exception (e.g., log it or show a message)
                    Console.WriteLine($"Email sending failed: {ex.Message}");
                }
            }
        }

    }
}
