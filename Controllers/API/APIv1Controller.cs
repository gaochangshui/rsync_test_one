using GetUserAvatar.Models;
using GitlabManager.DataContext;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using static GitlabManager.Controllers.UsersController;

namespace GitLabManager.Controllers
{
    public class APIv1Controller : ApiController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        [HttpGet]
        public IHttpActionResult SetProtectedBranchCanNotPush()
        {
            DateTime dt = DateTime.Now.AddDays(-30);
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);
            var response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects?archived=0&per_page=100&last_activity_after=\"" + dt.ToString("yyyy-MM-dd") + "\"").Result;
            int TotalPages = int.Parse(response.Headers.GetValues("X-Total-Pages").First());
            int Page = int.Parse(response.Headers.GetValues("X-Page").First());
            while (Page <= TotalPages)
            {
                response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects?archived=0&per_page=100&page=" + Page.ToString() + "&last_activity_after=\"" + dt.ToString("yyyy-MM-dd") + "\"").Result;
                var result = response.Content.ReadAsStringAsync().Result;
                if (Page < TotalPages)
                {
                    Page = int.Parse(response.Headers.GetValues("X-Next-Page").First());
                }
                else
                {
                    Page = TotalPages + 1;
                }

                List<Project> projects = JsonConvert.DeserializeObject<List<Project>>(result);
                foreach (var project in projects)
                {
                    //获取保护分支
                    var ProtectedBranchsresult = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + project.id + "/protected_branches").Result.Content.ReadAsStringAsync().Result;
                    List<ProtectedBranch> protectedBranchs = JsonConvert.DeserializeObject<List<ProtectedBranch>>(ProtectedBranchsresult);
                    foreach (var protectedBranch in protectedBranchs)
                    {
                        var responsepdelete = httpClient.DeleteAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + project.id + "/protected_branches/" + protectedBranch.name).Result;
                        var result1 = responsepdelete.Content.ReadAsStringAsync().Result;
                        HttpContent httpContent = new FormUrlEncodedContent(
                        new List<KeyValuePair<string, string>> {
                            new KeyValuePair<string, string>("id", protectedBranch.id.ToString()),
                            new KeyValuePair<string, string>("name", protectedBranch.name),
                            new KeyValuePair<string, string>("allow_force_push", "false"),
                            new KeyValuePair<string, string>("push_access_level", "0")
                        });
                        var responseppost = httpClient.PostAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + project.id + "/protected_branches", httpContent).Result;
                        var result2 = responseppost.Content.ReadAsStringAsync().Result;
                    }
                }
            }
            return Json(response.Content.ReadAsStringAsync());
        }

        [HttpGet]
        public IHttpActionResult DisableRetiredUser()
        {
            List<TRE_Employee> retirepeople = new List<TRE_Employee>();
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token4"]);
            var response = httpClient.GetAsync("https://trechina.cn/apiv1/employeelist").Result;
            var result = response.Content.ReadAsStringAsync().Result;
            List<TRE_Location> locations = JsonConvert.DeserializeObject<List<TRE_Location>>(result);
            TRE_Location Location = locations.Where(a => a.LocationID == 21099999).FirstOrDefault();
            foreach (TRE_Section s in Location.SectionList)
            {
                foreach (TRE_Belong b in s.BelongList)
                {
                    foreach (TRE_Employee e in b.EmployeeList)
                    {
                        var user = db.Users.Where(i => i.username == e.EmpolyeeCD.ToString()).FirstOrDefault();
                        if (user != null)
                        {
                            retirepeople.Add(e);
                            var responseppost = httpClient.PostAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "users/" + user.id.ToString() + "/block", null).Result;
                        }
                    }
                }
            }
            return Json(retirepeople);
        }

        [HttpGet]
        public IHttpActionResult SetReviewer()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token6"]);//此token仅限于SetReviewer使用
            HttpContent httpContent = new FormUrlEncodedContent(
            new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("preferred_language", "zh_CN")
            });

            List<Project> allProjects = new List<Project>();
            var response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects?archived=0&per_page=100").Result;
            int TotalPages = int.Parse(response.Headers.GetValues("X-Total-Pages").First());
            int Page = int.Parse(response.Headers.GetValues("X-Page").First());
            while (Page <= TotalPages)
            {
                response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects?archived=0&per_page=100&page=" + Page.ToString()).Result;
                var result = response.Content.ReadAsStringAsync().Result;
                if (Page < TotalPages)
                {
                    Page = int.Parse(response.Headers.GetValues("X-Next-Page").First());
                }
                else
                {
                    Page = TotalPages + 1;
                }

                List<Project> projectslist = JsonConvert.DeserializeObject<List<Project>>(result);
                foreach (var item in projectslist)
                {
                    if (item.shared_with_groups is null)
                    {
                        httpContent = new FormUrlEncodedContent(
                        new List<KeyValuePair<string, string>> {
                            new KeyValuePair<string, string>("group_id", "70"),
                            new KeyValuePair<string, string>("group_access","20"),
                            new KeyValuePair<string, string>("id",item.id.ToString()),
                            new KeyValuePair<string, string>("expires_at",DateTime.Today.AddDays(1).ToShortDateString())

                            });
                        response = httpClient.PutAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + item.id.ToString() + "/share", httpContent).Result;
                    }
                    else
                    {
                        bool has_shared_to_technicalcommittee_reviewer = false;
                        foreach (var sharedgroup in item.shared_with_groups)
                        {
                            if (sharedgroup.group_id == 70)
                            {
                                has_shared_to_technicalcommittee_reviewer = true;
                                httpContent = new FormUrlEncodedContent(
                                new List<KeyValuePair<string, string>>
                                {

                                });
                                if (sharedgroup.expires_at is null)
                                {
                                    response = httpClient.DeleteAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + item.id.ToString() + "/share/70").Result;
                                }
                                else if (sharedgroup.expires_at <= DateTime.Now)
                                {
                                    response = httpClient.DeleteAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + item.id.ToString() + "/share/70").Result;
                                }
                                else if (sharedgroup.expires_at > DateTime.Today.AddDays(1))
                                {
                                    response = httpClient.DeleteAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + item.id.ToString() + "/share/70").Result;
                                }

                            }

                        }
                        if (!has_shared_to_technicalcommittee_reviewer)
                        {
                            httpContent = new FormUrlEncodedContent(
                            new List<KeyValuePair<string, string>> {
                            new KeyValuePair<string, string>("group_id", "70"),
                            new KeyValuePair<string, string>("group_access","20"),
                            new KeyValuePair<string, string>("id",item.id.ToString()),
                            new KeyValuePair<string, string>("expires_at",DateTime.Today.AddDays(1).ToShortDateString())
                                });
                            response = httpClient.PostAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + item.id.ToString() + "/share", httpContent).Result;

                        }
                    }
                    allProjects.Add(item);
                }
            }
            return Json(allProjects);
        }

        [HttpGet]
        public IHttpActionResult SetAvatar()
        {
            var users = db.Users.Where(i => i.avatar == null).OrderBy(i => i.username).ToList();
            string strUrl = "";
            foreach (var item in users)
            {
                int usercd = 0;
                try
                {
                    usercd = int.Parse(item.username);
                }
                catch (Exception ex)
                {
                    continue;
                }
                if (usercd < 99000000)
                {
                    strUrl = "http://t3cloud.jp/TCloudFiles/EmployeePhoto/" + item.username + ".jpg";
                    WebRequest imgRequest = WebRequest.Create(strUrl);
                    try
                    {
                        HttpWebResponse res = (HttpWebResponse)imgRequest.GetResponse();
                        if (res.StatusCode.ToString() == "OK")
                        {
                            string dertory = @"D:\UserAvatar\" + item.id.ToString() + @"\";
                            string filename = item.username.ToString() + ".jpg"; ;
                            if (!System.IO.Directory.Exists(dertory))
                            {
                                System.IO.Directory.CreateDirectory(dertory);
                            }

                            CutForSquare(imgRequest.GetResponse().GetResponseStream(), dertory + filename, 254, 100);
                            
                            HttpClient httpClient = new HttpClient();
                            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token2"]);
                            MultipartFormDataContent form = new MultipartFormDataContent();
                            HttpContent content = new StringContent("user");
                            form.Add(content, "avatar");
                            var stream = new StreamContent(System.IO.File.OpenRead(dertory + filename));
                            content = stream;
                            content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                            {
                                Name = "avatar",
                                FileName = filename
                            };
                            form.Add(content);
                            var response = httpClient.PutAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "users/" + item.id.ToString(), form).Result;
                        }
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }

                }
            }
            var ret = db.Users.Where(i => i.avatar == null).OrderBy(i => i.username).ToList();
            return Json(ret);
        }

        [HttpGet]
        public IHttpActionResult UserRemind()
        {
            List<Send> sends = getSendList(ConfigurationManager.AppSettings["gitlab_instance"] + "projects?pagination=keyset&per_page=100&order_by=id&archived=0");
            if (sends.Count != 0)
            {
                // 发送钉钉
                sendDingDing(sends);
            }
            return Json(sends);
        }
    }
}