using Chat.Mvc.Proxies;
using Microsoft.AspNetCore.Mvc;
using chat.Modelos;
using Chat.Mvc.Models;

namespace Chat.Mvc.Controllers
{
    public class UsersController : Controller
    {

        private readonly IChatApiProxy _chatApiProxy;

        public UsersController(IChatApiProxy chatApiProxy)
        {
            _chatApiProxy = chatApiProxy;
        }

        // Acción para mostrar todos los usuarios
        public async Task<IActionResult> Index()
        {
            // Obtener la lista de usuarios
            var usuarios = await _chatApiProxy.GetUsersAsync();

            // Obtener la lista de grupos
            var grupos = await _chatApiProxy.GetGruposAsync(); // Asegúrate de tener este método en tu proxy

            // Crear un ViewModel para enviar ambos datos a la vista
            var viewModel = new UsuariosYGruposViewModel
            {
                Usuarios = usuarios,
                Grupos = grupos
            };

            return View(viewModel);
        }


        // Acción para mostrar los detalles de un usuario
        public async Task<IActionResult> Details(int id)
        {
            var user = await _chatApiProxy.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]


        public async Task<IActionResult> Create(User user, IFormFile? profilePicture)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Manejar la foto de perfil
                    if (profilePicture != null)
                    {
                        var uploadPath = Path.Combine("wwwroot/uploads/perfiles");
                        var fileName = await FileHelper.SaveFileAsync(profilePicture, uploadPath);
                        user.ProfilePicture = $"/uploads/perfiles/{fileName}";
                    }

                    // Enviar datos al proxy
                    var success = await _chatApiProxy.CreateUserAsync(user);
                    if (success)
                    {
                        return RedirectToAction(nameof(Index));
                    }

                    ModelState.AddModelError("", "Error al crear el usuario en el servidor.");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error: {ex.Message}");
                }
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _chatApiProxy.DeleteUserAsync(id);
                if (!success)
                {
                    TempData["Error"] = "No se pudo eliminar el usuario. Verifica si existe.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al intentar eliminar: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }




    }
}
