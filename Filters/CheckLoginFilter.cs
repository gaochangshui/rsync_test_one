using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace GitLabManager.Filters
{
    public class CheckLoginFilter:IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            string ctrlName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
            string actionName = filterContext.ActionDescriptor.ActionName;
            if (ctrlName == "Account" && (actionName == "Index" || actionName == "Login"))
            {

            }
            else
            {
                if (filterContext.HttpContext.Session["LoginedUser"] == null)
                {
                    //filterContext.RequestContext.HttpContext.Response.Redirect("/Account/Login");
                    filterContext.Result = new RedirectResult(String.Concat("~/Account/Login", "?ReturnUrl=", filterContext.HttpContext.Request.RawUrl));
                    return;
                }
            }
        }
    }
}