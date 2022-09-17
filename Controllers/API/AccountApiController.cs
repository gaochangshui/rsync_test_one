using GetUserAvatar.Controllers;
using GitlabManager.DataContext;
using GitlabManager.Models;
using System.Web;
using System.Web.Http;

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
                return Json(new { UserCD = model.UserCD});
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
    }
}
