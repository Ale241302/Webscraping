using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using WebScraping.Data;

namespace WebScraping.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _context; // Cambia a AppDbContext

        public LoginModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Validar las credenciales del usuario consultando la base de datos
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == Username && u.Password == Password);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, Username)
                };
                var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                ClaimsPrincipal principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync("MyCookieAuth", principal);

                return RedirectToPage("/Index");  // Redirigir a la página principal después del login
            }
            else
            {
                ErrorMessage = "Usuario o contraseña inválidos";
                return Page();
            }
        }
    }
}
