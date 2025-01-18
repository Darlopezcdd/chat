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
                usuarioActivoId = 1; // Usuario predefinido
            }

            var chatViewModel = new ChatViewModel();

            // Obtener usuarios, grupos y mensajes
            var usuarios = await _chatApiProxy.GetUsersAsync();
            var grupos = await _chatApiProxy.GetGruposAsync();
            var mensajes = await _chatApiProxy.GetMensajesAsync();

            chatViewModel.Usuarios = usuarios;
            chatViewModel.Grupos = grupos;
            chatViewModel.UsuarioActivoId = usuarioActivoId;
            chatViewModel.UsuarioSeleccionadoId = usuarioSeleccionadoId;

            // Filtrar mensajes según usuario o grupo
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

            // Mapear mensajes a MensajeConNombres
            chatViewModel.Mensajes = mensajesFiltrados
                .Select(m => new MensajeConNombres
                {
                    Id = m.Id,
                    Contenido = m.Contenido,
                    FechaEnvio = m.FechaEnvio,
                    NombreRemitente = usuarios.FirstOrDefault(u => u.Id == m.UserRemitenteId)?.Name ?? "Desconocido",
                    NombreDestinatario = usuarios.FirstOrDefault(u => u.Id == m.UserDestinatarioId)?.Name ?? "Desconocido"
                })
                .ToList();

            // Actualizar atributo "Leido" a true para los mensajes mostrados
            foreach (var mensaje in mensajesFiltrados)
            {
                if (!mensaje.Leido)
                {
                    mensaje.Leido = true;
                }
            }


            await _chatApiProxy.MarcarMensajesComoLeidosAsync(mensajesFiltrados);

            return View(chatViewModel);
        }







        public IActionResult Create(int remitenteId)
        {
            // Llama al método del proxy para obtener usuarios.
            var usuarios = _chatApiProxy.GetUsersAsync().Result;

            // Imprime los usuarios para depuración.
            Console.WriteLine("Usuarios cargados desde la API:");
            Console.WriteLine(JsonConvert.SerializeObject(usuarios));

            // Pasa los usuarios al ViewBag para la vista.
            ViewBag.UserRemitenteId = new SelectList(usuarios, "Id", "Name");
            ViewBag.UserDestinatarioId = new SelectList(usuarios, "Id", "Name");

            var mensaje = new Mensaje
            {
                UserRemitenteId = remitenteId
            };

            return View(mensaje);
        }


        [HttpPost]
        public async Task<IActionResult> Create(Mensaje mensaje)
        {
            if (ModelState.IsValid)
            {
                mensaje.FechaEnvio = DateTime.Now;

  
                Console.WriteLine($"Enviando mensaje desde el controlador: {JsonConvert.SerializeObject(mensaje)}");

                var success = await _chatApiProxy.CreateMensajeAsync(mensaje);
                if (success)
                {
                    return RedirectToAction("Index", new { userId = mensaje.UserRemitenteId });
                }
                ModelState.AddModelError("", "Error al enviar el mensaje a través del proxy.");
            }

            return View(mensaje);
        }
        //[HttpPost]
        //public void LoadUsers()
        //{
        //    Obtener la lista de usuarios desde el proxy
        //    var usuarios = _chatApiProxy.GetUsersAsync().Result;

        //    Actualizar el ViewBag con los usuarios
        //    ViewBag.UserRemitenteId = new SelectList(usuarios, "Id", "Name");
        //    ViewBag.UserDestinatarioId = new SelectList(usuarios, "Id", "Name");

        //    Devolver la vista actualizada
        //    var mensaje = new Mensaje(); // Modelo vacío o prellenado si es necesario

        //}
    }
}
