using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using chat.Modelos;

namespace chat.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GrupoesController : ControllerBase
    {
        private readonly dbContext _context;

        public GrupoesController(dbContext context)
        {
            _context = context;
        }

        // GET: api/Grupoes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Grupo>>> GetGrupo()
        {
            return await _context.Grupo.ToListAsync();
        }

        // GET: api/Grupoes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Grupo>> GetGrupo(int id)
        {
            var grupo = await _context.Grupo.FindAsync(id);

            if (grupo == null)
            {
                return NotFound();
            }

            return grupo;
        }

        // PUT: api/Grupoes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGrupo(int id, Grupo grupo)
        {
            if (id != grupo.Id)
            {
                return BadRequest();
            }

            _context.Entry(grupo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GrupoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Grupoes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Grupo>> PostGrupo(Grupo grupo)
        {
            _context.Grupo.Add(grupo);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetGrupo", new { id = grupo.Id }, grupo);
        }

        // DELETE: api/Grupoes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGrupo(int id)
        {
            var grupo = await _context.Grupo.FindAsync(id);
            if (grupo == null)
            {
                return NotFound();
            }

            _context.Grupo.Remove(grupo);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool GrupoExists(int id)
        {
            return _context.Grupo.Any(e => e.Id == id);
        }
    }
}
