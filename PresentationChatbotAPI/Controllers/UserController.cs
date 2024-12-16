using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace PresentationChatbotAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize] // Ovo zahteva autentifikaciju za pristup (možeš ukloniti ako nije potrebno)
    public class UserController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager; // Dodaj SignInManager

        public UserController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager) // Dodaj u konstruktor
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager; // Inicijalizacija
        }

        // GET: api/user
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = _userManager.Users;
            return Ok(users);
        }

        // POST: api/user/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] UserDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new IdentityUser { UserName = model.Email, Email = model.Email };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
                return Ok(new { Message = "User created successfully", User = user });

            return BadRequest(result.Errors);
        }

        // POST: api/user/assign-role
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] RoleAssignmentDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return NotFound("User not found");

            if (!await _roleManager.RoleExistsAsync(model.Role))
                return NotFound("Role does not exist");

            var result = await _userManager.AddToRoleAsync(user, model.Role);
            if (result.Succeeded)
                return Ok(new { Message = "Role assigned successfully" });

            return BadRequest(result.Errors);
        }

        // DELETE: api/user/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound("User not found");

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
                return Ok(new { Message = "User deleted successfully" });

            return BadRequest(result.Errors);
        }

        // GET: api/user/current
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userName = User?.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized("User is not logged in.");
            }

            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(new { Email = user.Email, UserName = user.UserName });
        }

        // POST: api/user/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized("Invalid email or password.");

            var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, false, false);
            if (result.Succeeded)
            {
                return Ok(new { Message = "Login successful", User = user.UserName });
            }

            return Unauthorized("Invalid email or password.");
        }

    }

    // DTO klasa za korisnika
    public class UserDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    // DTO klasa za dodelu role
    public class RoleAssignmentDto
    {
        public string Email { get; set; }
        public string Role { get; set; }
    }



}
