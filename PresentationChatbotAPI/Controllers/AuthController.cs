namespace LearningSystemAPI.Controllers;

[ApiController, Route("api/[controller]")]
public class AuthController(UserManager<IdentityUser> userManager,IConfiguration configuration) : ControllerBase
{
    public class GoogleLoginDto
    {
        public string Token { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
    }

    [AllowAnonymous]
    [HttpPost("googleLogin")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto model)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(model.Token,
                new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new[] { "211471642082-vi2chql2kao03pv30lhqaoa4stk87965.apps.googleusercontent.com" }
                });


            string email = payload.Email;
            string fullName = payload.Name ?? "Nepoznato Ime";
            string photoUrl = payload.Picture ?? "";

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { error = "Kreiranje korisnika nije uspelo." });
                }
            }

            if (model.UserType?.ToLower() == "profesor")
            {
                if (!await userManager.IsInRoleAsync(user, "Professor"))
                {
                    await userManager.AddToRoleAsync(user, "Professor");
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(user, "Student"))
                {
                    await userManager.AddToRoleAsync(user, "Student");
                }
            }

            var jwt = GenerateJwt(user, fullName, photoUrl);

            return Ok(new
            {
                jwt,
                fullName,
                email,
                photoUrl
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Neuspešna Google validacija", details = ex.Message });
        }
    }

    private string GenerateJwt(IdentityUser user, string fullName, string photoUrl)
    {
        var issuer = configuration["JwtSettings:Issuer"];
        var audience = configuration["JwtSettings:Audience"];
        var secretKey = configuration["JwtSettings:SecretKey"];

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("fullName", fullName),
            new Claim("photoUrl", photoUrl)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
