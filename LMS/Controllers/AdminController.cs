using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NuGet.Packaging;
using System.Security.Claims;
using System.Linq;
using LMS.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Drawing;
using LMS.Models.Data_Models;

namespace LMS.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<IdentityUser> userManager, 
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment webHostEnvironment,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }
        // USERS
        //GET : Admin/UserList
        public async Task<IActionResult> UserList()
        {
          var users = _userManager.Users.ToList();

            var userRoles = new List<UserRoleViewModel>();

            foreach(var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
               foreach(var role in roles)
                {
                    userRoles.Add(new UserRoleViewModel
                    {
                        User = user,
                        Role = role
                    });
                }
            }

            return View(userRoles);
        }

        // GET : Admin/AddUser
        public IActionResult AddUser()
        {
            return View();
        }

        //POST : Admin/AddUser
        [HttpPost]
        public async Task<IActionResult> AddUser(UserViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = new IdentityUser
                {
                    Email = model.Email,
                    UserName = model.FullName
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                    await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, model.FullName));
                    await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.MobilePhone, model.PhoneNumber));
                    return RedirectToAction(nameof(UserList));
                }
                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        //GET: Admin/EditUser/5
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if(user == null)
            {
                return NotFound();
            }

            // Await the task to get the list of claims
            var claims = await _userManager.GetClaimsAsync(user);

            // Extract the specific claims after awaiting
            var fullNameClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var phoneClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.MobilePhone)?.Value;
            var roles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Email = user.Email ?? string.Empty,
                FullName = fullNameClaim ?? string.Empty,
                PhoneNumber = phoneClaim ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty,
            };

            return View(model);
        }

        //POST: Admin/EditUser/5
        [HttpPost]
        public async Task<IActionResult> EditUser(string id, EditUserViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);

                if (user == null)
                {
                    return NotFound();
                }

                user.Email = model.Email;
                user.UserName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRoleAsync(user, roles.FirstOrDefault() ?? String.Empty);
                    await _userManager.AddToRoleAsync(user, model.Role);

                    return RedirectToAction(nameof(UserList));
                }
                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        //GET : Admin/DeleteUser/5
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        //POST : Admin/DeleteUser/id
        [HttpPost, ActionName("DeleteUser")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(UserList));
            }

            ModelState.AddModelError(string.Empty, "Failed to delete user.");
            return View(user);
        }

        //COURSES
        //GET : Courses
        public async Task<IActionResult> CourseList()
        {
            var courses = await _context.Courses.Include(c => c.Teacher).ToListAsync();
            return View(courses);
        }

        //GET : Admin/AddCourse
        public IActionResult AddCourse()
        {
            ViewBag.Teachers = new SelectList(_userManager.GetUsersInRoleAsync("Teacher").Result, "Id", "UserName");
            return View();
        }

        //POST : Admin/AddCourse
        [HttpPost]
        public async Task<IActionResult> AddCourse(Course course, IFormFile image)
        {
            if (ModelState.IsValid)
            {
                //Handle course image profile 
                if (image != null)
                {
                    string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "course_images");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                    string filePath = Path.Combine(uploadDir, uniqueFileName);
                    await using(var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(fileStream);
                    }
                    course.ImagePath = "/course_images/" + uniqueFileName;
                }

                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(CourseList));
            }
            ViewBag.Teachers = new SelectList(_userManager.GetUsersInRoleAsync("Teacher").Result, "Id", "UserName", course.TeacherId);
            return View(course);
        }

        //GET : Admin/EditCourse/5
        public async Task<IActionResult> EditCourse(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            ViewBag.Teachers = new SelectList(_userManager.GetUsersInRoleAsync("Teacher").Result, "Id", "UserName", course.TeacherId);
            return View(course);
        }

        //POST : Admin/EditCourse/5
        [HttpPost]
        public async Task<IActionResult> EditCourse(int id, Course course, IFormFile image)
        {
            if (id != course.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    if (image != null)
                    {
                        string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "course_images");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                        string filePath = Path.Combine(uploadDir, uniqueFileName);
                        await using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(fileStream);
                        }
                        course.ImagePath = "/course_images/" + uniqueFileName;
                    }
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch(DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(CourseList));
            }
            ViewBag.Teachers = new SelectList(_userManager.GetUsersInRoleAsync("Teacher").Result, "Id", "UserName", course.TeacherId);
            return View(course);
        }

        //GET : Admin/DeleteCourse/5
        [HttpGet]
        public async Task<IActionResult> DeleteCourse(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var course = await _context.Courses.Include(c => c.Teacher)
                .FirstOrDefaultAsync(m => m.Id == id);
            if(course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        //POST : Admin/DeleteCourse/5
        [HttpPost, ActionName(nameof(DeleteCourse))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(CourseList));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        //VIEWMODELS
        public class UserViewModel
        {
            public string Email { get; set; }
            public string FullName  { get; set; }
            public string PhoneNumber { get; set; }
            public string Password { get; set; }
            public string Role { get; set; }
        }

        public class EditUserViewModel
        {
            public string Id { get; set; }
            public string Email { get; set; }
            public string FullName { get; set; }
            public string PhoneNumber { get; set; }
            public string Role { get; set; }
        }

        public class UserRoleViewModel
        {
            public IdentityUser User { get; set; }
            public string Role { get; set; }
        }
    }
}
