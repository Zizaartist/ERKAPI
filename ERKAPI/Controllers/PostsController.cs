using ERKAPI.Controllers.FrequentlyUsed;
using ERKAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace ERKAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PostsController : Controller
    {
        private readonly ERKContext _context;

        public PostsController(ERKContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Возвращает подробную информацию поста, включая комментарии
        /// </summary>
        // GET: api/Posts/3
        [Route("{id}")]
        [HttpGet]
        public ActionResult<Post> GetFullPost(int id)
        {
            var post = _context.Posts.Include(post => post.PostData)
                                        .ThenInclude(data => data.PostImages)
                                    .Include(post => post.Repost)
                                        .ThenInclude(repost => repost.PostData)
                                            .ThenInclude(data => data.PostImages)
                                    .Include(post => post.Repost)
                                        .ThenInclude(repost => repost.Author)
                                    .Include(post => post.Author)
                                    .FirstOrDefault(post => post.PostId == id);

            if (post == null)
            {
                return NotFound();
            }

            if (post.Repost != null) post.Repost.IsOriginalPost = false;

            return post;
        }

        /// <summary>
        /// Публикация своего поста
        /// </summary>
        // POST: api/Posts/
        [HttpPost]
        public ActionResult PublishPost(PostData postData)
        {
            var myId = this.GetMyId();

            var newPost = new Post
            {
                AuthorId = myId,
                CreatedDate = DateTime.UtcNow,
                PostData = postData
            };

            _context.Posts.Add(newPost);
            _context.SaveChanges();

            return Ok();
        }

        /// <summary>
        /// Изменяет состояние лайка/дизлайка
        /// </summary>
        /// <param name="id">Id поста</param>
        /// <param name="opinion">Желаемое состояние лайка/дизлайка</param>
        //PUT: api/Posts/ChangeOpinion/?opinion=True
        [Route("ChangeOpinion/{id}")]
        [HttpPut]
        public ActionResult ChangeOpinion(int id, bool? opinion = null)
        {
            var myId = this.GetMyId();

            var post = _context.Posts.Find(id);

            if (post == null) 
            {
                return NotFound();
            }

            var existingOpinion = _context.Opinions.Find(myId, id);

            //Если мнение не существует, но мы хотим утвердить
            if (existingOpinion == null && opinion != null)
            {
                _context.Opinions.Add(new Opinion { LikeDislike = opinion.Value,
                                                    UserId = myId,
                                                    PostId = id });
            }
            //Если мнение существует...
            else if(existingOpinion != null)
            {
                //... но его нужно удалить
                if (opinion == null)
                {
                    _context.Opinions.Remove(existingOpinion);
                }
                //... но оно отличается от желаемого
                else if (opinion != existingOpinion.LikeDislike)
                {
                    existingOpinion.LikeDislike = opinion.Value;
                }
            }

            _context.SaveChanges();

            return Ok();
        }

        /// <summary>
        /// Создает/удаляет репост
        /// </summary>
        // PUT: api/Posts/ToggleRepost/3
        [Route("ToggleRepost/{id}")]
        [HttpPut]
        public ActionResult<bool> ToggleRepost(int id, bool? forceState = null)
        {
            var myId = this.GetMyId();

            var targetPost = _context.Posts.Include(post => post.Repost)
                                        .FirstOrDefault(post => post.PostId == id);

            if (targetPost == null)
            {
                return NotFound();
            }

            var originalPostId = targetPost.Repost != null ? targetPost.Repost.PostId : targetPost.PostId;

            var repost = _context.Posts.FirstOrDefault(post => post.AuthorId == myId &&
                                                                post.RepostId == originalPostId);

            var repostExists = repost != null;

            //Toggle
            if (forceState == null)
            {
                if (repostExists)
                {
                    _context.Posts.Remove(repost);
                }
                else
                {
                    _context.Posts.Add(new Post 
                    { 
                        RepostId = originalPostId,
                        AuthorId = myId,
                        CreatedDate = DateTime.UtcNow
                    });
                }
                repostExists = !repostExists;
            }
            //Force state
            else
            {
                //Подписаться, еще не подписан
                if (forceState.Value && !repostExists)
                {
                    _context.Posts.Add(new Post 
                    { 
                        RepostId = originalPostId,
                        AuthorId = myId,
                        CreatedDate = DateTime.UtcNow
                    });
                    repostExists = true;
                }
                //Отписаться, сейчас подписан
                else if (!forceState.Value && repostExists)
                {
                    _context.Posts.Remove(repost);
                    repostExists = false;
                }
            }

            _context.SaveChanges();

            return repostExists;
        }

        /// <summary>
        /// Скрывает новость, добавляя в черный список
        /// </summary>
        // POST: api/Posts/Hide/3
        [Route("Hide/{id}")]
        [HttpPost]
        public ActionResult HidePost(int id) 
        {
            var mySelf = Functions.identityToUser(User.Identity, _context, true);

            var post = _context.Posts.Find(id);

            if (post == null) 
            {
                return NotFound();
            }

            mySelf.BlacklistedPosts.Add(post);

            _context.SaveChanges();

            return Ok();
        }
    }
}
