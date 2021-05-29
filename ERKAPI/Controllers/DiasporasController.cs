using ERKAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERKAPI.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class DiasporasController : Controller
    {
        private readonly ERKContext _context;

        public DiasporasController(ERKContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить все диаспоры
        /// </summary>
        // GET: api/Diasporas/
        [HttpGet]
        public ActionResult<IEnumerable<Diaspora>> GetAll()
        {
            IQueryable<Diaspora> diasporas = _context.Diasporas;

            if (!diasporas.Any()) 
            {
                return NotFound();
            }

            var result = diasporas.ToList();

            return result;
        }

        /// <summary>
        /// Создает запрос на создание новой диаспоры
        /// </summary>
        // POST: api/Diasporas/
        [HttpPost]
        public ActionResult PostRequest(DiasporaRequest diasporaRequest) 
        {
            var myId = this.GetMyId();

            diasporaRequest.RequesterId = myId;

            _context.DiasporaRequests.Add(diasporaRequest);
            _context.SaveChanges();

            return Ok();
        }
    }
}
