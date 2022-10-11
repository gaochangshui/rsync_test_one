using System;
using System.Configuration;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using System.Text;
using GitlabManager.Models;
using System.Web.Http;

namespace GitlabManager.Controllers
{
    [RoutePrefix("api/System")]
    public class SystemController : ApiController
    {
        // 失效时间
        private readonly string timeStamp = ConfigurationManager.AppSettings["TimeStamp"];

        // 加密秘钥
        private readonly string secretKey = ConfigurationManager.AppSettings["SecretKey"];

        [HttpPost, Route("CreateToken")]
        public HttpResult CreateToken([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest == null)
            {
                return new HttpResult() { Success = false, Message = "登录信息为空！" };
            }

            AuthInfo authInfo = new AuthInfo()
            {
                UserId = loginRequest.UserId,
                Expires = DateTime.Now.AddMinutes(loginRequest.Expires)
            };

            IJsonSerializer serializer = new JsonNetSerializer();// 序列化Json
            IJwtAlgorithm algorithm = new HMACSHA256Algorithm();//加密方式
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();// base64加解密

            IJwtEncoder encoder = new JwtEncoder(algorithm, serializer, urlEncoder);// JWT编码

            byte[] key = Encoding.UTF8.GetBytes(secretKey);
            var token = encoder.Encode(authInfo, key); // 生成令牌

            return new HttpResult() { Success = true, Data = token, Message = "Token创建成功！" };
        }
    }
}