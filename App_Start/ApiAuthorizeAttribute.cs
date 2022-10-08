using System;
using GitlabManager.Models;
using System.Linq;
using System.Text;
using JWT;
using JWT.Serializers;
using JWT.Algorithms;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Net.Http;
using System.Net;
using System.Configuration;

namespace GitlabManager.App_Start
{
    public class ApiAuthorizeAttribute : AuthorizeAttribute
    {
        // 加密秘钥
        private readonly string secretKey = ConfigurationManager.AppSettings["SecretKey"];

        // 加密秘钥
        private readonly string withToken = ConfigurationManager.AppSettings["WithToken"];

        // 认证错误信息
        private string errorMsg = "";

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            // 不开启token认证
            if (withToken == "false")
            {
                return true;
            }

            try
            {
                var authHeader = from t in actionContext.Request.Headers where t.Key == "token" select t.Value.FirstOrDefault();
                var enumerable = authHeader as string[] ?? authHeader.ToArray();
                string token = enumerable.FirstOrDefault();

                if (string.IsNullOrEmpty(enumerable.FirstOrDefault()))
                {
                    errorMsg = "签名为空！";
                    return false;
                }

                IJsonSerializer serializer = new JsonNetSerializer(); // 序列化Json
                IJwtAlgorithm algorithm = new HMACSHA256Algorithm(); //加密方式
                IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder(); // base64加解密

                IDateTimeProvider provider = new UtcDateTimeProvider();
                IJwtValidator validator = new JwtValidator(serializer, provider);

                IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder, algorithm);

                //解密
                byte[] key = Encoding.UTF8.GetBytes(secretKey);
                var authInfo = decoder.DecodeToObject<AuthInfo>(token, key, verify: true);

                if (authInfo != null)
                {
                    //判断口令过期时间
                    if (authInfo.Expires < DateTime.Now)
                    {
                        this.errorMsg = "签名过期!";
                        return false;
                    }

                    actionContext.RequestContext.RouteData.Values.Add("token", authInfo);
                    return true;
                }
            }
            catch
            {
                errorMsg = "签名无效！";
            }
            return false;
        }

        /// <summary>
        /// 处理授权失败的请求
        /// </summary>
        /// <param name="actionContext"></param>
        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            var erModel = new HttpResult()
            {
                Success = false,
                Message = errorMsg
            };
            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.OK, erModel, "application/json");
        }
    }
}