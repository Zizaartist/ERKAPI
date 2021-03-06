using ERKAPI.Controllers.FrequentlyUsed;
using ERKAPI.Models;
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
    [Authorize]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly ERKContext _context;

        public UsersController(ERKContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Возвращает публичные данные пользователя
        /// </summary>
        // GET: api/Users/Profile/3
        [Route("Profile/{id}")]
        [HttpGet]
        public ActionResult<User> GetProfileInfo(int id)
        {
            var myId = this.GetMyId();

            var user = _context.Users.Include(user => user.Subscribers.Where(sub => sub.UserId == myId))
                                    .FirstOrDefault(user => user.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            user.ShowSubCount = true;
            user.MyId = myId;

            return user;
        }

        /// <summary>
        /// Подписывает или отписывает от пользователя
        /// </summary>
        /// <param name="id"></param>
        /// <param name="forceState"></param>
        /// <returns>Текущее состояние подписки</returns>
        // PATCH: api/Users/ToggleSub/3?foreState=False
        [Route("ToggleSub/{id}")]
        [HttpPut]
        public ActionResult<bool> ToggleSubscription(int id, bool? forceState = null)
        {
            var mySelf = Functions.identityToUser(User.Identity, _context);

            var subscriptionUser = _context.Users.Find(id);

            if (subscriptionUser == null)
            {
                return NotFound();
            }

            var subscription = _context.Subscriptions.FirstOrDefault(sub => sub.SubscribedToId == id &&
                                                                            sub.SubscriberId == mySelf.UserId);

            bool subscriptionExists = subscription != null;

            //Toggle
            if (forceState == null)
            {
                if (subscriptionExists)
                {
                    _context.Subscriptions.Remove(subscription);
                    RecountOpinions(mySelf, false, false);
                    RecountOpinions(subscriptionUser, true, false);
                }
                else
                {
                    subscriptionUser.Subscribers.Add(mySelf);
                    RecountOpinions(mySelf, false, true);
                    RecountOpinions(subscriptionUser, true, true);
                }
                subscriptionExists = !subscriptionExists;
            }
            //Force state
            else
            {
                //Подписаться, еще не подписан
                if (forceState.Value && !subscriptionExists)
                {
                    subscriptionUser.Subscribers.Add(mySelf);
                    RecountOpinions(mySelf, false, true);
                    RecountOpinions(subscriptionUser, true, true);
                    subscriptionExists = true;
                }
                //Отписаться, сейчас подписан
                else if (!forceState.Value && subscriptionExists)
                {
                    _context.Subscriptions.Remove(subscription);
                    RecountOpinions(mySelf, false, false);
                    RecountOpinions(subscriptionUser, true, false);
                    subscriptionExists = false;
                }
            }

            _context.SaveChanges();

            return subscriptionExists;
        }

        /// <summary>
        /// Возвращает личную информацию
        /// </summary>
        // GET: api/Users/Info
        [Route("Info")]
        [HttpGet]
        public ActionResult<User> GetMyInfo() 
        {
            var mySelf = Functions.identityToUser(User.Identity, _context);
            mySelf.ShowSensitiveData = true;
            return mySelf;
        }

        /// <summary>
        /// Изменяет личную информацию
        /// </summary>
        // PATCH: api/Users/Info
        [Route("Info")]
        [HttpPatch]
        public ActionResult EditMyInfo(User userData)
        {
            var mySelf = Functions.identityToUser(User.Identity, _context, true);

            mySelf.Name = userData.Name;
            mySelf.Email = userData.Email;
            mySelf.DateOfBirth = userData.DateOfBirth;
            if(userData.Avatar != null) mySelf.Avatar = userData.Avatar;

            _context.SaveChanges();

            return Ok();
        }

        /// <summary>
        /// Пересчитывает количество подписок и подписчиков
        /// </summary>
        /// <param name="user">Tracked сущность пользователя</param>
        private void RecountOpinions(User user, bool subscriberOrSubscription, bool addedOrRemoved)
        {
            if (subscriberOrSubscription)
            {
                user.SubscriberCount = _context.Subscriptions.Where(sub => sub.SubscribedToId == user.UserId) 
                                                            .Count() + (addedOrRemoved ? 1 : -1);
            }
            else
            {
                user.SubscriptionCount = _context.Subscriptions.Where(sub => sub.SubscriberId == user.UserId)
                                                            .Count() + (addedOrRemoved ? 1 : -1);
            }
        }
    }
}
