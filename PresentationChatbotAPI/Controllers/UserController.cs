namespace LearningSystemAPI.Controllers;
using System.Text;

[ApiController, Route("api/[controller]")]
public class UserController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<IdentityUser> signInManager, IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = userManager.Users;

        return Ok(users);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateUser([FromBody] UserDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = new IdentityUser { UserName = model.Email, Email = model.Email };

        var result = await userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(model.UserType))
            {
                if (!await roleManager.RoleExistsAsync(model.UserType))
                    await roleManager.CreateAsync(new IdentityRole(model.UserType));

                await userManager.AddToRoleAsync(user, model.UserType);
            }

            return Ok(new { Message = "User created successfully", User = user });
        }

        return BadRequest(result.Errors);
    }


    [HttpPost("assign-role")]
    public async Task<IActionResult> AssignRole([FromBody] RoleAssignmentDto model)
    {
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return NotFound("User not found");

        if (!await roleManager.RoleExistsAsync(model.Role))
            return NotFound("Role does not exist");

        var result = await userManager.AddToRoleAsync(user, model.Role);
        if (result.Succeeded)
            return Ok(new { Message = "Role assigned successfully" });

        return BadRequest(result.Errors);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound("User not found");

        var result = await userManager.DeleteAsync(user);
        if (result.Succeeded)
            return Ok(new { Message = "User deleted successfully" });

        return BadRequest(result.Errors);
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userName = User?.Identity?.Name;
        if (string.IsNullOrEmpty(userName))
        {
            return Unauthorized("User is not logged in.");
        }

        var user = await userManager.FindByNameAsync(userName);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        return Ok(new { Email = user.Email, UserName = user.UserName });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return Unauthorized("Invalid email or password.");

        var result = await signInManager.PasswordSignInAsync(user.UserName, model.Password, false, false);
        if (result.Succeeded)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email)
            }),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(configuration["JwtSettings:TokenExpirationMinutes"])),
                Issuer = configuration["JwtSettings:Issuer"],
                Audience = configuration["JwtSettings:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new
            {
                Token = tokenHandler.WriteToken(token),
                Expires = tokenDescriptor.Expires
            });
        }

        return Unauthorized("Invalid email or password.");
    }
}

public class UserDto
{
    [JsonProperty("Email")]
    public string Email { get; set; }

    [JsonProperty("UserType")]
    public string? UserType { get; set; }

    [JsonProperty("FirstName")]
    public string? FirstName { get; set; }

    [JsonProperty("LastName")]
    public string? LastName { get; set; }

    [JsonProperty("IndexNumber")]
    public string? IndexNumber { get; set; }

    [JsonProperty("Courses")]
    public List<string>? Courses { get; set; }

    [JsonProperty("Subject")]
    public string? Subject { get; set; }

    [JsonProperty("Password")]
    public string Password { get; set; }
}


public class LoginDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class RoleAssignmentDto
{
    public string Email { get; set; }
    public string Role { get; set; }
}
