using GetUserAvatar.Controllers;
using GitlabManager.DataContext;
using GitlabManager.Models;
using System.Web;
using System.Web.Http;
using System;
using System.Configuration;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using System.Text;

namespace GitLabManager.Controllers.API
{
    public class AccountApiController : ApiController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        [HttpPost]
        public IHttpActionResult Login(LoginModel model)
        {
            if (ModelState.IsValid && AccountController.apiLogin(model))
            {
                HttpContext.Current.Response.Cookies["LoginedUser"].Value = model.UserCD;
                HttpContext.Current.Response.Cookies["LoginedUserName"].Value = model.UserName;
                HttpContext.Current.Response.Cookies["LoginedUserAvatar"].Value = model.AvatarUrl;
                HttpContext.Current.Response.Cookies["LoginedUserWeb"].Value = model.WebUrl;
                HttpContext.Current.Response.Cookies["UserRole"].Value = AccountController.getUserRole(model.UserCD);
                HttpContext.Current.Response.Cookies["ClientIP"].Value = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                // token make
                var token = CreateToken(model.UserCD);

                return Json(new { UserCD = model.UserCD, AvatarUrl = model.AvatarUrl,Token = token });
            }
            ModelState.AddModelError("", "UserCode or Password is invalid.");
            return Ok(ModelState);
        }

        [HttpPost]
        public IHttpActionResult LogOff()
        {
            HttpContext.Current.Response.Cookies["LoginedUser"].Value = "";
            HttpContext.Current.Response.Cookies["LoginedUserName"].Value = "";
            HttpContext.Current.Response.Cookies["LoginedUserAvatar"].Value = "";
            HttpContext.Current.Response.Cookies["LoginedUserWeb"].Value = "";
            HttpContext.Current.Response.Cookies["UserRole"].Value = "";
            HttpContext.Current.Response.Cookies["ClientIP"].Value = "";
            return Ok();
        }

        private string CreateToken(string UserID)
        {
            string timeStamp = ConfigurationManager.AppSettings["TimeStamp"];
            string secretKey = ConfigurationManager.AppSettings["SecretKey"];
            AuthInfo authInfo = new AuthInfo()
            {
                UserId = UserID,
                Expires = DateTime.Now.AddMinutes(Convert.ToInt32(timeStamp))
            };

            IJsonSerializer serializer = new JsonNetSerializer();// 序列化Json
            IJwtAlgorithm algorithm = new HMACSHA256Algorithm();//加密方式
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();// base64加解密

            IJwtEncoder encoder = new JwtEncoder(algorithm, serializer, urlEncoder);// JWT编码

            byte[] key = Encoding.UTF8.GetBytes(secretKey);
            var token = encoder.Encode(authInfo, key); // 生成令牌

            return token;
        }
    }
}
