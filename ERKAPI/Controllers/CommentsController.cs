using ERKAPI.Models;
using ERKAPI.StaticValues;
using Microsoft.AspNetCore.Authorization;
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
    public class CommentsController : Controller
    {
        private readonly ERKContext _context;

        public CommentsController(ERKContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получает комментарии поста постранично
        /// </summary>
        // GET: api/Comments/3/4
        [Route("{id}/{page}")]
        [HttpGet]
        public ActionResult<IEnumerable<Comment>> GetComments(int id, int page) 
        {
            var comments = _context.Comments.Include(comment => comment.Author)
                                            .Where(comment => comment.PostId == id)
                                            .OrderByDescending(comment => comment.CreatedDate)
                                            .Skip(PageLengths.COMMENTS_PAGE * page)
                                            .Take(PageLengths.COMMENTS_PAGE);

            if(!comments.Any()) 
            {
                return NotFound();
            }

            var result = comments.ToList();

            return result;
        }

        /// <summary>
        /// Создает комментарий
        /// </summary>
        // POST: api/Comments
        [HttpPost]
        public ActionResult PostComment(Comment comment)
        {
            var myId = this.GetMyId();

            var post = _context.Posts.Find(comment.PostId);

            if (post == null)
            {
                return NotFound();
            }

            comment.AuthorId = myId;
            comment.CreatedDate = DateTime.UtcNow;

            _context.Comments.Add(comment);
            _context.SaveChanges();

            return Ok();
        }
    }
}
