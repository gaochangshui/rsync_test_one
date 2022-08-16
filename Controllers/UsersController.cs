using GetUserAvatar.Models;
using GitlabManager.DataContext;
using GitlabManager.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Mvc;
using System.Configuration;
using DingDingManager.BLL;
using GitLabManager.Attribute;

namespace GitlabManager.Controllers
{
    public class UsersController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Users
        [GitAuthorize(Roles = "Admin")]
        public ActionResult Index()
        {
            return View(db.Users.OrderBy(i => i.id).ToList());
        }

        [GitAuthorize(Roles = "Admin")]
        public ActionResult CreateUserSubGroupInPlayground()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);
            foreach (var item in db.Users.Where(i => i.external == false).Where(i => i.username.Contains("0")).ToList())
            {
                HttpContent httpContent = new FormUrlEncodedContent(
                new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("name", item.username),
                        new KeyValuePair<string, string>("path", item.username),
                        new KeyValuePair<string, string>("description", item.name + "的独享空间"),
                        new KeyValuePair<string, string>("visibility", "private"),
                        new KeyValuePair<string, string>("parent_id", "10")
                });
                var response = httpClient.PostAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "groups", httpContent).Result;

                var responseGetID = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "groups/?search=" + item.username).Result;
                var result = responseGetID.Content.ReadAsStringAsync().Result;
                List<UserSubGroup> subGroup = JsonConvert.DeserializeObject<List<UserSubGroup>>(result);
                int UserPlayGroundGroupId = 0;
                if (subGroup.Count > 0)
                {
                    UserPlayGroundGroupId = subGroup[0].id;
                }


                HttpClient httpClientAddUser = new HttpClient();
                httpClientAddUser.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);

                HttpContent httpContentAddUser = new FormUrlEncodedContent(
                new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("id", UserPlayGroundGroupId.ToString()),
                        new KeyValuePair<string, string>("user_id", item.id.ToString()),
                        new KeyValuePair<string, string>("access_level", "50")
                });


                string addMemberUri = ConfigurationManager.AppSettings["gitlab_instance"] + "groups/" + UserPlayGroundGroupId.ToString() + "/members";
                var responseAddUser = httpClientAddUser.PostAsync(addMemberUri, httpContentAddUser).Result;
                result = response.Content.ReadAsStringAsync().Result;
            }

            var responseforback = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "groups").Result;

            return Json(responseforback.Content.ReadAsStringAsync(), JsonRequestBehavior.AllowGet);
        }

        [GitAuthorize(Roles = "Admin")]
        public ActionResult DeleteNoteActiveUser()
        {
            DateTime dt = DateTime.Now.AddDays(-90);
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);
            var response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects?archived=0").Result;
            var result = response.Content.ReadAsStringAsync().Result;
            List<Project> projects = JsonConvert.DeserializeObject<List<Project>>(result);
            foreach (var project in projects)
            {
                //获取项目全部直属User
                var ProjectUsersresult = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + project.id + "/members").Result.Content.ReadAsStringAsync().Result;
                List<ProjectUsers> projectsUsers = JsonConvert.DeserializeObject<List<ProjectUsers>>(ProjectUsersresult);
                //转换为筛选list
                List<ActiveUser> users = new List<ActiveUser>();
                foreach (var user in projectsUsers)
                {
                    ActiveUser one = new ActiveUser();
                    one.id = user.id;
                    one.name = user.name;
                    one.username = user.username;
                    one.state = 0;
                    users.Add(one);
                }
                //获取项目活跃事件
                var Eventsresult = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + project.id + "/events?after=\"" + dt.ToString("yyyy-MM-dd") + "\"").Result.Content.ReadAsStringAsync().Result;
                List<Event> events = JsonConvert.DeserializeObject<List<Event>>(Eventsresult);
                foreach (var item in events)
                {
                    if (users.Where(a => a.id == item.author_id).Count<ActiveUser>() > 0)
                    {
                        users.Remove(users.Find(a => a.id == item.author_id));
                    }
                }

                if (users.Count > 0)
                {
                    foreach (var user in users)
                    {
                        var responsedelete = httpClient.DeleteAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + project.id + "/members/" + user.id).Result;
                        var result1 = responsedelete.Content.ReadAsStringAsync().Result;
                    }
                }
            }
            return Json(response.Content.ReadAsStringAsync(), JsonRequestBehavior.AllowGet);
        }
        public ActionResult SetProtectedBranchCanNotPush()
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

            return Json(response.Content.ReadAsStringAsync(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult DisableRetiredUser()
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
            return Json(retirepeople, JsonRequestBehavior.AllowGet);
        }
        public class TRE_Location
        {
            public int LocationID { get; set; }
            public string LocationName { get; set; }
            public string web_url { get; set; }
            public List<shared_with_groups> shared_with_groups { get; set; }
            public List<TRE_Section> SectionList { get; set; }

        }

        public class TRE_Section
        {
            public int SectionID { get; set; }
            public string SectionName { get; set; }
            public List<TRE_Belong> BelongList { get; set; }

        }
        public class TRE_Belong
        {
            public int BelongID { get; set; }
            public string BelongName { get; set; }
            public List<TRE_Employee> EmployeeList { get; set; }
        }
        public class TRE_Employee
        {
            public int EmpolyeeCD { get; set; }
            public string EmployeeName { get; set; }
        }

        public class ActiveUser
        {
            public int id { get; set; }
            public string username { get; set; }
            public string name { get; set; }
            public int state { get; set; }
        }
        public class UserSubGroup
        {
            public int id { get; set; }
            public string name { get; set; }
        }
        [GitAuthorize(Roles = "Admin")]
        public ActionResult AddAllUserToPlayground()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token5"]);
            foreach (var item in db.Users.Where(i => i.external == false).ToList())
            {
                HttpContent httpContent = new FormUrlEncodedContent(
                new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("id", "10"),
                        new KeyValuePair<string, string>("user_id", item.id.ToString()),
                        new KeyValuePair<string, string>("access_level", "40")
                });
                var response = httpClient.PostAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "groups/10/members", httpContent).Result;

                var result = response.Content.ReadAsStringAsync().Result;
            }

            var responseforback = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "groups/10/members").Result;

            return Json(responseforback.Content.ReadAsStringAsync(), JsonRequestBehavior.AllowGet);
        }

        [GitAuthorize(Roles = "Admin")]
        public ActionResult DeleteAllUserFromPlayground()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token5"]);
            foreach (var item in db.Users.Where(i => i.external == false).ToList())
            {
                var response = httpClient.DeleteAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "groups/10/members/" + item.id.ToString()).Result;
                var result = response.Content.ReadAsStringAsync().Result;
            }

            var responseforback = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "groups/10/members").Result;

            return Json(responseforback.Content.ReadAsStringAsync(), JsonRequestBehavior.AllowGet);
        }

        public object SetReviewer()
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

            return allProjects;
        }


        public class container_expiration_policy
        {
            public string cadence { get; set; }
        }
        public ActionResult SetAvatar()
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
                catch(Exception ex)
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
            return View("Index", db.Users.Where(i => i.avatar == null).OrderBy(i => i.username).ToList());
        }
        [GitAuthorize(Roles = "Admin")]
        public ActionResult SetLanguage()
        {
            var users = db.Users.Where(i => i.username.Contains("0")).Where(i => i.preferred_language == "en").OrderBy(i => i.username).ToList();
            foreach (var item in users)
            {
                if (int.Parse(item.username) < 99000000)
                {
                    HttpClient httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token3"]);
                    HttpContent httpContent = new FormUrlEncodedContent(
                    new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("preferred_language", "zh_CN")
                });


                    var response = httpClient.PutAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "users/" + item.id.ToString(), httpContent).Result;

                }

            }

            return View("Index", db.Users.Where(i => i.preferred_language == "en").OrderBy(i => i.username).ToList());
        }

        // GET: Users/Details/5
        [GitAuthorize(Roles = "Admin")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // GET: Users/Create
        [GitAuthorize(Roles = "Admin")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、https://go.microsoft.com/fwlink/?LinkId=317598 をご覧ください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        [GitAuthorize(Roles = "Admin")]
        public ActionResult Create([Bind(Include = "id,email,username,avatar")] User user)
        {
            if (ModelState.IsValid)
            {
                db.Users.Add(user);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(user);
        }

        // GET: Users/Edit/5
        [GitAuthorize(Roles = "Admin")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // 過多ポスティング攻撃を防止するには、バインド先とする特定のプロパティを有効にしてください。
        // 詳細については、https://go.microsoft.com/fwlink/?LinkId=317598 をご覧ください。
        [HttpPost]
        [ValidateAntiForgeryToken]
        [GitAuthorize(Roles = "Admin")]
        public ActionResult Edit([Bind(Include = "id,email,username,name,avatar,preferred_language")] User user)
        {
            if (ModelState.IsValid)
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(user);
        }

        // GET: Users/Delete/5
        [GitAuthorize(Roles = "Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [GitAuthorize(Roles = "Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            User user = db.Users.Find(id);
            db.Users.Remove(user);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// 正方型裁剪
        /// 以图片中心为轴心，截取正方型，然后等比缩放
        /// 用于头像处理
        /// </summary>
        /// <remarks>吴剑 2012-08-08</remarks>
        /// <param name="fromFile">原图Stream对象</param>
        /// <param name="fileSaveUrl">缩略图存放地址</param>
        /// <param name="side">指定的边长（正方型）</param>
        /// <param name="quality">质量（范围0-100）</param>
        public static void CutForSquare(System.IO.Stream fromFile, string fileSaveUrl, int side, int quality)
        {
            //创建目录
            string dir = System.IO.Path.GetDirectoryName(fileSaveUrl);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            //原始图片（获取原始图片创建对象，并使用流中嵌入的颜色管理信息）
            System.Drawing.Image initImage = System.Drawing.Image.FromStream(fromFile, true);

            //原图宽高均小于模版时的处理
            if (initImage.Width <= side && initImage.Height <= side)
            {
                //initImage.Save(fileSaveUrl, System.Drawing.Imaging.ImageFormat.Jpeg);//不作处理，直接保存
                side = initImage.Width;//按宽度截取正方形
            }

            //原始图片的宽、高
            int initWidth = initImage.Width;
            int initHeight = initImage.Height;

            //非正方型先裁剪为正方型
            if (initWidth != initHeight)
            {
                //截图对象
                System.Drawing.Image pickedImage = null;
                System.Drawing.Graphics pickedG = null;

                //宽大于高的横图
                if (initWidth > initHeight)
                {
                    //对象实例化
                    pickedImage = new System.Drawing.Bitmap(initHeight, initHeight);
                    pickedG = System.Drawing.Graphics.FromImage(pickedImage);
                    //设置质量
                    pickedG.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    pickedG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    //定位
                    Rectangle fromR = new Rectangle((initWidth - initHeight) / 2, 0, initHeight, initHeight);
                    Rectangle toR = new Rectangle(0, 0, initHeight, initHeight);
                    //画图
                    pickedG.DrawImage(initImage, toR, fromR, System.Drawing.GraphicsUnit.Pixel);
                    //重置宽
                    initWidth = initHeight;
                }
                //高大于宽的竖图
                else
                {
                    //对象实例化
                    pickedImage = new System.Drawing.Bitmap(initWidth, initWidth);
                    pickedG = System.Drawing.Graphics.FromImage(pickedImage);
                    //设置质量
                    pickedG.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    pickedG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    //定位
                    Rectangle fromR = new Rectangle(0, (initHeight - initWidth) / 2, initWidth, initWidth);
                    Rectangle toR = new Rectangle(0, 0, initWidth, initWidth);
                    //画图
                    pickedG.DrawImage(initImage, toR, fromR, System.Drawing.GraphicsUnit.Pixel);
                    //重置高
                    initHeight = initWidth;
                }

                //将截图对象赋给原图
                initImage = (System.Drawing.Image)pickedImage.Clone();
                //释放截图资源
                pickedG.Dispose();
                pickedImage.Dispose();
            }

            //缩略图对象
            System.Drawing.Image resultImage = new System.Drawing.Bitmap(side, side);
            System.Drawing.Graphics resultG = System.Drawing.Graphics.FromImage(resultImage);
            //设置质量
            resultG.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            resultG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            //用指定背景色清空画布
            resultG.Clear(Color.White);
            //绘制缩略图
            resultG.DrawImage(initImage, new System.Drawing.Rectangle(0, 0, side, side), new System.Drawing.Rectangle(0, 0, initWidth, initHeight), System.Drawing.GraphicsUnit.Pixel);

            //关键质量控制
            //获取系统编码类型数组,包含了jpeg,bmp,png,gif,tiff
            ImageCodecInfo[] icis = ImageCodecInfo.GetImageEncoders();
            ImageCodecInfo ici = null;
            foreach (ImageCodecInfo i in icis)
            {
                if (i.MimeType == "image/jpeg" || i.MimeType == "image/bmp" || i.MimeType == "image/png" || i.MimeType == "image/gif")
                {
                    ici = i;
                }
            }
            EncoderParameters ep = new EncoderParameters(1);
            ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);

            //保存缩略图
            resultImage.Save(fileSaveUrl, ici, ep);

            //释放关键质量控制所用资源
            ep.Dispose();

            //释放缩略图资源
            resultG.Dispose();
            resultImage.Dispose();

            //释放原始图片资源
            initImage.Dispose();
        }


        /**
         * 
         */
        public ActionResult UserRemind()
        {
            List<Send> sends = getSendList(ConfigurationManager.AppSettings["gitlab_instance"] + "projects?pagination=keyset&per_page=100&order_by=id&archived=0");
            if (sends.Count != 0)
            {
                // 发送钉钉
                sendDingDing(sends);
            }
            return Json(sends, JsonRequestBehavior.AllowGet);
        }

        /**
        * 
        */
        public static String sendDingDing(List<Send> sends)
        {
            String Appkey = ConfigurationManager.AppSettings["Appkey"];
            String Appsecret = ConfigurationManager.AppSettings["Appsecret"];
            long AgentId = long.Parse(ConfigurationManager.AppSettings["AgentId"]);
            DingTalkClientBLL client = new DingTalkClientBLL();
            string AccessToken = client.GetToken();
            foreach (var send in sends)
            {
                String dingDingId = getDingDingId(send.username);
                if (dingDingId != "")
                {
                    string Msg = "【GitLab用户过期通知】:\n" +
                        "" + send.name + "您好，" + send.project_name + "项目中您的权限将要到期。过期日：" + send.expires_at + "，请注意及时处理，祝您工作顺利。";
                    client.SendMessage(AccessToken, AgentId, dingDingId, Msg, send.project_url);
                }
            }
            return "";
        }

        public static List<Send> getSendList(String url)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);
            var responseforback = httpClient.GetAsync(url).Result;
            var result = responseforback.Content.ReadAsStringAsync().Result;
            IEnumerable<string> oo;
            responseforback.Headers.TryGetValues("link", out oo);
            List<Project> projects = JsonConvert.DeserializeObject<List<Project>>(result);
            String d3 = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd");
            String d2 = DateTime.Now.AddDays(2).ToString("yyyy-MM-dd");
            String d1 = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            List<Send> sends = new List<Send>();
            foreach (var project in projects)
            {
                //获取项目全部直属User
                var ProjectUsersresult = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + project.id + "/members").Result.Content.ReadAsStringAsync().Result;
                List<ProjectUsers> projectsUsers = JsonConvert.DeserializeObject<List<ProjectUsers>>(ProjectUsersresult);
                foreach (var user in projectsUsers)
                {
                    if (!string.IsNullOrEmpty(user.expires_at) && (user.expires_at.Substring(0, 10) == d1 || user.expires_at.Substring(0, 10) == d2 || user.expires_at.Substring(0, 10) == d3))
                    {
                        Send send = new Send();
                        send.name = user.name;
                        send.username = user.username;
                        send.project_name = project.name;
                        send.project_url = project.web_url;
                        send.expires_at = user.expires_at;
                        sends.Add(send);
                    }
                }
            }
            if (oo != null)
            {
                String nexturl = nextUrl(oo.First());
                if (nexturl != "")
                {
                    sends = sends.Concat(getSendList(nexturl)).ToList();
                }
            }
            return sends;
        }

        public static String nextUrl(String head)
        {
            if (head != "" && head.IndexOf("next") != -1)
            {
                string[] urlList = head.Split(new string[] { ">; rel=\\\"next\\\", <" }, StringSplitOptions.None).First().Split('>').First().Split('<');
                if (urlList.Length == 2)
                {
                    return urlList[1];
                }
            }
            return "";
        }


        public class UserDingDing
        {
            public string UserCD { get; set; }
            public string UserName { get; set; }
            public string DingID { get; set; }
            public string CreateDate { get; set; }
            public string UpdateDate { get; set; }
        }

        public static String getDingDingId(String cd)
        {
            HttpClient httpClient = new HttpClient();
            var responseforback = httpClient.GetAsync("https://trechina.cn/APIv1/UsersDing?usercd=" + cd).Result.Content.ReadAsStringAsync().Result;
            /*
            if (responseforback != "" && responseforback.IndexOf("DingID") != -1)
            {
                string[] urlList = responseforback.Split(new string[] { "\\\",\\\"CreateDate\\\"" }, StringSplitOptions.None).First().Split(new string[] { "\\\"DingID\\\":\\\"" }, StringSplitOptions.None);
                if (urlList.Length == 2)
                {
                    return urlList[1];
                }
            }
            */

            List<UserDingDing> userDings = JsonConvert.DeserializeObject<List<UserDingDing>>(responseforback);
            if (userDings.Count == 1)
            {
                return userDings[0].DingID;
            }
            return "";
        }
    }
}
