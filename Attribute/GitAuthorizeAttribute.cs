using GitlabManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GitLabManager.Attribute
{
    public class GitAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            string currentRole = httpContext.Session["UserRole"].ToString();
            string[] vs = currentRole.Split(',');
            foreach(string str in vs)
            {
                if (Roles.Contains(str))
                    return true;
            }
            return base.AuthorizeCore(httpContext);
        }
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectResult("/Home/Unauthorized");
        }
    }
}