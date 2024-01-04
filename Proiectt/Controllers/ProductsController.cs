
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Proiect.Data;
using Proiect.Models;
using System;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;


namespace Proiect.Controllers
{
    public class ProductsController : Controller
    {

        private readonly ApplicationDbContext db;
        private IWebHostEnvironment _env;
        private string? databaseFileName;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

      
        public ProductsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
         IWebHostEnvironment env
        )
        {
            _env = env;
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index(string sortOrder, string sortDirection)
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            ViewData["CurrentSortOrder"] = sortOrder;
            ViewData["CurrentSortDirection"] = sortDirection;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            var products = db.Products.Where(a => a.Active == true)
                   .Include("Category")
                 .Include("ApplicationUser");

            foreach (var p in products)
            {
                double? d = db.Reviews.Where(r => r.ProductId == p.Id).Average(r => r.Rating);
                p.Rating = (int?)d;

            }

            db.SaveChanges();
            var search = "";
            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim();

                List<int> productIds = db.Products
                                    .Where(
                                        at => at.Title.Contains(search)

                                    ).Select(a => a.Id).ToList();



                products = db.Products.Where(pro =>
                            productIds.Contains(pro.Id))
                            .Include("Category")
                            .Include("ApplicationUser")

                          .OrderBy(a => a.Date);

            }

            ViewBag.SearchString = search;

            // AFISARE PAGINATA


            int _perPage = 3;
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"].ToString();
                ViewBag.Alert = TempData["messageType"];
            }

            // Fiind un numar variabil de produse, verificam de fiecare data utilizand metoda Count()
            int totalItems = products.ToList().Count();


            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);


            var offset = 0;

            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;

            }


            switch (sortOrder)
            {
                case "Price":
                    products = sortDirection == "asc"
                        ? products.OrderBy(p => p.Price)
                        : products.OrderByDescending(p => p.Price);
                    break;
                case "Rating":

                    products = sortDirection == "asc"
                        ? products.OrderBy(p => p.Rating)
                        : products.OrderByDescending(p => p.Rating);
                    break;
                default:
                    products = products.OrderBy(p => p.Date);
                    break;
            }

            var paginatedArticles = products.Skip(offset).Take(_perPage);

            ViewBag.Products = paginatedArticles;


            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);


            if (search != "")
            {
                ViewBag.PaginationBaseUrl = $"/Products/Index?sortOrder={sortOrder}&sortDirection={sortDirection}&search={search}&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = $"/Products/Index?sortOrder={sortOrder}&sortDirection={sortDirection}&page";
            }

            return View();

        }




        [Authorize(Roles = "Editor")]
        public IActionResult New()
        {
            Product p = new Product();
            p.Categ = GetAllCategories();
            return View(p);
        }

        [Authorize(Roles = "Editor")]
        [HttpPost]
        public async Task<IActionResult> New(Product p, IFormFile? ProductPhoto)
        {
            p.ApplicationUserId = _userManager.GetUserId(User);
            p.Date = DateTime.Now;
            p.Active = false;

            if (ModelState.IsValid)
            {
                db.Products.Add(p);
                db.SaveChanges();
                if (ProductPhoto != null && ProductPhoto.Length > 0)
                {

                    var entityPath = Path.Combine(
                                _env.WebRootPath,
                                "images",
                                p.Id.ToString()
                            );

                    var storagePath = Path.Combine(entityPath, ProductPhoto.FileName);
                    Directory.CreateDirectory(entityPath);
                    p.Photo = "/images/" + p.Id.ToString() + "/" + ProductPhoto.FileName;


                    using (var fileStream = new FileStream(storagePath, FileMode.Create))
                    {
                        await ProductPhoto.CopyToAsync(fileStream);
                    }

                    db.SaveChanges();
                }
                
                
                TempData["Message"] = "Prous adaugat cu succes in coada de asteptare. Va rugam asteptati ca un administrator sa aprobe cererea de adaugare";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");

            }
            p.Categ = GetAllCategories();
            return View(p);

        }
        [Authorize(Roles = "Admin")]
        public IActionResult Pending()
        {
            ViewBag.Products = db.Products.Where(_ => _.Active == false).Include("Category").Include("ApplicationUser");

            if (TempData["Message"] != null)
            {
                ViewBag.Message = TempData["Message"];
                ViewBag.Alert = TempData["messageType"];
            }
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Pending(int id)
        {
            var produs = db.Products.Find(id);
            if (produs == null)
            {
                TempData["Message"] = "Produsul nu mai exista";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");

            }
            if (produs.Active == false)
            {
                produs.Active = true;
                db.SaveChanges();
                TempData["Message"] = "Aprobat cu succes";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Pending");
            }

            produs.Active = false;
            db.SaveChanges();
            TempData["Message"] = "Produs ascuns de la clienti";
            TempData["messageType"] = "alert alert-success";
            return RedirectToAction("Index");
        }

        public IActionResult Show(int id)
        {
            var produs = db.Products
                         .Include("ApplicationUser")
                         .Include("Category")
                         .Include("Reviews")
                          .Include("Reviews.ApplicationUser")
                         .Where(c => c.Id == id).First();

            if (produs.Reviews != null && produs.Reviews.Any())
            {
                double averageRating = produs.Reviews.Average(r => r.Rating ?? 0); // Utilizarea operatorului coalescent pentru a trata valorile nullable
                produs.Rating = (int?)(double)averageRating;
            }


            else
            {
                produs.Rating = null; // Dacă nu există comentarii, setați Rating la null sau la o valoare implicită, în funcție de necesități
            }


            if (TempData["message"] != null)
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }
            if (produs == null)
            {
                TempData["Message"] = "Produsul nu exista in Baza de Date";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");

            }
            GetButtonRights();
            if (User.IsInRole("Admin"))
            {
                ViewBag.CanToggleStatus = true;
                return View(produs);
            }
            if (produs.Active == true)
            {
                ViewBag.CanToggleStatus = false;
                return View(produs);
            }


            TempData["Message"] = "Nu aveti acces la aceasta resursa";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Index");

        }


        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Show([FromForm] Review review)
        {
            review.Date = DateTime.Now;
            review.ApplicationUserId = _userManager.GetUserId(User);

            var existingReview = db.Reviews
      .FirstOrDefault(r => r.ProductId == review.ProductId &&
                           r.ApplicationUserId == _userManager.GetUserId(User));

            if (existingReview == null)
            {
                // Utilizatorul nu a mai scris o recenzie, permite adaugarea
                if (ModelState.IsValid)
                {
                    db.Reviews.Add(review);
                    db.SaveChanges();
                    return Redirect("/Products/Show/" + review.ProductId);
                }
            }

            else
            {
                // Utilizatorul a scris deja o recenzie, poate fi redirectionat la editare sau afisat un mesaj de eroare
                TempData["message"] = "Ati scris deja o recenzie pentru acest produs.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Products");
            }

                Product prod = db.Products.Include("Category")
                         .Include("ApplicationUser")
                         .Include("Reviews")
                           .Include("Reviews.ApplicationUser")
                         .Where(prod => prod.Id == review.ProductId)
                         .FirstOrDefault();


                ViewBag.UserOrders = db.Orders
                                          .Where(b => b.UserId == _userManager.GetUserId(User))
                                          .ToList();

                GetButtonRights();

                return View(prod);
            
        }


        [Authorize(Roles = "Editor,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var pr = db.Products.Include("Category").Where(db => db.Id == id).First();
            pr.Categ = GetAllCategories();
            if (User.IsInRole("Admin") || _userManager.GetUserId(User) == pr.ApplicationUserId)
            {
                return View(pr);
            }
            TempData["Message"] = "Nu ai dreptul la aceasta resursa";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Editor,Admin")]
        public async Task<IActionResult> Edit(int id, Product p, IFormFile? ProductPhoto)
        {
            Product query = db.Products.Find(id);
           
                if (User.IsInRole("Admin") || _userManager.GetUserId(User) == query.ApplicationUserId)
                {
                    if (ModelState.IsValid)
                    {

                        if (ProductPhoto != null && ProductPhoto.Length > 0)
                        {
                        string cacheBuster = DateTime.Now.Ticks.ToString();//probleme de caching cand folosesc firefox: poza nu isi da update in front-end cand ii dau updte in back-end trebui deci un cache buster care schimba numele fisierului
                            var entityPath = Path.Combine(
                                _env.WebRootPath,
                                "images",
                                query.Id.ToString()
                            );

                            var storagePath = Path.Combine(entityPath, cacheBuster+ProductPhoto.FileName);

                        if (Directory.Exists(entityPath))
                        {
                            Directory.Delete(entityPath, true);
                        }
                            Directory.CreateDirectory(entityPath);


                            query.Photo = "/images/" + query.Id.ToString()+"/"+cacheBuster+ProductPhoto.FileName;


                            using (var fileStream = new FileStream(storagePath, FileMode.Create))
                            {
                                await ProductPhoto.CopyToAsync(fileStream);
                            }

                        }
                        query.CategoryId = p.CategoryId;
                        query.Title = p.Title;
                        query.Description = p.Description;
                        query.Price = p.Price;


                        db.SaveChanges();
                        TempData["Message"] = "Prous editat cu succes.";
                        TempData["messageType"] = "alert-success";
                        return RedirectToAction("Index");

                    }
                    p.Categ = GetAllCategories();
                    return View(p);

                }

                TempData["Message"] = "Nu ai dreptul la aceasta resursa";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");


        }

        [HttpPost]
        public IActionResult AddProduct([FromForm] ProductOrder productOrder)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Redirect("/Identity/Account/Register");
            }

            var cart = db.Orders.Where(or => or.IsCart == true && or.UserId == _userManager.GetUserId(User));

            // creează coșul dacă nu există
            if (!cart.Any())
            {
                db.Orders.Add(new Order
                {
                    UserId = _userManager.GetUserId(User),
                    IsCart = true,

                });
                db.SaveChanges();

                cart = db.Orders.Where(or => or.IsCart == true && or.UserId == _userManager.GetUserId(User));
            }

            var orderId = cart.First().OrderId;

            Product p = db.Products.Find(productOrder.ProductId);
            if (p.Active == false)
            {
                TempData["message"] = "Produsul nu se mai vinde";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = productOrder.ProductId });

            }
            if (ModelState.IsValid && productOrder.ProductId > 0)
            {

                productOrder.OrderId = orderId;


                db.ProductOrders.Add(productOrder);
                db.SaveChanges();

                TempData["message"] = "Produsul a fost adaugat in cosul dumneavostra";
                TempData["messageType"] = "alert-success";
            }
            else
            {
                TempData["message"] = "Date invalide pentru adaugarea produsului in cos";
                TempData["messageType"] = "alert-danger";
            }

            return RedirectToAction("Show", new { id = productOrder.ProductId });
        }





        [HttpPost]
        [Authorize(Roles = "Editor,Admin")]
        public ActionResult Delete(int id)
        {
            try
            {

                var query = db.Products.Find(id);
                if (User.IsInRole("Admin") || query.ApplicationUserId == _userManager.GetUserId(User))
                {
                    db.Products.Remove(query);
                    db.SaveChanges();
                    TempData["Message"] = "Produs sters cu succes";
                    TempData["messageType"] = "alert alert-success";
                    return RedirectToAction("Index");
                }
                TempData["Message"] = "Nu ai dreptul la resursa asta";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Message"] = "a aparut o eroare; poate resursa este deja stearsa";
                TempData["messageType"] = " alert-danger";
                return RedirectToAction("Index");
            }
        }

        [NonAction]
        private void GetButtonRights()
        {
            ViewBag.IsAdmin = User.IsInRole("Admin");

            ViewBag.UserId = _userManager.GetUserId(User);
            ViewBag.IsEditor = User.IsInRole("Editor");

        }

        [NonAction]
        private IEnumerable<SelectListItem> GetAllCategories()
        {
            var c = new List<SelectListItem>();

            var query = db.Categories;
            foreach (Category cat in query)
            {
                c.Add(new SelectListItem { Value = cat.Id.ToString(), Text = cat.CategoryName });
            }

            return c;
        }
    }
}