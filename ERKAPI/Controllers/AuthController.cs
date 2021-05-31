using ERKAPI.Controllers.FrequentlyUsed;
using ERKAPI.Models;
using ERKAPI.StaticValues;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ERKAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly ERKContext _context;
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _clientFactory;

        public AuthController(ERKContext context, ILogger<AuthController> logger, IMemoryCache cache, IHttpClientFactory clientFactory)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _clientFactory = clientFactory;
        }

        /// <summary>
        /// Получение пользовательского access токена
        /// в случае отсутствия пользователя в бд, создается новый
        /// </summary>
        /// <returns>Сериализированный токен и корректный номер телефона</returns>
        // POST: api/Auth/Token/?phone=79991745473&code=3667
        [Route("Token")]
        [HttpPost]
        public ActionResult<Token> GetToken([Phone] string phone, string code)
        {
            var formattedPhone = Functions.convertNormalPhoneNumber(phone);

            var codeValidationErrorText = ValidateCode(formattedPhone, code);
            if (codeValidationErrorText != null)
            {
                return BadRequest(new { errorText = codeValidationErrorText });
            }

            //Телефон и код проверены, теперь регистрация

            var existingUser = _context.Users.FirstOrDefault(user => user.Phone == formattedPhone);
            if (existingUser == null) 
            {
                return BadRequest(new { errorText = "Пользователь не найден" });
            }

            //Выдача токена

            ClaimsIdentity identity;
            try
            {
                identity = GetIdentity(existingUser);
            }
            catch (Exception _ex)
            {
                _logger.LogWarning($"Ошибка при попытке получить identity - {_ex}");
                return BadRequest(new { errorText = "Unexpected error." });
            }

            // создаем JWT-токен
            var jwt = new JwtSecurityToken(
                    claims: identity.Claims,
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            return new Token { AccessToken = encodedJwt, Username = identity.Name };
        }

        // POST: api/Auth/Register/?phone=79991745473&code=3667
        [Route("Register")]
        [HttpPost]
        public ActionResult<Token> GetToken([Phone] string phone, string code, User userData)
        {
            var formattedPhone = Functions.convertNormalPhoneNumber(phone);

            var codeValidationErrorText = ValidateCode(formattedPhone, code);
            if (codeValidationErrorText != null)
            {
                return BadRequest(new { errorText = codeValidationErrorText });
            }

            //Телефон и код проверены, теперь регистрация

            var existingUser = _context.Users.FirstOrDefault(user => user.Phone == formattedPhone);
            if (existingUser != null)
            {
                return BadRequest(new { errorText = "Пользователь с таким номером уже существует" });
            }
            existingUser = RegisterNewUser(formattedPhone, userData);

            //Выдача токена

            ClaimsIdentity identity;
            try
            {
                identity = GetIdentity(existingUser);
            }
            catch (Exception _ex)
            {
                _logger.LogWarning($"Ошибка при попытке получить identity - {_ex}");
                return BadRequest(new { errorText = "Unexpected error." });
            }

            // создаем JWT-токен
            var jwt = new JwtSecurityToken(
                    claims: identity.Claims,
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            return new Token { AccessToken = encodedJwt, Username = identity.Name };
        }

        /// <summary>
        /// Проверяет существует ли пользователь с таким номером
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        // GET: api/Auth/IsRegistered/?phone=79991745473
        [Route("IsRegistered")]
        [HttpGet]
        public ActionResult<bool> IsRegistered([Phone] string phone) => _context.Users.Any(user => user.Phone == Functions.convertNormalPhoneNumber(phone));

        /// <summary>
        /// Отправляет СМС код на указанный номер и создает временный кэш с кодом для проверки
        /// </summary>
        /// <param name="phone">Неотформатированный номер</param>
        // POST: api/Auth/SmsCheck/?phone=79991745473
        [Route("SmsCheck")]
        [HttpPost]
        public async Task<IActionResult> SmsCheck([Phone] string phone)
        {
            string PhoneLoc = Functions.convertNormalPhoneNumber(phone);
            Random rand = new Random();
            string generatedCode = rand.Next(1000, 9999).ToString();
            if (phone != null)
            {
                if (Functions.IsPhoneNumber(PhoneLoc))
                {
                    //Позволяет получать ip отправителя, можно добавить к запросу sms api для фильтрации спаммеров
                    var senderIp = Request.HttpContext.Connection.RemoteIpAddress;
                    string moreReadable = senderIp.ToString();

                    HttpClient client = _clientFactory.CreateClient();
                    HttpResponseMessage response = await client.GetAsync($"https://smsc.ru/sys/send.php?login=syberia&psw=K1e2s3k4i5l6&phones={PhoneLoc}&mes={generatedCode}");
                    if (response.IsSuccessStatusCode)
                    {
                        //Добавляем код в кэш на 5 минут
                        _cache.Set(Functions.convertNormalPhoneNumber(phone), generatedCode, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                        });
                    }
                }
                else
                {
                    return BadRequest();
                }
            }

            return Ok();
        }

        /// <summary>
        /// Проверяет активность (сущ.) кода
        /// </summary>
        /// <param name="code">СМС код</param>
        /// <param name="phone">Номер получателя</param>
        // POST: api/Auth/CodeCheck/?code=3344&phone=79991745473
        [Route("CodeCheck")]
        [HttpPost]
        public IActionResult CodeCheck(string code, [Phone] string phone)
        {
            var formattedPhone = Functions.convertNormalPhoneNumber(phone);

            var codeValidationErrorText = ValidateCode(formattedPhone, code);
            if (codeValidationErrorText != null) 
            {
                return BadRequest(new { errorText = codeValidationErrorText });
            }
            return Ok();
        }

        // PATCH: api/Auth/ChangeNumber/?newPhone=79991745473&code=1333
        [Route("ChangeNumber")]
        [Authorize]
        [HttpPatch]
        public ActionResult ChangeNumber([Phone] string newPhone, string code)
        {
            var formattedPhone = Functions.convertNormalPhoneNumber(newPhone);

            var codeValidationErrorText = ValidateCode(formattedPhone, code);
            if (codeValidationErrorText != null)
            {
                return BadRequest(new { errorText = codeValidationErrorText });
            }

            var mySelf = Functions.identityToUser(User.Identity, _context, true);

            mySelf.Phone = formattedPhone;

            _context.SaveChanges();

            return Ok();
        }

        /// <summary>
        /// Проверяет соответствие кода из кэша с полученным от пользователя
        /// </summary>
        /// <param name="key">Ключ кэша - отформатированный телефон пользователя</param>
        /// <returns>Строка ошибки, null в случае успеха</returns>
        private string ValidateCode(string key, string code) 
        {
            string localCode;

            if (!_cache.TryGetValue(key, out localCode))
            {
                return "Ошибка при извлечении из кэша.";
            }

            if (localCode == null)
            {
                return "Устаревший или отсутствующий код.";
            }
            else
            {
                if (localCode != code)
                {
                    return "Ошибка. Получен неверный код. Подтвердите номер еще раз.";
                }
            }
            return null;
        }

        ///// <summary>
        ///// Отправляет СМС код на указанный номер и создает временный кэш с кодом для проверки
        ///// </summary>
        ///// <param name="phone">Неотформатированный номер</param>
        //// POST: api/Auth/SmsCheck/?phone=79991745473
        //[Route("SmsCheck")]
        //[HttpPost]
        //public async Task<IActionResult> SmsCheck(string phone)
        //{
        //    string PhoneLoc = Functions.convertNormalPhoneNumber(phone);
        //    Random rand = new Random();
        //    string generatedCode = rand.Next(1000, 9999).ToString();
        //    if (phone != null)
        //    {
        //        if (Functions.IsPhoneNumber(PhoneLoc))
        //        {
        //            //Позволяет получать ip отправителя, можно добавить к запросу sms api для фильтрации спаммеров
        //            var senderIp = Request.HttpContext.Connection.RemoteIpAddress;
        //            string moreReadable = senderIp.ToString();

        //            HttpClient client = _clientFactory.CreateClient();
        //            HttpResponseMessage response = await client.GetAsync($"https://smsc.ru/sys/send.php?login=syberia&psw=K1e2s3k4i5l6&phones={PhoneLoc}&mes={generatedCode}");
        //            if (response.IsSuccessStatusCode)
        //            {
        //                //Добавляем код в кэш на 5 минут
        //                _cache.Set(Functions.convertNormalPhoneNumber(phone), generatedCode, new MemoryCacheEntryOptions
        //                {
        //                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        //                });
        //            }
        //        }
        //        else
        //        {
        //            return BadRequest();
        //        }
        //    }

        //    return Ok();
        //}

        ///// <summary>
        ///// Проверяет активность (сущ.) кода
        ///// </summary>
        ///// <param name="code">СМС код</param>
        ///// <param name="phone">Номер получателя</param>
        //// POST: api/Auth/CodeCheck/?code=3344&phone=79991745473
        //[Route("CodeCheck")]
        //[HttpPost]
        //public IActionResult CodeCheck(string code, string phone)
        //{
        //    if (code == _cache.Get(Functions.convertNormalPhoneNumber(phone)).ToString())
        //    {
        //        return Ok();
        //    }

        //    return BadRequest();
        //}

        ///// <summary>
        ///// Подтверждает валидность токена
        ///// </summary>
        //// GET: api/Auth/ValidateToken
        //[Route("ValidateToken")]
        //[HttpGet]
        //public ActionResult ValidateToken()
        //{
        //    if (!User.Identity.IsAuthenticated)
        //    {
        //        return Unauthorized(); //"Токен недействителен или отсутствует"
        //    }

        //    return Ok();
        //}

        //identity with user rights
        private ClaimsIdentity GetIdentity(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.UserId.ToString()),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, "User")
            };
            ClaimsIdentity claimsIdentity =
            new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);
            return claimsIdentity;
        }

        private User RegisterNewUser(string formattedPhone, User userData)
        {
            var newUser = new User()
            {
                Phone = formattedPhone,
                Name = userData.Name,
                DateOfBirth = userData.DateOfBirth,
                Email = userData.Email,
                ShowDoB = true,
                Avatar = userData.Avatar,
                CountryId = userData.CountryId,
                CityId = userData.CityId 
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            return newUser;
        }
    }
}
