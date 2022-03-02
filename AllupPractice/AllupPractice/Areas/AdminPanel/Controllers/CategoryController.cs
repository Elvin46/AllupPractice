using AllupPractice.Areas.AdminPanel.Data;
using AllupPractice.DataAccessLayer;
using AllupPractice.Models;
using Fiorella.Areas.AdminPanel.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AllupPractice.Areas.AdminPanel.Controllers
{
    [Area("AdminPanel")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _dbContext;

        public CategoryController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _dbContext.Categories.Where(x => x.IsDeleted == false).ToListAsync();
            return View(categories);
        }
        public async Task<IActionResult> Create()
        {
            var parentCategories = await _dbContext.Categories
                .Where(x => x.IsDeleted == false && x.IsMain)
                .ToListAsync();
            ViewBag.ParentCategories = parentCategories;
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category, int parentCategoryId)
        {
            var parentCategories = await _dbContext.Categories
                .Where(x => x.IsDeleted == false && x.IsMain)
                .ToListAsync();
            ViewBag.ParentCategories = parentCategories;
            if (!ModelState.IsValid)
            {
                return View();
            }
            if (category.IsMain)
            {
                var maxSize = 1;
                if (category.Photo == null)
                {
                    ModelState.AddModelError("", "Please Choose Photo");
                    return View();
                }
                if (!category.Photo.IsImage())
                {
                    ModelState.AddModelError("", "Upload Valid Images(png,jpg,jpeg)");
                    return View();
                }
                if (!category.Photo.IsAllowedSize(maxSize))
                {
                    ModelState.AddModelError("", $"This file is too large.Allowed maximum size is {maxSize}mb.");
                    return View();
                }

                var fileName = await category.Photo.GenerateFile(Constants.ImageFolderPath);
                category.Image = fileName;
            }
            else
            {
                if (parentCategoryId == 0)
                {
                    ModelState.AddModelError("", "Please Choose Parent Category");
                    return View();
                }
                var existParentCategory = await _dbContext.Categories
                    .Include(x => x.Children.Where(y => !y.IsDeleted))
                    .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == parentCategoryId);
                if (existParentCategory == null)
                    return NotFound();
                var existChildCategory = existParentCategory.Children.Any(x => x.Name.ToLower() == category.Name.ToLower());
                if (existChildCategory)
                {
                    ModelState.AddModelError("", "This Child Category Already Exist");
                    return View();
                }
                category.Parent = existParentCategory;
            }
            await _dbContext.Categories.AddAsync(category);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            var category = await _dbContext.Categories
                .Where(x => x.Id == id && !x.IsDeleted)
                .Include(x => x.Parent)
                .Include(x => x.Children.Where(y => !y.IsDeleted))
                .FirstOrDefaultAsync();
            if (category == null)
            {
                return Json(new { status = 404 });
            }
            var path = Path.Combine(Constants.ImageFolderPath, category.Image);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
            category.IsDeleted = true;
            if (category.IsMain)
            {
                foreach (var item in category.Children)
                {
                    item.IsDeleted = true;
                }
            }
            await _dbContext.SaveChangesAsync();
            return Json(new { status = 200 });
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            var category = await _dbContext.Categories
                .Where(x => x.Id == id && !x.IsDeleted)
                .Include(x => x.Parent)
                .Include(x => x.Children.Where(y => !y.IsDeleted))
                .FirstOrDefaultAsync();
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }
        public async Task<IActionResult> Update(int? id)
        {
            var parentCategories = await _dbContext.Categories
                .Where(x => x.IsDeleted == false && x.IsMain)
                .ToListAsync();
            ViewBag.ParentCategories = parentCategories;
            if (id == null)
            {
                return BadRequest();
            }
            var category = await _dbContext.Categories
                .Where(x => x.Id == id && !x.IsDeleted)
                .Include(x => x.Parent)
                .Include(x => x.Children.Where(y => !y.IsDeleted))
                .FirstOrDefaultAsync();
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int? id,Category category,int parentCategoryId)
        {
            var parentCategories = await _dbContext.Categories
               .Where(x => x.IsDeleted == false && x.IsMain)
               .ToListAsync();
            ViewBag.ParentCategories = parentCategories;
            if (!ModelState.IsValid)
            {
                return View(category);
            }
            if (id == null)
            {
                return BadRequest();
            }
            if (id != category.Id)
            {
                return BadRequest();
            }
            var existCategory = await _dbContext.Categories
                .Where(x => x.Id == id && !x.IsDeleted)
                .Include(x => x.Parent)
                .Include(x => x.Children.Where(y => !y.IsDeleted))
                .FirstOrDefaultAsync();
            if (existCategory == null)
            {
                return NotFound();
            }
            
            if (existCategory.IsMain)
            {
                if (!category.Photo.IsImage())
                {
                    ModelState.AddModelError("Photo", "It must be photo(jpg,png)");
                    return View(existCategory);
                }
                if (!category.Photo.IsAllowedSize(1))
                {
                    ModelState.AddModelError("Photo", "Size of photo has to be under 1 mb");
                    return View(category);
                }
                var path = Path.Combine(Constants.ImageFolderPath, existCategory.Image);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
                var fileName = await category.Photo.GenerateFile(Constants.ImageFolderPath);
                existCategory.Image = fileName;
            }
            else
            {
                if (parentCategoryId == 0)
                {
                    ModelState.AddModelError("", "Please Choose Parent Category");
                    return View();
                }
                var existParentCategory = await _dbContext.Categories
                    .Include(x => x.Children.Where(y => !y.IsDeleted))
                    .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == parentCategoryId);
                if (existParentCategory == null)
                    return NotFound();
                var existChildCategory = existParentCategory.Children.Any(x => x.Name.ToLower() == category.Name.ToLower());
                if (existChildCategory)
                {
                    ModelState.AddModelError("", "This Child Category Already Exist");
                    return View();
                }
                existCategory.Parent = existParentCategory;
            }

            existCategory.Name = category.Name;
            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
