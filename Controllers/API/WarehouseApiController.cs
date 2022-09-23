using DingDingManager.BLL;
using GetUserAvatar.Models;
using GitlabManager.Controllers;
using GitlabManager.DataContext;
using GitlabManager.Models;
using GitLabManager.BLL;
using GitLabManager.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using LibGit2Sharp;
using System.IO;
using GitLabManager.DataContext;
using System.Threading;

namespace GitLabManager.Controllers.API
{
    public class WarehouseApiController : ApiController
    {
        // public static ApplicationDbContext db = new ApplicationDbContext();
        // public static AgoraDbContext db_agora = new AgoraDbContext();
        public static SmtpClientBLL smtp = new SmtpClientBLL();

        [HttpGet]
        public IHttpActionResult Index()
        {
            string pj_name = HttpContext.Current.Request.QueryString["pj_name"];
            string group_name = HttpContext.Current.Request.QueryString["group_name"];

            string pagesize = HttpContext.Current.Request.QueryString["pageSize"];
            string pageNum = HttpContext.Current.Request.QueryString["pageNum"];


            return Json(GetWarehouses(pj_name, group_name, pageNum, pagesize, new List<Project>()));
        }

        [HttpGet]
        public IHttpActionResult ProjectURL()
        {
            string id = HttpContext.Current.Request.QueryString["pj_id"];
            try
            {
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);
                HttpContent httpContent = new FormUrlEncodedContent(
                new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("preferred_language", "zh_CN")
                });

                var response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + id).Result;
                var result = response.Content.ReadAsStringAsync().Result;
                SingleProject projects = JsonConvert.DeserializeObject<SingleProject>(result);


                return Json(new { Success = true, url = projects.web_url });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet]
        public IHttpActionResult ProjectBranches()
        {
            string id = HttpContext.Current.Request.QueryString["pj_id"];
            try
            {
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);
                HttpContent httpContent = new FormUrlEncodedContent(
                new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("preferred_language", "zh_CN")
                });

                var response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + id + "/repository/branches?per_page=100").Result;
                var result = response.Content.ReadAsStringAsync().Result;

                List<object> v = JsonConvert.DeserializeObject<List<object>>(result);
                return Json(new { Success = true, branchs = JsonConvert.DeserializeObject(result) });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet]
        public IHttpActionResult ProjectsIInvolved()
        {
            string pj_name = HttpContext.Current.Request.QueryString["pj_name"];
            string group_name = HttpContext.Current.Request.QueryString["group_name"];

            string user_cd = HttpContext.Current.Request.QueryString["user_cd"];

            string pagesize = HttpContext.Current.Request.QueryString["pageSize"];
            string pageNum = HttpContext.Current.Request.QueryString["pageNum"];

            List<Project> projects = ProjectsIInvolved(user_cd);
            Page_Warehouses page = new Page_Warehouses();
            if (projects.Count > 0)
            {
                page = GetWarehouses(pj_name, group_name, pageNum, pagesize, projects);
                return Json(page);
            }
            page.rowCount = 0;
            page.pageSize = Convert.ToInt32(pagesize);
            page.pageNum = Convert.ToInt32(pageNum);
            return Json(page);
        }

        [HttpGet]
        public IHttpActionResult ProjectsTemplate()
        {

            string pagesize = HttpContext.Current.Request.QueryString["pageSize"];
            string pageNum = HttpContext.Current.Request.QueryString["pageNum"];

            List<Project> projects = ProjectsInGroup();
            Page_Warehouses page = new Page_Warehouses();
            if (projects.Count > 0)
            {
                page = GetWarehouses(null, null, pageNum, pagesize, projects);
                return Json(page);
            }
            page.rowCount = 0;
            page.pageSize = Convert.ToInt32(pagesize);
            page.pageNum = Convert.ToInt32(pageNum);
            return Json(page);
        }

        [HttpGet]
        public IHttpActionResult ProjectsIStarred()
        {
            string user_cd = HttpContext.Current.Request.QueryString["user_cd"];
            
            string pagesize = HttpContext.Current.Request.QueryString["pageSize"];
            string pageNum = HttpContext.Current.Request.QueryString["pageNum"];

            List<Project> projects = ProjectsIStarred(user_cd);
            Page_Warehouses page = new Page_Warehouses();
            if (projects.Count > 0)
            {
                page = GetWarehouses(null, null, pageNum, pagesize, projects);
                return Json(page);
            }
            page.rowCount = 0;
            page.pageSize = Convert.ToInt32(pagesize);
            page.pageNum = Convert.ToInt32(pageNum);
            return Json(page);
        }
        [HttpGet]
        public IHttpActionResult IndexNum()
        {
            string user_cd = HttpContext.Current.Request.QueryString["user_cd"];
            int allProjNum = GetWarehouses(null, null, "-1", null, new List<Project>()).rowCount;
            int myProjNum = ProjectsIInvolved(user_cd).Count;
            int tempProjNum = ProjectsInGroup().Count;
            int starredProjNum = ProjectsIStarred(user_cd).Count;
            var num = new { all = allProjNum ,my = myProjNum ,temp = tempProjNum , starred = starredProjNum };
            return Json(new { Success = true, num = num });
        }
        private List<Project> ProjectsIInvolved(string user_cd)
        {
            try
            {
                List<Project> list = new List<Project>();
                string sql = "select temp.id,temp.name " +
                        " from" +
                        "(select " +
                        "p.id ,p.name " +
                        ",cast(namespace_id as VARCHAR) group_id,n.name group_name " +
                        ",string_agg(distinct concat_ws(':',u1.username,case when m1.access_level=50 then 'Owner' " +
                        "when m1.access_level=40 then 'M' " +
                        "when m1.access_level=30 then 'D' " +
                        "when m1.access_level=20 then 'R' " +
                        "when m1.access_level=10 then '' end ),',') AS project_member " +
                        ",string_agg(distinct concat_ws(':',u2.username,case when m2.access_level=50 then 'Owner' " +
                        "when m2.access_level=40 then 'M' " +
                        "when m2.access_level=30 then 'D' " +
                        "when m2.access_level=20 then 'R' " +
                        "when m2.access_level=10 then 'G' end),',') AS group_member " +
                        "From " +
                        "public.projects as p " +
                        "inner join public.namespaces as n on p.namespace_id=n.id " +
                        "left join public.members as m1 on p.id=m1.source_id and m1.source_type='Project' " +
                        "left join public.members as m2 on n.id=m2.source_id and m2.source_type='Namespace' " +
                        "left join public.users as u1 on m1.user_id=u1.id " +
                        "left join public.users as u2 on m2.user_id=u2.id " +
                        "group by p.id,p.name, p.namespace_id,n.name " +
                        ") temp " +
                        "where temp.project_member like '%"+ user_cd + "%' or temp.group_member like '%"+ user_cd + "%' ";
                list = DBCon.db.Database.SqlQuery<Project>(sql).ToList();
                //int UserIdinGitlab = db.Users.Where(i => i.username.Equals(user_cd)).FirstOrDefault().id;

                //HttpClient httpClient = new HttpClient();
                //httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);
                //HttpContent httpContent = new FormUrlEncodedContent(
                //new List<KeyValuePair<string, string>> {
                //        new KeyValuePair<string, string>("preferred_language", "zh_CN")
                //});

                //var response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "users/" + UserIdinGitlab + "/projects").Result;
                //var result = response.Content.ReadAsStringAsync().Result;
                //List<Project> v = JsonConvert.DeserializeObject<List<Project>>(result);
                return list;
            }
            catch
            {
                return new List<Project>();
            }
        }

        private List<Project> ProjectsInGroup()
        {
            try
            {
                List<Project> list = new List<Project>();
                string sql = "select temp.id,temp.name from " +
                        "(select " +
                        "p.id ,p.name " +
                        ",cast(namespace_id as VARCHAR) group_id,n.name group_name " +
                        ",string_agg(distinct concat_ws(':',u1.name,case when m1.access_level=50 then 'Owner' " +
                        "when m1.access_level=40 then 'M' " +
                        "when m1.access_level=30 then 'D' " +
                        "when m1.access_level=20 then 'R' " +
                        "when m1.access_level=10 then '' end ),',') AS project_member " +
                        ",string_agg(distinct concat_ws(':',u2.name,case when m2.access_level=50 then 'Owner' " +
                        "when m2.access_level=40 then 'M' " +
                        "when m2.access_level=30 then 'D' " +
                        "when m2.access_level=20 then 'R' " +
                        "when m2.access_level=10 then 'G' end),',') AS group_member " +
                        "From public.projects as p " +
                        "inner join public.namespaces as n on p.namespace_id=n.id " +
                        "left join public.members as m1 on p.id=m1.source_id and m1.source_type='Project' " +
                        "left join public.members as m2 on n.id=m2.source_id and m2.source_type='Namespace' " +
                        "left join public.users as u1 on m1.user_id=u1.id " +
                        "left join public.users as u2 on m2.user_id=u2.id " +
                        "group by p.id,p.name, p.namespace_id,n.name " +
                        ")temp " +
                        "where temp.name ilike '%template%' and group_name not like '%0%' ";
                list = DBCon.db.Database.SqlQuery<Project>(sql).ToList();

                //HttpClient httpClient = new HttpClient();
                //httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);
                //HttpContent httpContent = new FormUrlEncodedContent(
                //new List<KeyValuePair<string, string>> {
                //        new KeyValuePair<string, string>("preferred_language", "zh_CN")
                //});

                //var response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "groups/" + group_id + "/projects").Result;
                //var result = response.Content.ReadAsStringAsync().Result;
                //List<Project> v = JsonConvert.DeserializeObject<List<Project>>(result);
                return list;
            }
            catch
            {
                return new List<Project>();
            }
        }
        private List<Project> ProjectsIStarred(string user_cd)
        {
            try
            {
                int UserIdinGitlab = DBCon.db.Users.Where(i => i.username.Equals(user_cd)).FirstOrDefault().id;

                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);
                HttpContent httpContent = new FormUrlEncodedContent(
                new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("preferred_language", "zh_CN")
                });

                var response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "users/" + UserIdinGitlab + "/starred_projects").Result;
                var result = response.Content.ReadAsStringAsync().Result;
                List<Project> v = JsonConvert.DeserializeObject<List<Project>>(result);
                return v;
            }
            catch
            {
                return new List<Project>();
            }
        }

        public static Page_Warehouses GetWarehouses(string pj_name, string group_name, string pageNum, string pageSize, List<Project> projects)
        {
            String sql = "select " +
                        "cast(p.id as VARCHAR) as id,p.name as pj_name " +
                        ",cast(p.creator_id as VARCHAR) as creator_id " +
                        ",u3.name as creator_name " +
                        ",to_char(p.created_at at time zone '+8:00','yyyy-MM-dd hh24:mi:ss') created_at " +
                        ",to_char(p.updated_at at time zone '+8:00','yyyy-MM-dd hh24:mi:ss') updated_at " +
                        ",to_char(p.last_activity_at at time zone '+8:00','yyyy-MM-dd hh24:mi:ss') last_activity_at " +
                        ",p.description as description" +
                        ",p.archived as archived" +
                        ",cast(namespace_id as VARCHAR) group_id,n.name group_name " +
                        ", replace('{' || string_agg(" +
                        "     distinct concat_ws(" +
                        "         ':'''," +
                        "         '''id''', u1.id || ''''," +
                        "         ',''name''', u1.name || ''''," +
                        "         ',''access_level''', case when m1.access_level=50 then 'Owner' when m1.access_level=40 then 'M' when m1.access_level=30 then 'D' when m1.access_level=20 then 'R' when m1.access_level=10 then 'G' end || ''''," +
                        "         ',''avatar''', 'https://code.trechina.cn/gitlab/uploads/-/system/user/avatar/' || u1.id || '/' || u1.avatar || ''''" +
                        "     ), '},{'" +
                        " ) || '}', ':'',', ',')AS project_member" +

                        ", replace('{' || string_agg(" +
                        "     distinct concat_ws(" +
                        "         ':'''," +
                        "         '''id''', u2.id || ''''," +
                        "         ',''name''', u2.name || ''''," +
                        "         ',''access_level''', case when m2.access_level=50 then 'Owner' when m2.access_level=40 then 'M' when m2.access_level=30 then 'D' when m2.access_level=20 then 'R' when m2.access_level=10 then 'G' end || ''''," +
                        "         ',''avatar''', 'https://code.trechina.cn/gitlab/uploads/-/system/user/avatar/' || u2.id || '/' || u2.avatar || ''''" +
                        "     ), '},{'" +
                        " ) || '}', ':'',', ',')AS group_member" +

                        ",cast(t.sync_time as VARCHAR) AS sync_time " +
                        "From " +
                        "public.projects as p " +
                        "inner join public.namespaces as n on p.namespace_id=n.id " +
                        "left join public.members as m1 on p.id=m1.source_id and m1.source_type='Project' " +
                        "left join public.members as m2 on n.id=m2.source_id and m2.source_type='Namespace' " +
                        "left join public.users as u1 on m1.user_id=u1.id " +
                        "left join public.users as u2 on m2.user_id=u2.id " +
                        "left join public.users as u3 " +
                        "on p.creator_id=u3.id " +
                        "left join (select project_id,max(sync_time)sync_time From sync_info group by project_id) as t " +
                        "on p.id=t.project_id " +
                        " where 1=1";

            String sqlEnd = "group by p.id,p.name,p.description,p.creator_id,u3.name,p.created_at,p.updated_at, " +
                       "namespace_id,n.name,sync_time " +
                       "order by p.updated_at desc ";
            int ps = 50;
            int pn = 1;
            if (!string.IsNullOrEmpty(pageSize))
            {
                ps = Convert.ToInt32(pageSize);
            }
            if (!string.IsNullOrEmpty(pageNum))
            {
                pn = Convert.ToInt32(pageNum);
            }
            string sqlPage = "limit " + ps + " offset " + ( pn - 1 ) * ps;
            //sqlEnd = sqlEnd + sqlPage;
            if (pn == -1)
            {
                sqlPage = "";
            }
            List<Warehouse> list;
            String msql = "";
            int dataCnt = 0;
            if (projects != null && projects.Count > 0)
            {
                string pjstr= " and p.id in ( ";
                foreach(Project pj in projects)
                {
                    pjstr += pj.id + ",";
                }
                pjstr = pjstr.Substring(0, pjstr.Length - 1) + ")";
                sql = sql + pjstr;
            }
            if (string.IsNullOrEmpty(pj_name) && string.IsNullOrEmpty(group_name))
            {
                dataCnt = DBCon.db.Database.SqlQuery<Warehouse>(sql + sqlEnd).Count();
                //int ppp = db.Database.SqlQuery<Warehouse>(sql + sqlEnd).Count();
                list = DBCon.db.Database.SqlQuery<Warehouse>(sql + sqlEnd + sqlPage).ToList();
            }
            else if (string.IsNullOrEmpty(pj_name) && !string.IsNullOrEmpty(group_name))
            {
                msql = sql + " and  n.name ilike '%" + group_name + "%' ";
                dataCnt = DBCon.db.Database.SqlQuery<Warehouse>(msql + sqlEnd).Count();
                list = DBCon.db.Database.SqlQuery<Warehouse>(msql + sqlEnd + sqlPage).ToList();
            }
            else if (!string.IsNullOrEmpty(pj_name) && string.IsNullOrEmpty(group_name))
            {
                msql = sql + " and  p.name ilike '%" + pj_name + "%' ";
                dataCnt = DBCon.db.Database.SqlQuery<Warehouse>(msql + sqlEnd).Count();
                list = DBCon.db.Database.SqlQuery<Warehouse>(msql + sqlEnd + sqlPage).ToList();
            }
            else
            {
                msql = sql + " and (n.name ilike '%" + group_name + "%' or p.name ilike '%" + pj_name + "%' or p.description ilike '%"+ pj_name + "%') ";
                dataCnt = DBCon.db.Database.SqlQuery<Warehouse>(msql + sqlEnd).Count();
                list = DBCon.db.Database.SqlQuery<Warehouse>(msql + sqlEnd + sqlPage).ToList();
            }
            
            foreach (Warehouse li in list)
            {
                //空对象处理
                string nullString = "{'id','name','access_level','avatar'}";
                if (nullString.Equals(li.project_member))
                {
                    li.project_member = "";
                }
                //空头像处理
                string nullAvatar = ",'avatar'}";
                if (li.group_member.Contains(nullAvatar))
                {
                    string stra = li.group_member;
                    li.group_member = stra.Replace(nullAvatar, ",'avatar':'https://code.trechina.cn/gitlab/assets/no_avatar-849f9c04a3a0d0cea2424ae97b27447dc64a7dbfae83c036c45b403392f0e8ba.png'}");
                }
                if (li.project_member.Contains(nullAvatar))
                {
                    string stra = li.project_member;
                    li.project_member = stra.Replace(nullAvatar, ",'avatar':'https://code.trechina.cn/gitlab/assets/no_avatar-849f9c04a3a0d0cea2424ae97b27447dc64a7dbfae83c036c45b403392f0e8ba.png'}");
                }
            }
            Page_Warehouses page_ = new Page_Warehouses();
            page_.Warehouses = list;
            page_.rowCount = dataCnt;
            page_.pageSize = Convert.ToInt32(ps);
            page_.pageNum = Convert.ToInt32(pn);
            //page_.pageNumAll = (int)Math.Ceiling((double)dataCnt / page_.pageSize);
            return page_;
        }

        [HttpGet]
        public IHttpActionResult SearchSyncWarehouse()
        {
            string pj_name = HttpContext.Current.Request.QueryString["pj_name"];
            string user_cd = HttpContext.Current.Request.QueryString["user_cd"];
            string group_name = HttpContext.Current.Request.QueryString["group_name"];
            HttpClient httpClient = new HttpClient();
            String param = "?userCode=" + user_cd + "&pjName=" + pj_name + "&groupName=" + group_name;
            var syncList = httpClient.GetAsync("http://172.17.1.30:8088/gitlab/sync/" + param).Result;
            return Json(syncList.Content.ReadAsStringAsync().Result);
        }

        [HttpGet]
        public IHttpActionResult WarehouseSetting()
        {
            string pj_id = HttpContext.Current.Request.QueryString["pj_id"];
            //AsNoTracking是DBQuery类的方法，只显示不更新，查询出来的对象是Detached状态
            ProjectSyncSettings pjset = DBCon.db_agora.ProjectSyncSettings.AsNoTracking().Where(i => i.project_id.ToString() == pj_id).FirstOrDefault();
            if (pjset != null)
            {
                pjset.remote_token = "********";
            }
            return Json(new { Success = true, setting = pjset });
        }

        [HttpPost]
        public IHttpActionResult SaveWarehouseSetting(ProjectSettingsReq req)
        {
            //string pj_id = HttpContext.Current.Request.Params["pj_id"];
            //string user_cd = HttpContext.Current.Request.Params["user_cd"];
            //string branch = HttpContext.Current.Request.Params["branch"];
            //string remote_url = HttpContext.Current.Request.Params["remote_url"];
            //string remote_token = ConfigurationManager.AppSettings["remote_token"];//默认token
            //string remote_user = "";

            string pj_id = req.pj_id;
            string user_cd = req.user_cd;
            string branch = req.branch;
            string remote_url = req.remote_url;
            string remote_token = ConfigurationManager.AppSettings["remote_token"];//默认token
            string remote_user = "";
            if (remote_url.Contains("github"))
            {
                remote_token = req.remote_token;
                remote_user = req.remote_user;
            }
            bool is_modified = req.is_modified == "true";

            ProjectSyncSettings pjset = DBCon.db_agora.ProjectSyncSettings.Where(i => i.project_id.ToString() == pj_id).FirstOrDefault();
            int dbstate = 0;
            
                int maxid = DBCon.db_agora.ProjectSyncSettings.Select(q => q.id).ToList().Count>0 ?
                    DBCon.db_agora.ProjectSyncSettings.Select(q => q.id).ToList().Max():0;
            RSABLL rSA = new RSABLL();
            if (pjset == null)
            {
                ProjectSyncSettings _pjset = new ProjectSyncSettings();
                _pjset.id = maxid + 1;
                _pjset.project_id = int.Parse(pj_id);
                _pjset.sync_branches = branch;
                _pjset.remote_url = remote_url;
                _pjset.updated_at = DateTime.Now;
                _pjset.remote_token = rSA.Encrypt(remote_token);
                _pjset.remote_user = remote_user;
                //DateTime默认值
                _pjset.last_sync_at = new DateTime(1900, 01, 01, 08, 00, 00);
                _pjset.last_successful_sync_at = new DateTime(1900, 01, 01, 08, 00, 00);
                DBCon.db_agora.ProjectSyncSettings.Add(_pjset);
            }
            else
            {
                pjset.project_id = int.Parse(pj_id);
                pjset.sync_branches = branch;
                pjset.remote_url = remote_url;
                pjset.updated_at = DateTime.Now;
                if (is_modified)
                {
                    pjset.remote_token = rSA.Encrypt(remote_token);
                }
                pjset.remote_user = remote_user;
                DBCon.db_agora.Entry(pjset).State = EntityState.Modified;
            }
            dbstate = DBCon.db_agora.SaveChanges();
            
            return Json(new { Success = dbstate > 0, state = dbstate });
        }

        public class ProjectSettingsReq
        {
            public string pj_id { get; set; }
            public string user_cd { get; set; }
            public string branch { get; set; }
            public string remote_url { get; set; }
            public string remote_token { get; set; }
            public string remote_user { get; set; }
            public string is_modified { get; set; }
        }

        [HttpGet]
        public IHttpActionResult RequestForAccess()
        {
            string pj_id = HttpContext.Current.Request.QueryString["pj_id"];
            //string user_cd = HttpContext.Current.Request.Cookies["LoginedUser"].Value;
            string user_cd = HttpContext.Current.Request.QueryString["user_cd"];
            User user = DBCon.db.Users.Where(i => i.username.Equals(user_cd)).FirstOrDefault();

            DateTime dt = DateTime.Now.AddDays(1);
            string end_date = dt.ToString("yyyy-MM-dd");
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);

            HttpContent httpContent = new FormUrlEncodedContent(
            new List<KeyValuePair<string, string>> {
                            new KeyValuePair<string, string>("user_id", user.id.ToString()),
                            new KeyValuePair<string, string>("expires_at", end_date),
                            new KeyValuePair<string, string>("access_level", "30")//Developer
            });
            var response = httpClient.PostAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + pj_id + "/members", httpContent).Result;
            var result2 = response.Content.ReadAsStringAsync().Result;
            //给所有者和Maintainer发钉钉
            response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + pj_id + "/members").Result;
            var result = response.Content.ReadAsStringAsync().Result;
            List<ProjectUsers> projectUsers = JsonConvert.DeserializeObject<List<ProjectUsers>>(result);
            List<ProjectUsers> maintainers = projectUsers.FindAll(x => x.access_level == 40).ToList();//筛选maintainer权限

            httpContent = new FormUrlEncodedContent(
            new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("preferred_language", "zh_CN")
            });

            response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + pj_id).Result;
            result = response.Content.ReadAsStringAsync().Result;
            SingleProject project = JsonConvert.DeserializeObject<SingleProject>(result);

            if (maintainers.Count > 0)
            {
                DingTalkClientBLL client = new DingTalkClientBLL();
                long AgentId = long.Parse(ConfigurationManager.AppSettings["AgentId"]);
                string AccessToken = client.GetToken();
                foreach (var send in maintainers)
                {
                    String dingDingId = UsersController.getDingDingId(send.username);
                    if (dingDingId != "")
                    {
                        string Msg = "【GitLab用户权限申请通知】:\n" +
                            "" + send.name + "您好，仓库（地址： " + project.web_url + " ）的权限。刚被用户：" + user.name + "申请。过期日：" + end_date + "，请注意及时更新，祝您工作顺利。";
                        client.SendMessage(AccessToken, AgentId, dingDingId, Msg, project.web_url);
                    }
                }
            }
            return Ok(result2);
        }

        [HttpGet]
        public IHttpActionResult RequestTechnicalCommitteeReview()
        {
            string pj_id = HttpContext.Current.Request.QueryString["pj_id"];
            string user_cd = HttpContext.Current.Request.QueryString["user_cd"];
            string branchs = HttpContext.Current.Request.QueryString["branchs"];
            string main_lan = HttpContext.Current.Request.QueryString["main_lan"];
            string data_base = HttpContext.Current.Request.QueryString["data_base"];
            string review_info = HttpContext.Current.Request.QueryString["review_info"];
            string desire_date = HttpContext.Current.Request.QueryString["desire_date"];
            string comment = HttpContext.Current.Request.QueryString["comment"];
            string qcd_project = HttpContext.Current.Request.QueryString["qcd_project"];

            int codereviewer = DBCon.db.Users.Where(i => i.username.Equals("codereviewer")).FirstOrDefault().id;

            DateTime dt = DateTime.Parse(desire_date).AddDays(3);
            string end_date = dt.ToString("yyyy-MM-dd");
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);

            HttpContent httpContent = new FormUrlEncodedContent(
            new List<KeyValuePair<string, string>> {
                            new KeyValuePair<string, string>("user_id", codereviewer.ToString()),
                            new KeyValuePair<string, string>("expires_at", end_date),
                            new KeyValuePair<string, string>("access_level", "40")//Maintainer
            });
            var response = httpClient.PostAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + pj_id + "/members", httpContent).Result;
            var result2 = response.Content.ReadAsStringAsync().Result;

            //发信
            User user = DBCon.db.Users.Where(i => i.username.Equals(user_cd)).FirstOrDefault();
            httpContent = new FormUrlEncodedContent(
            new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("preferred_language", "zh_CN")
            });

            response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + pj_id).Result;
            var result = response.Content.ReadAsStringAsync().Result;
            SingleProject project = JsonConvert.DeserializeObject<SingleProject>(result);

            string emailContent = "技术委员会负责人，您好：<br/>" +
                "申请技术委员会支持，对核心代码Review。<br/>" +
                "请技术委员会协调人员做一次Review。<br/>" +
                "申请者：" + user.name + "<br/>" +
                "仓库地址：" + project.web_url + "<br/>" +
                "分支：" + branchs + "<br/>" +
                "主要语言：" + main_lan + "<br/>" +
                "数据库：" + data_base + "<br/>" +
                "评审信息：" + review_info + "<br/>" +
                "期望完成日期：" + desire_date + "<br/>" +
                "备注：" + comment;
            string title = "["+ qcd_project == ""?"GitLabmanager": qcd_project + "]核心代码Reviewer申请";
            smtp.SendMail(user.email, "technicalcommittee@cn.tre-inc.com", "qualityassurance@cn.tre-inc.com", title, emailContent);
            return Ok(result2);
        }

        [HttpGet]
        public IHttpActionResult SyncWarehouse()
        {
            //代码clone用的token
            string token = ConfigurationManager.AppSettings["gitlab_token_rsync"].ToString();

            // 日本gitlab仓库代码同期时候用的token
            string tokenPush = ConfigurationManager.AppSettings["remote_token"].ToString();

            // 项目ID
            string projectID = HttpContext.Current.Request.QueryString["pj_id"];

            string parentFolder = System.AppDomain.CurrentDomain.BaseDirectory
                    + "\\.TempData";
            string baseFolder = parentFolder + "\\" + Guid.NewGuid().ToString("N");

            string messageInfo = string.Empty;
            bool isSuccess = true;

            try
            {
                // 1. 作业文件夹创建
                Directory.CreateDirectory(baseFolder);
                // 2. gitlab的项目地址取得
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);
                HttpContent httpContent = new FormUrlEncodedContent(
                new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("preferred_language", "zh_CN")
                });

                var response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + projectID).Result;
                var result = response.Content.ReadAsStringAsync().Result;
                SingleProject projects = JsonConvert.DeserializeObject<SingleProject>(result);

                // 3. 从gitlab网站上取得源代码，放入工作目录里面（workFolder）
                var co = new CloneOptions()
                {
                    CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = "oauth2", Password = token }
                };
                string logMsg = Repository.Clone(projects.web_url, @baseFolder, co);
                // 4.课题相关信息取得
                ProjectSyncSettings projectInfo = DBCon.db_agora.ProjectSyncSettings.Where(i => i.project_id.ToString() == projectID).FirstOrDefault();
                projectInfo.last_sync_at = DateTime.Now;
                try
                {
                    string remoteUrl = projectInfo.remote_url;                             // 远程地址
                    string remoteUser = projectInfo.remote_user;                         // 远程用户(github同期用)
                    string[] rsyncBranch = projectInfo.sync_branches.Split(',');       // 同期分支
                    bool isGithub = remoteUrl.Contains("github.com");                  // 是否是github仓库
                    string remoteToken = "";
                    if (isGithub == true)
                    {
                        try
                        {
                            RSABLL rSA = new RSABLL();
                            remoteToken = rSA.Decrypt(projectInfo.remote_token);     // 密码解密(远程秘钥)
                        }
                        catch
                        {
                            remoteToken = projectInfo.remote_token;                          // 非加密的情况
                        }
                    }
                    else
                    {
                        remoteToken = tokenPush; // 日本gitlab token 
                    }

                    // 5.push认证信息设定
                    string userName = isGithub == true ? remoteUser : "oauth2";
                    var pushOption = new PushOptions()
                    {
                        CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = userName, Password = remoteToken }
                    };

                    // 6. 创建本地存储位置对象
                    var repo = new Repository(@baseFolder);

                    // 7. 添加新的远程源
                    string name = "originNew";
                    repo.Network.Remotes.Add(name, remoteUrl);
                    var remote = repo.Network.Remotes.FirstOrDefault(r => r.Name == name);

                    // 8.根据同期分支信息 循环推送代码到远程
                    for (int i = 0; i < rsyncBranch.Length; i++)
                    {
                        string objBranch = rsyncBranch[i];

                        // 根据设定的分支信息，找出有效的远程分支
                        Branch remoteBranch = null;
                        foreach (var branch in repo.Branches.ToList())
                        {
                            if (branch.CanonicalName.Contains(objBranch))
                            {
                                remoteBranch = branch;
                                break;
                            }
                        }

                        if (remoteBranch != null)
                        {
                            // 取得本地分支
                            var localBranch = repo.Branches[objBranch];

                            if (localBranch == null)
                            {
                                // 本地分支不存在的情况下则创建本地分支
                                localBranch = repo.CreateBranch(objBranch, remoteBranch.Tip);
                            }

                            repo.Branches.Update(localBranch, b => b.TrackedBranch = remoteBranch.CanonicalName);

                            // 检出远程分支到本地
                            Commands.Checkout(repo, localBranch);

                            // 推送代码到远程仓库
                            repo.Network.Push(remote, localBranch.CanonicalName, pushOption);
                        }
                    }

                    projectInfo.last_sync_succeeded = true;
                    projectInfo.last_successful_sync_at = DateTime.Now;
                    projectInfo.last_sync_error = string.Empty;
                    messageInfo = "同期成功！";
                    isSuccess = true;
                }
                catch (Exception ex)
                {
                    projectInfo.last_sync_succeeded = false;
                    projectInfo.last_sync_error = ex.Message;
                    messageInfo = ex.Message;
                    isSuccess = false;
                }
                finally
                {
                    DBCon.db_agora.Entry(projectInfo).State = EntityState.Modified;
                    DBCon.db_agora.SaveChanges();    
                }
            }
            catch (Exception ex)
            {
                messageInfo = ex.Message;
                isSuccess = false;
            }

            // 删除作业文件夹
            DirectoryInfo dir = new DirectoryInfo(@baseFolder);
            SetGitFilesNormal(dir);

            try 
            { 
                Directory.Delete(@baseFolder, true); 
            }
            catch 
            {
                SetGitFilesNormal(dir);

                try
                {
                    Directory.Delete(@baseFolder, true);
                }
                catch
                {
                };
            };
            return Json(new { Success = isSuccess, Message = messageInfo });
        }
        private void SetGitFilesNormal(DirectoryInfo directory)
        {
            foreach (FileInfo fi in directory.GetFiles())
            {
                fi.IsReadOnly = false;
            }

            foreach (DirectoryInfo subdir in directory.GetDirectories())
            {
                SetGitFilesNormal(subdir);
            }
        }

        [HttpGet]
        public IHttpActionResult SendDingDingMsg()
        {
            string parentFolder = AppDomain.CurrentDomain.BaseDirectory + "\\LOG";
            string logFile = parentFolder + "\\send_dinging_log.txt";

            Directory.CreateDirectory(parentFolder);
            var sws = new StreamWriter(logFile, true, System.Text.Encoding.UTF8);

            try
            {
                // 昨日
                string yday = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
                // 昨日没有提交代码的人员取得
                var users = NoUploadCodeUsers(yday);

                if (users == null || users.Count == 0)
                {
                    // 没有数据日志
                    string logTxt = "没有获取到" + yday + "日的数据！";
                    sws.WriteLine(logTxt);
                    return Json(new { success = true, message = "no data!" });
                }

                // 钉钉代理ID取得
                long AgentId = long.Parse(ConfigurationManager.AppSettings["AgentId"]);
                DingTalkClientBLL client = new DingTalkClientBLL();
                string AccessToken = client.GetToken();

                var  httpClient = new HttpClient();
                foreach (var u in users)
                {
                    // 钉钉个人用户id取得
                    string dingDingId = GetDingDingId(u.EmployeeCD, httpClient);

                    // 通知内容
                    string Msg = "未推送代码通知:\n系统检测到"
                        + yday + "，QCD系统中实绩登录了开发（"
                        + u.PJCD + " " + u.PJName
                        + "），但是未推送代码到GitLab平台，请确认。如有疑问，请联系本日担当 刘淼。"
                        + "\n代码审计和帮助请参考：http://docs.trechina.cn/docs/code_management/audit_rules";

                    // 发送通知
                    //client.SendMessage(AccessToken, AgentId, dingDingId, Msg, "");

                    // 发送成功日志
                    string logTxt ="通知日期：" + yday;
                    logTxt += ", 通知人员：" + u.EmployeeCD + "_";
                    logTxt += u.EmployeeName == null ? "" : u.EmployeeName;
                    logTxt += ", 通知项目：" + u.PJCD + "_" + u.PJName;
                    logTxt += ", 通知状态：发送成功";

                    sws.WriteLine(logTxt);
                }

                return Json(new { success = true ,message = ""});
            } 
            catch (Exception ex)
            {
                return Json(new { success = true, message = ex.Message});
            }
            finally
            {
                sws.Close();
            }
        }

        private  string GetDingDingId(String cd, HttpClient httpClient)
        {
            Thread.Sleep(500);
            var responseforback = httpClient.GetAsync("https://trechina.cn/APIv1/UsersDing?usercd=" + cd).Result.Content.ReadAsStringAsync().Result;
            try
            {
                    List<UserDingDing> userDings = JsonConvert.DeserializeObject<List<UserDingDing>>(responseforback);
                    if (userDings.Count == 1)
                    {
                        return userDings[0].DingID;
                    }
            }
            catch (Exception ex)
            {
                string err = ex.Message;
            }
            return "";
        }

        private List<NoCodeUserMode> NoUploadCodeUsers(string day)
        {
            try
            {
                // gitlab 用户信息取得（用户名）
                var _users = DBCon.db.Users.ToList();

                // 项目信息取得（课题名）
                var _agre = DBCon.db_agora.Agreements.Where(i => i.repo_flg == true).ToList();

                // 昨日没有登录代码的人员和项目号取得
                string api = "http://172.17.1.60:8097/api/get_data.cgi?day=" + day;
                var httpClient = new HttpClient();
                var response = httpClient.GetAsync(api).Result;
                var result = response.Content.ReadAsStringAsync().Result;
                var list = JsonConvert.DeserializeObject<List<NoCodeUserMode>>(result);

                // 根据人员CD和项目CD 添加用户名
                foreach (var i in list)
                {
                    var uname = _users.Where(u => u.username == i.EmployeeCD).FirstOrDefault();
                    if (uname != null && uname.name != null)
                    {
                        i.EmployeeName = uname.name;
                    }

                    var qcd = _agre.Where(a => a.agreement_cd == i.PJCD).FirstOrDefault();
                    if(qcd != null && qcd.agreement_name != null)
                    {
                        i.PJName = qcd.agreement_name;
                    }
                }

                // 结果返回
                return list;
            }
            catch
            {
                return new List<NoCodeUserMode>();
            }
        }

        [HttpGet]
        public IHttpActionResult SetExpiresDate()
        {
            var expDate = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");

            string token = ConfigurationManager.AppSettings["gitlab_token1"];
            string api = ConfigurationManager.AppSettings["gitlab_instance"] + "projects";
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", token);

            // 没有设定有效日期的人员取得
            var members = DBCon.db.Members.Where(i => i.source_type == "Project" && i.expires_at == null).ToList();
            if(members != null && members.Count > 0)
            {
                foreach (var m in members)
                {
                    Thread.Sleep(500);
                    var response = httpClient.GetAsync(api + "/" + m.source_id).Result;
                    var result = response.Content.ReadAsStringAsync().Result;
                    var project = JsonConvert.DeserializeObject<projectWithNameSpace>(result);

                    if (project != null && project.name_with_namespace.StartsWith("public-playground") == false)
                    {
                        var httpContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>> {
                            new KeyValuePair<string, string>("user_id", m.user_id),
                            new KeyValuePair<string, string>("expires_at", expDate),
                            new KeyValuePair<string, string>("access_level", m.access_level.ToString())
                        });
                        var mapi = api + "/" + m.source_id + "/members";
                        response = httpClient.PutAsync(mapi, httpContent).Result;
                        result = response.Content.ReadAsStringAsync().Result;
                    }
                }
            }

            return Ok();
        }
        private class UserDingDing
        {
            public string UserCD { get; set; }
            public string UserName { get; set; }
            public string DingID { get; set; }
            public string CreateDate { get; set; }
            public string UpdateDate { get; set; }
        }

        private class NoCodeUserMode
        {
            public string EmployeeCD { get; set; }
            public string PJCD { get; set; }
            public string PJName { get; set; }
            public string EmployeeName { get; set; }
        }

        private class projectWithNameSpace
        {
            public int id { get; set; }
            public string name { get; set; }
            public string name_with_namespace { get; set; }
        }
    }
}
