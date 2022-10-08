using GitLabManager.BLL;
using System;
using System.Web;
using System.Web.Http;
using GitlabManager.App_Start;

namespace GitLabManager.Controllers.API
{
    [ApiAuthorize]
    public class UsersApiController : ApiController
    {
        private static string path = AppDomain.CurrentDomain.BaseDirectory;
        [HttpGet]
        public IHttpActionResult GenerateKeys()
        {
            RSABLL rsa = new RSABLL();
            rsa.GenerateKeys(path);
            return Ok();
        }

        [HttpPost]
        public IHttpActionResult Encrypt()
        {
            string text = HttpContext.Current.Request.Params["text"];
            RSABLL rsa = new RSABLL();
            string a = rsa.Encrypt(text);
            return Ok(a);
        }

        [HttpPost]
        public IHttpActionResult Decrypt()
        {
            string text = HttpContext.Current.Request.Params["text"];
            RSABLL rsa = new RSABLL();
            string a = rsa.Decrypt(text);
            return Ok(a);
        }
    }
}
