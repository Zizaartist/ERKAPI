using ERKAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERKAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiasporaController : Controller
    {
        private readonly ERKContext _context;

        public DiasporaController(ERKContext context)
        {
            _context = context;
        }
    }
}
