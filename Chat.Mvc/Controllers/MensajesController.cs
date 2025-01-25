using Chat.Mvc.Proxies;
using chat.Modelos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Chat.Mvc.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Chat.Mvc.Controllers
{
    public class MensajesController : Controller
    {
        private readonly IChatApiProxy _chatApiProxy;

        public MensajesController(IChatApiProxy chatApiProxy)
        {
            _chatApiProxy = chatApiProxy;
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index(int usuarioActivoId = 0, int? usuarioSeleccionadoId = null, int? grupoId = null)
        {
            if (usuarioActivoId == 0)
            {
                usuarioActivoId = 1; // Usuario por defecto
            }

            var chatViewModel = new ChatViewModel();

            // Obtener todos los datos
            var usuarios = await _chatApiProxy.GetUsersAsync();
            var grupos = await _chatApiProxy.GetGruposAsync();

            var mensajes = await _chatApiProxy.GetMensajesAsync();

            // Filtrar grupos en los que el usuario activo es miembro
            var gruposFiltrados = grupos
                .Where(g => g.Users != null && g.Users.Any(u => u.Id == usuarioActivoId))
                .ToList();


            chatViewModel.Usuarios = usuarios; // Todos los usuarios
            chatViewModel.Grupos = gruposFiltrados; // Grupos filtrados según el usuario activo
            chatViewModel.UsuarioActivoId = usuarioActivoId;
            chatViewModel.UsuarioSeleccionadoId = usuarioSeleccionadoId;

            // Filtrar mensajes
            List<Mensaje> mensajesFiltrados;
            if (grupoId.HasValue)
            {
                mensajesFiltrados = mensajes.Where(m => m.GrupoId == grupoId.Value).ToList();
            }
            else if (usuarioSeleccionadoId.HasValue)
            {
                mensajesFiltrados = mensajes
                    .Where(m =>
                        (m.UserRemitenteId == usuarioActivoId && m.UserDestinatarioId == usuarioSeleccionadoId) ||
                        (m.UserRemitenteId == usuarioSeleccionadoId && m.UserDestinatarioId == usuarioActivoId))
                    .ToList();
            }
            else
            {
                mensajesFiltrados = new List<Mensaje>();
            }

            // Mapear mensajes
            chatViewModel.Mensajes = mensajesFiltrados
                .Select(m => new MensajeConNombres
                {
                    Id = m.Id,
                    Contenido = m.Contenido,
                    FechaEnvio = m.FechaEnvio,
                    NombreRemitente = usuarios.FirstOrDefault(u => u.Id == m.UserRemitenteId)?.Name ?? "Desconocido",
                    NombreDestinatario = usuarios.FirstOrDefault(u => u.Id == m.UserDestinatarioId)?.Name ?? "Desconocido",
                    UrlArchivo = m.UrlArchivo
                })
                .ToList();
         
            Console.WriteLine($"Total Grupos: {grupos.Count}");
            foreach (var grupo in grupos)
            {
                Console.WriteLine($"Grupo: {grupo.Name}, Usuarios: {grupo.Users?.Count ?? 0}");
            }


            return View(chatViewModel);
        }


        public async Task<IActionResult> Create(int remitenteId, int? grupoId = null)
        {
            var usuarios = await _chatApiProxy.GetUsersAsync();
            var grupos = await _chatApiProxy.GetGruposAsync();

            // Filtrar remitentes según el grupo seleccionado
            List<User> remitentesFiltrados;
            if (grupoId.HasValue)
            {
                var grupoSeleccionado = grupos.FirstOrDefault(g => g.Id == grupoId);
                remitentesFiltrados = grupoSeleccionado?.Users ?? new List<User>();
            }
            else
            {
                remitentesFiltrados = usuarios;
            }

            ViewBag.UserRemitenteId = new SelectList(remitentesFiltrados, "Id", "Name");
            ViewBag.UserDestinatarioId = new SelectList(usuarios, "Id", "Name");
            ViewBag.GrupoId = new SelectList(grupos, "Id", "Name");

            var mensaje = new Mensaje
            {
                UserRemitenteId = remitenteId,
                GrupoId = grupoId
            };

            return View(mensaje);
        }


        [HttpPost]
        public async Task<IActionResult> Create(Mensaje mensaje, IFormFile? archivoAdjunto)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Validar y guardar el archivo
                    if (archivoAdjunto != null && archivoAdjunto.Length > 0)
                    {
                        var uploadPath = Path.Combine("wwwroot/uploads/mensajes");
                        var fileName = await FileHelper.SaveFileAsync(archivoAdjunto, uploadPath);
                        mensaje.UrlArchivo = $"/uploads/mensajes/{fileName}";
                    }

                    mensaje.FechaEnvio = DateTime.Now;

                    // Enviar mensaje al proxy
                    var success = await _chatApiProxy.CreateMensajeAsync(mensaje);
                    if (success)
                    {
                        return RedirectToAction("Index", new { usuarioActivoId = mensaje.UserRemitenteId });
                    }

                    ModelState.AddModelError("", "Error al enviar el mensaje.");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error: {ex.Message}");
                }
            }

            // Recargar listas en caso de error
            var usuarios = await _chatApiProxy.GetUsersAsync();
            var grupos = await _chatApiProxy.GetGruposAsync();

            // Actualizar listas según el grupo seleccionado
            var remitentesFiltrados = mensaje.GrupoId.HasValue
                ? grupos.FirstOrDefault(g => g.Id == mensaje.GrupoId)?.Users ?? new List<User>()
                : usuarios;

            ViewBag.UserRemitenteId = new SelectList(remitentesFiltrados, "Id", "Name", mensaje.UserRemitenteId);
            ViewBag.UserDestinatarioId = new SelectList(usuarios, "Id", "Name", mensaje.UserDestinatarioId);
            ViewBag.GrupoId = new SelectList(grupos, "Id", "Name", mensaje.GrupoId);

            return View(mensaje);
        }


        public async Task<IActionResult> Edit(int id)
        {
            var mensaje = await _chatApiProxy.GetMensajeByIdAsync(id);
            if (mensaje == null)
            {
                return NotFound();
            }
            return View(mensaje);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Mensaje mensaje)
        {
            if (ModelState.IsValid)
            {
                var success = await _chatApiProxy.UpdateMensajeAsync(mensaje);
                if (success)
                {
                    return RedirectToAction("Index", new { usuarioActivoId = mensaje.UserRemitenteId });
                }
                ModelState.AddModelError("", "Error al actualizar el mensaje.");
            }
            return View(mensaje);
        }
        public async Task<IActionResult> ConfirmDelete(int id)
        {
            var mensaje = await _chatApiProxy.GetMensajeByIdAsync(id);
            if (mensaje == null)
            {
                return NotFound();
            }
            return View(mensaje);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _chatApiProxy.DeleteMensajeAsync(id);
            if (success)
            {
                return RedirectToAction("Index");
            }
            TempData["Error"] = "Error al eliminar el mensaje.";
            return RedirectToAction("ConfirmDelete", new { id });
        }


    }
}
