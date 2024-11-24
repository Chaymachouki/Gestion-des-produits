using AppDb.Models;
using AppDb.Models.Repositories;
using AppDb.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
namespace AppDb.Controllers
{
    public class ProductController : Controller
    {
        readonly IRepository<Product> SqlProductRepository;
        private readonly IWebHostEnvironment hostingEnvironment;
        public ProductController(IRepository<Product> ProdRepository, IWebHostEnvironment hostingEnvironment)
        {
            SqlProductRepository = ProdRepository;
            this.hostingEnvironment = hostingEnvironment;
        }

        // GET: ProductController
        public ActionResult Index()
        {
            var Products = SqlProductRepository.GetAll();
            return View(Products);
        }

        // GET: ProductController/Details/5
        public ActionResult Details(int id)
        {
            var Product = SqlProductRepository.Get(id);
            return View(Product);
        }

        // GET: ProductController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ProductController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                string uniqueFileName = null;
                if (model.ImagePath != null)
                {
                    string uploadsFolder = Path.Combine(hostingEnvironment.WebRootPath, "images");
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImagePath.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    model.ImagePath.CopyTo(new FileStream(filePath, FileMode.Create));
                }
                Product newProduct = new Product
                {
                    Désignation = model.Désignation,
                    Prix = model.Prix,
                    Quantite = model.Quantite,
                    Image = uniqueFileName
                };
                SqlProductRepository.Add(newProduct);
                return RedirectToAction("details", new { id = newProduct.Id });
            }
            return View();
        }

        // GET: ProductController/Edit/5
        public ActionResult Edit(int id)
        {
            Product product = SqlProductRepository.Get(id);
            EditViewModel productEditViewModel = new EditViewModel
            {
                Id = product.Id,
                Désignation = product.Désignation,
                Prix = product.Prix,
                Quantite = product.Quantite,
                ExistingImagePath = product.Image
            };
            return View(productEditViewModel);
        }

        // POST: ProductController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EditViewModel model)
        {
            if (ModelState.IsValid)
            {
                Product product = SqlProductRepository.Get(model.Id);
                product.Désignation = model.Désignation;
                product.Prix = model.Prix;
                product.Quantite = model.Quantite;

                if (model.ImagePath != null)
                {
                    if (model.ExistingImagePath != null)
                    {
                        string filePath = Path.Combine(hostingEnvironment.WebRootPath, "images", model.ExistingImagePath);

                        if (System.IO.File.Exists(filePath))
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            System.IO.File.Delete(filePath);
                        }
                    }
                    product.Image = ProcessUploadedFile(model);
                }

                Product updatedProduct = SqlProductRepository.Update(product);
                if (updatedProduct != null)
                    return RedirectToAction("Index");
                else
                    return NotFound();
            }
            return View(model);
        }

        [NonAction]
        private string ProcessUploadedFile(EditViewModel model)
        {
            string uniqueFileName = null;
            if (model.ImagePath != null)
            {
                string uploadsFolder = Path.Combine(hostingEnvironment.WebRootPath, "images");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImagePath.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    model.ImagePath.CopyTo(fileStream);
                }
            }
            return uniqueFileName;
        }

        public IActionResult Search(string term)
        {
            var products = SqlProductRepository.Search(term);

            if (products == null || !products.Any())
            {
                TempData["NoResults"] = "Aucun produit trouvé avec ce terme.";
            }

            return View(products);
        }

        // GET: ProductController/Delete/5
        public ActionResult Delete(int id)
        {
            var product = SqlProductRepository.Get(id);
            return View(product);
        }

        [HttpPost, ActionName("ProductDeleted")]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(Product P)
        {
            var Pr = SqlProductRepository.Get(P.Id);
            if (Pr == null)
            {
                return NotFound();
            }

            if (Pr.Image != null)
            {
                string filePath = Path.Combine(hostingEnvironment.WebRootPath, "images", Pr.Image);
                if (System.IO.File.Exists(filePath))
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.IO.File.Delete(filePath);
                }
            }

            SqlProductRepository.Delete(P.Id);
            return RedirectToAction("Index");
        }
    }
}
