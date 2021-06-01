using ERKAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERKAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController : Controller
    {
        private readonly ERKContext _context;

        public LocationsController(ERKContext context)
        {
            _context = context;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Country>> GetAll() 
        {
            IQueryable<Country> countries = _context.Countries.Include(country => country.Cities);

            if (!countries.Any()) 
            {
                return NotFound();
            }

            var result = countries.ToList();

            return result;
        }
    }
}
