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
            var myId = this.GetMyId();

            var post = _context.Posts.Include(post => post.PostData)
                                        .ThenInclude(data => data.PostMedia)
                                    .Include(post => post.Repost)
                                        .ThenInclude(repost => repost.PostData)
                                            .ThenInclude(data => data.PostMedia)
                                    .Include(post => post.Repost)
                                        .ThenInclude(repost => repost.Author)
                                    .Include(post => post.Repost)
                                        .ThenInclude(repost => repost.Reposts.Where(repost => repost.AuthorId == myId))
                                    .Include(post => post.Author)
                                        .ThenInclude(author => author.Subscribers.Where(sub => sub.UserId == myId))
                                    .Include(post => post.Opinions.Where(opinion => opinion.UserId == myId)) //should countain just 1 record if any at all
                                    .Include(post => post.Reposts.Where(repost => repost.AuthorId == myId))
                                    .FirstOrDefault(post => post.PostId == id);

            if (post == null)
            {
                return NotFound();
            }

            post.MyId = myId;
            if(post.Author != null) post.Author.MyId = myId;

            return post;
        }

        /// <summary>
        /// Публикация своего поста
        /// </summary>
        // POST: api/Posts/
        [HttpPost]
        public ActionResult<int> PublishPost(PostData postData)
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

            return newPost.PostId;
        }

        /// <summary>
        /// Изменяет состояние лайка/дизлайка
        /// </summary>
        /// <param name="id">Id поста</param>
        /// <param name="opinion">Желаемое состояние лайка/дизлайка</param>
        //PUT: api/Posts/ChangeOpinion/3/?opinion=True
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
                RecountOpinions(post, opinion.Value, true);
            }
            //Если мнение существует...
            else if(existingOpinion != null)
            {
                //... но его нужно удалить
                if (opinion == null)
                {
                    bool initialValue = existingOpinion.LikeDislike;
                    _context.Opinions.Remove(existingOpinion);
                    RecountOpinions(post, initialValue, false);
                }
                //... но оно отличается от желаемого
                else if (opinion != existingOpinion.LikeDislike)
                {
                    existingOpinion.LikeDislike = opinion.Value;
                    RecountOpinions(post, true, opinion.Value);
                    RecountOpinions(post, false, !opinion.Value);
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

            var postToHide = _context.Posts.Include(post => post.Reposts)
                                    .FirstOrDefault(post => post.PostId == id);

            if (postToHide == null) 
            {
                return NotFound();
            }

            mySelf.BlacklistedPosts.Add(postToHide);

            //Если среди репостов есть мой - удалить
            var myRepost = postToHide.Reposts.FirstOrDefault(post => post.AuthorId == mySelf.UserId);
            if (myRepost != null) _context.Posts.Remove(myRepost);

            foreach (var repost in postToHide.Reposts)
            {
                mySelf.BlacklistedPosts.Add(repost);
            }

            _context.SaveChanges();

            return Ok();
        }

        /// <summary>
        /// Пересчитывает количество лайков/дизлайков
        /// </summary>
        /// <param name="post">Tracked сущность поста</param>
        /// <param name="changedValue">Какой тип opinion изменился (добавился/удалился)</param>
        private void RecountOpinions(Post post, bool changedValue, bool addedOrRemoved)
        {
            //Пересчет лайков/дизлайков
            if (changedValue)
            {
                post.Likes = _context.Opinions.Where(opinion => opinion.PostId == post.PostId && opinion.LikeDislike)
                                            .Count() + (addedOrRemoved ? 1 : -1);
            }
            else
            {
                post.Dislikes = _context.Opinions.Where(opinion => opinion.PostId == post.PostId && !opinion.LikeDislike)
                                                .Count() + (addedOrRemoved ? 1 : -1);
            }
        }
    }
}
