using GitlabManager.DataContext;
using GitlabManager.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Mvc;


namespace GetUserAvatar.Controllers
{
    public class AccountController : Controller
    {
        public static ApplicationDbContext db = new ApplicationDbContext();
        // GET: Account
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!String.IsNullOrEmpty(returnUrl))
            {
                ViewBag.h2 = "Log in with a privileged account.";
            }
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            if (ModelState.IsValid && apiLogin(model))//, model.RememberMe
            {
                System.Web.HttpContext.Current.Session.Add("LoginedUser", model.UserCD);
                System.Web.HttpContext.Current.Session.Add("LoginedUserName", model.UserName);
                System.Web.HttpContext.Current.Session.Add("LoginedUserAvatar", model.AvatarUrl);
                System.Web.HttpContext.Current.Session.Add("LoginedUserWeb", model.WebUrl);
                System.Web.HttpContext.Current.Session.Add("UserRole", getUserRole(model.UserCD));
                System.Web.HttpContext.Current.Session.Add("ClientIP", System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"]);
                return RedirectToLocal(returnUrl);
            }
            ModelState.AddModelError("", "UserCode or Password is invalid.");
            return View(model);
        }

        public static bool apiLogin(LoginModel model)
        {
            string ldapUserName = model.UserCD;
            string ldapPassword = model.Password;
            try
            {
                string systemId = ConfigurationManager.AppSettings["SystemId"];
                string url = ConfigurationManager.AppSettings["LDAPApi"];
                string resultData = HttpPost(url, "{\"userCode\":\"" + ldapUserName + "\",\"password\":\"" + ldapPassword + "\",\"systemId\":\"" + systemId + "\"}");

                apiResponse rsp = JsonConvert.DeserializeObject<apiResponse>(resultData);
                if (rsp.code == 200)
                {
                    model.UserName = rsp.data[0].userName;
                    model.mail = rsp.data[0].mail;
                    try
                    {
                        int UserIdinGitlab = db.Users.Where(i => i.username.Equals(model.UserCD)).FirstOrDefault().id;
                        HttpClient httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token4"]);
                        var responseGetID = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "users/" + UserIdinGitlab).Result;
                        var result = responseGetID.Content.ReadAsStringAsync().Result;
                        SigninUser user = JsonConvert.DeserializeObject<SigninUser>(result);
                        model.AvatarUrl = user.avatar_url;
                        model.WebUrl = user.web_url;
                    }
                    catch (Exception)
                    {
                        model.AvatarUrl = "";
                        model.WebUrl = "https://code.trechina.cn/gitlab";
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

        }
        public class SigninUser
        {
            public int id { get; set; }
            public string avatar_url { get; set; }
            public string web_url { get; set; }
        }

        public static string HttpPost(string url, string body)
        {
            //ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            Encoding encoding = Encoding.UTF8;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.Accept = "text/html, application/xhtml+xml, */*";
            request.ContentType = "application/json";

            byte[] buffer = encoding.GetBytes(body);
            request.ContentLength = buffer.Length;
            request.GetRequestStream().Write(buffer, 0, buffer.Length);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            Session.Remove("LoginedUser");
            Session.Remove("UserRole");
            Session.Remove("ClientIP");

            return RedirectToAction("Login", "Account");
        }

        public static string getUserRole(string UserCD)
        {
            string ret = "";
            string adminList = ConfigurationManager.AppSettings["SuperUser"];
            string[] li = adminList.Split(';');
            if (li.Contains(UserCD))
            {
                ret += "Admin,";
            }

            string saleList = ConfigurationManager.AppSettings["SalesOffice"];
            string[] saleli = saleList.Split(';');

            if (saleli.Contains(UserCD))
            {
                ret += "Sale,";
            }
            ret += "User";
            return ret;
        }
    }
    public class apiResponse
    {
        public int status { get; set; }

        public int code { get; set; }

        public string msg { get; set; }
        public List<apiUser> data { get; set; }

    }

    public class apiUser
    {
        public string userCode { get; set; }
        public string userName { get; set; }
        public string mail { get; set; }
    }
}