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
                    NombreDestinatario = m.GrupoId != null
                        ? grupos.FirstOrDefault(g => g.Id == m.GrupoId)?.Name ?? "Grupo Desconocido"
                        : usuarios.FirstOrDefault(u => u.Id == m.UserDestinatarioId)?.Name ?? "Desconocido",
                    UrlArchivo = m.UrlArchivo
                })
                .ToList();

            Console.WriteLine($"Total Grupos: {grupos.Count}");
            foreach (var grupo in grupos)
            {
                Console.WriteLine($"Grupo: {grupo.Name}, Usuarios: {grupo.Users?.Count ?? 0}");
            }
            var leer=await _chatApiProxy.MarcarMensajesComoLeidosAsync(mensajesFiltrados);

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

                // Si se selecciona un grupo, no se permiten destinatarios individuales
                ViewBag.UserDestinatarioId = new SelectList(new List<User>(), "Id", "Name");
            }
            else
            {
                remitentesFiltrados = usuarios;

                // Si no hay grupo seleccionado, muestra todos los destinatarios
                ViewBag.UserDestinatarioId = new SelectList(usuarios, "Id", "Name");
            }

            ViewBag.UserRemitenteId = new SelectList(remitentesFiltrados, "Id", "Name");
            ViewBag.GrupoId = new SelectList(grupos, "Id", "Name", grupoId);

            var mensaje = new Mensaje
            {
                UserRemitenteId = remitenteId,
                GrupoId = grupoId
            };

            return View(mensaje);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarMensajesGlobal(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { success = false, mensajes = "El parámetro de búsqueda no puede estar vacío." });
            }

            try
            {
                var mensajes = await _chatApiProxy.BuscarMensajesPorContenidoAsync(query);

                if (mensajes.Any())
                {
                    return Json(new { success = true, mensajes });
                }
                else
                {
                    return Json(new { success = false, mensajes = "No se encontraron mensajes." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensajes = $"Error al realizar la búsqueda: {ex.Message}" });
            }
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
    [HttpGet]
        public async Task<IActionResult> BuscarMensajes(string query, int usuarioActivoId, int? usuarioSeleccionadoId = null, int? grupoId = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { success = false, mensajes = new List<object>() });
            }

            // Obtener mensajes y usuarios
            var mensajes = await _chatApiProxy.GetMensajesAsync();
            var usuarios = await _chatApiProxy.GetUsersAsync();
            var grupos = await _chatApiProxy.GetGruposAsync();

            // Filtrar mensajes
            var mensajesFiltrados = mensajes.Where(m =>
                !string.IsNullOrEmpty(m.Contenido) &&
                m.Contenido.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                (grupoId.HasValue ? m.GrupoId == grupoId : true) &&
                (usuarioSeleccionadoId.HasValue ?
                    ((m.UserRemitenteId == usuarioActivoId && m.UserDestinatarioId == usuarioSeleccionadoId) ||
                     (m.UserRemitenteId == usuarioSeleccionadoId && m.UserDestinatarioId == usuarioActivoId))
                    : true)
            ).Select(m =>
            {
                var nombreGrupo = m.GrupoId != null
                    ? grupos.FirstOrDefault(g => g.Id == m.GrupoId)?.Name
                    : null;

                var nombreDestinatario = nombreGrupo
                    ?? usuarios.FirstOrDefault(u => u.Id == m.UserDestinatarioId)?.Name
                    ?? "Desconocido";

                return new
                {
                    m.Id,
                    m.Contenido,
                    FechaEnvio = m.FechaEnvio.ToString("dd/MM/yyyy HH:mm"),
                    Remitente = usuarios.FirstOrDefault(u => u.Id == m.UserRemitenteId)?.Name ?? "Desconocido",
                    Destinatario = nombreDestinatario
                };
            }).ToList();

            return Json(new { success = true, mensajes = mensajesFiltrados });
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
