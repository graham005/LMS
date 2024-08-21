using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NuGet.Packaging;
using System.Security.Claims;
using System.Linq;

namespace LMS.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> UserList()
        {
            var users = await _userManager.GetUsersInRoleAsync("Teacher");
            users.AddRange(await _userManager.GetUsersInRoleAsync("Student"));

            var userRoles = new List<UserRoleViewModel>();

            foreach(var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles.Add(new UserRoleViewModel
                {
                    User = user,
                    Role = roles.FirstOrDefault() ?? String.Empty
                });
            }

            return View(users);
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

            var model = new UserViewModel
            {
                Email = user.Email ?? string.Empty,
                FullName = fullNameClaim ?? string.Empty,
                PhoneNumber = phoneClaim ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty,
            };

            return View(model);
        }

        //POST: Admin/EditUser/5
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
