using ERKAPI.Models;
using ERKAPI.StaticValues;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ERKAPI.Controllers
{
    [Route("api/")]
    [Authorize]
    [ApiController]
    public class FeedController : Controller
    {
        private readonly ERKContext _context;

        public FeedController(ERKContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получаем страницу постов сформированную из подписок
        /// </summary>
        // GET: api/Feed/2
        [Route("Feed/{page}")]
        [HttpGet]
        public ActionResult<IEnumerable<Post>> GetMyFeed(int page) 
        {
            var myId = this.GetMyId();

            var subscriptionIds = _context.Subscriptions.Where(sub => sub.SubscriberId == myId)
                                                        .Select(sub => sub.SubscribedToId);

            var blacklisted = _context.BlacklistedPosts.Where(post => post.UserId == myId)
                                                        .Select(post => post.PostId);

            var posts = InitialQueryWithoutReposts.Where(post => subscriptionIds.Contains(post.AuthorId.Value))
                                                .Where(post => !blacklisted.Contains(post.PostId));

            posts = SortByDateAndPaginate(posts, page);

            if (!posts.Any())
            {
                return NotFound();
            }

            var result = posts.ToList();
            TrimTexts(result);

            return result;
        }

        /// <summary>
        /// Получаем страницу постов сформированную по критерию наличия подстроки в тексте поста
        /// </summary>
        // GET: api/Search/3/?searchCriteria=blah
        [Route("Search/{page}")]
        [HttpGet]
        public ActionResult<IEnumerable<Post>> SearchForPosts(int page, string searchCriteria = null)
        {
            var myId = this.GetMyId();

            var searchCriteriaCaps = searchCriteria.ToUpper();

            var blacklisted = _context.BlacklistedPosts.Where(post => post.UserId == myId)
                                                        .Select(post => post.PostId);

            var posts = InitialQueryWithoutReposts.Where(post => (post.PostData != null ? post.PostData.Text.ToUpper().Contains(searchCriteriaCaps) : false))
                                                .Where(post => !blacklisted.Contains(post.PostId));

            posts = SortByDateAndPaginate(posts, page);

            if (!posts.Any()) 
            {
                return NotFound();
            }

            var result = posts.ToList();
            TrimTexts(result);

            return result;
        }

        // GET: api/Feed/Profile/3/2
        [Route("Profile/{id}/{page}")]
        [HttpGet]
        public ActionResult<IEnumerable<Post>> GetProfilePosts(int id, int page)
        {
            var myId = this.GetMyId();

            var blacklisted = _context.BlacklistedPosts.Where(bp => bp.UserId == myId)
                                                        .Select(bp => bp.PostId);

            var posts = InitialQueryWithReposts.Where(post => post.AuthorId == id)
                                                .Where(post => !blacklisted.Contains(post.PostId));

            posts = SortByDateAndPaginate(posts, page);

            if (!posts.Any())
            {
                return NotFound();
            }

            var result = posts.ToList();
            TrimTexts(result);

            return result;
        }

        private IQueryable<Post> SortByDateAndPaginate(IQueryable<Post> initialQuery, int page) => initialQuery
            .OrderByDescending(post => post.CreatedDate)
            .Skip(PageLengths.POSTS_PAGE * page)
            .Take(PageLengths.POSTS_PAGE);

        private IQueryable<Post> InitialQueryWithoutReposts => _context.Posts
            .Include(post => post.PostData)
                .ThenInclude(data => data.PostImages)
            .Include(post => post.Author)
            .Where(post => post.RepostId == null);

        private IQueryable<Post> InitialQueryWithReposts => _context.Posts
            .Include(post => post.PostData)
                .ThenInclude(data => data.PostImages)
            .Include(post => post.Author)
            .Include(post => post.Repost)
                .ThenInclude(repost => repost.PostData)
                    .ThenInclude(data => data.PostImages)
            .Include(post => post.Repost)
                .ThenInclude(repost => repost.Author);

        private void TrimTexts(IEnumerable<Post> result) 
        {
            foreach (var post in result) 
            {
                if (post.Repost != null)
                {
                    var text = post.Repost.PostData.Text;
                    post.Repost.PostData.Text = text?.Substring(0, Math.Min(text.Length, PageLengths.POST_TEXT_MAX));
                }
                else
                {
                    var text = post.PostData.Text;
                    post.PostData.Text = text?.Substring(0, Math.Min(text.Length, PageLengths.POST_TEXT_MAX));
                }
            }
        }
    }
}
