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
    public class ReportsController : Controller
    {
        private readonly ERKContext _context;

        public ReportsController(ERKContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Создает новую жалобу
        /// </summary>
        // POST: api/Reports/
        [HttpPost]
        public ActionResult PostReports(Report report) 
        {
            var myId = this.GetMyId();

            report.AuthorId = myId;

            _context.Reports.Add(report);
            _context.SaveChanges();

            return Ok();
        }
    }
}
