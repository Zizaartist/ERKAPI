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
    public class QuestionsController : Controller
    {
        private readonly ERKContext _context;

        public QuestionsController(ERKContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Создает новый вопрос
        /// </summary>
        // POST: api/Questions/
        [HttpPost]
        public ActionResult PostQuestion(Question question)
        {
            var myId = this.GetMyId();

            question.AuthorId = myId;

            _context.Questions.Add(question);
            _context.SaveChanges();

            return Ok();
        }
    }
}
