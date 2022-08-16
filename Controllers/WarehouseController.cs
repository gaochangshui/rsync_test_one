using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Web.Mvc;
using GetUserAvatar.Models;
using GitlabManager.DataContext;
using GitlabManager.Models;
using GitLabManager.Attribute;
using Newtonsoft.Json;

namespace GetUserAvatar.Controllers
{
    public class WarehouseController : Controller
    {
        public static ApplicationDbContext db = new ApplicationDbContext();


        // GET: Warehouse
        [GitAuthorize(Roles = "User")]
        public ActionResult Index()
        {
            string pj_name = Request.QueryString["pj_name"];
            string group_name = Request.QueryString["group_name"];

            string pagesize = Request.QueryString["pageSize"];
            string pageNum = Request.QueryString["pageNum"] == null ? "-1" : Request.QueryString["pageNum"];

            return View(GetWarehousesPage(pj_name, group_name, pageNum, pagesize).Warehouses);
        }

        public static Page_Warehouses GetWarehousesPage(string pj_name, string group_name, string pageNum, string pageSize)
        {
            string sql = "select " +
                        "cast(p.id as VARCHAR) as id,p.name as pj_name " +
                        ",cast(p.creator_id as VARCHAR) as creator_id " +
                        ",u3.name as creator_name " +
                        ",to_char(p.created_at at time zone '+8:00','yyyy-MM-dd hh24:mi:ss') created_at " +
                        ",to_char(p.updated_at at time zone '+8:00','yyyy-MM-dd hh24:mi:ss') updated_at " +
                        ",to_char(p.last_activity_at at time zone '+8:00','yyyy-MM-dd hh24:mi:ss') last_activity_at " +
                        ",p.description as description" +
                        ",p.archived as archived" +
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
                        "on p.id=t.project_id ";

            string sqlEnd = "group by p.id,p.name,p.description,p.creator_id,u3.name,p.created_at,p.updated_at, " +
                       "namespace_id,n.name,sync_time " +
                       "order by p.updated_at desc ";
            int ps = 50;
            int pn = 0;
            if (!string.IsNullOrEmpty(pageSize))
            {
                ps = Convert.ToInt32(pageSize);
            }
            if (!string.IsNullOrEmpty(pageNum))
            {
                pn = Convert.ToInt32(pageNum);
            }
            string sqlPage = "limit " + ps + " offset " + (pn - 1) * ps;
            //sqlEnd = sqlEnd + sqlPage;
            if (pn == -1)
            {
                sqlPage = "";
            }
            List<Warehouse> list;
            string msql = "";
            int dataCnt = 0;
            if (string.IsNullOrEmpty(pj_name) && string.IsNullOrEmpty(group_name))
            {
                dataCnt = db.Database.SqlQuery<Warehouse>(sql + sqlEnd).Count();
                //int ppp = db.Database.SqlQuery<Warehouse>(sql + sqlEnd).Count();
                list = db.Database.SqlQuery<Warehouse>(sql + sqlEnd + sqlPage).ToList();
            }
            else if (string.IsNullOrEmpty(pj_name) && !string.IsNullOrEmpty(group_name))
            {
                msql = sql + " where  n.name ilike '%" + group_name + "%' ";
                dataCnt = db.Database.SqlQuery<Warehouse>(msql + sqlEnd).Count();
                list = db.Database.SqlQuery<Warehouse>(msql + sqlEnd + sqlPage).ToList();
            }
            else if (!string.IsNullOrEmpty(pj_name) && string.IsNullOrEmpty(group_name))
            {
                msql = sql + " where  p.name ilike '%" + pj_name + "%' ";
                dataCnt = db.Database.SqlQuery<Warehouse>(msql + sqlEnd).Count();
                list = db.Database.SqlQuery<Warehouse>(msql + sqlEnd + sqlPage).ToList();
            }
            else
            {
                msql = sql + " where  n.name ilike '%" + group_name + "%' and p.name ilike '%" + pj_name + "%' ";
                dataCnt = db.Database.SqlQuery<Warehouse>(msql + sqlEnd).Count();
                list = db.Database.SqlQuery<Warehouse>(msql + sqlEnd + sqlPage).ToList();
            }
            /*   if (string.IsNullOrEmpty(pj_name) && string.IsNullOrEmpty(group_name))
               {
                   list = db.Database.SqlQuery<Warehouse>(sql + sqlEnd).ToList();
               } else if(string.IsNullOrEmpty(pj_name) && !string.IsNullOrEmpty(group_name)) {
                   sql = sql + " where  n.name ilike @group_name " + sqlEnd;
                   list = db.Database.SqlQuery<Warehouse>(sql, new SqlParameter("@group_name", "%" + group_name + "%")).ToList();
               } else if(!string.IsNullOrEmpty(pj_name) && string.IsNullOrEmpty(group_name))
               {
                   sql = sql + " where  p.name ilike @pj_name " + sqlEnd;
                   SqlParameter[] parameters = {
                       new SqlParameter("@pj_name", "%" + pj_name + "%")
                   };
                   list = db.Database.SqlQuery<Warehouse>(sql, parameters).ToList();
               } else
               {
                   sql = sql + " where  n.name ilike @group_name and p.name ilike @pj_name " + sqlEnd;
                   list = db.Database.SqlQuery<Warehouse>(sql, new SqlParameter("@group_name", "%" + group_name + "%"), new SqlParameter("@pj_name", "%" + pj_name + "%")).ToList();
               }
            */
            Page_Warehouses page_ = new Page_Warehouses();
            page_.Warehouses = list;
            page_.rowCount = dataCnt;
            page_.pageSize = Convert.ToInt32(pageSize);
            page_.pageNum = Convert.ToInt32(pageNum);
            //page_.pageNumAll = (int)Math.Ceiling((double)dataCnt / page_.pageSize);
            return page_;
        }

        // GET: Warehouse/Sync
        [GitAuthorize(Roles = "User")]
        public ActionResult Sync()
        {
            return View();
        }
        // GET: Warehouse/getSync
        [GitAuthorize(Roles = "User")]
        public ActionResult getSync()
        {
            HttpClient httpClient = new HttpClient();
            string pj_name = Request.QueryString["pj_name"];
            string group_name = Request.QueryString["group_name"];
            String canshu = "?userCode=" + Session["LoginedUser"].ToString() + "&pjName=" + pj_name + "&groupName=" + group_name;
            var syncList = httpClient.GetAsync("http://172.17.1.30:8088/gitlab/sync/" + canshu).Result;
            return Json(syncList.Content.ReadAsStringAsync().Result, JsonRequestBehavior.AllowGet);
        }
        [GitAuthorize(Roles = "User")]
        public ActionResult doSync()
        {
            HttpClient httpClient = new HttpClient();
            string id = Request.QueryString["id"];
            string canshu = "?userCode=" + Session["LoginedUser"].ToString() + "&id=" + id;
            var syncList = httpClient.GetAsync("http://172.17.1.30:8088/gitlab/shell" + canshu).Result;
            return Json(syncList.Content.ReadAsStringAsync().Result, JsonRequestBehavior.AllowGet);
        }

        //POST: ProjectId
        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult getProjectId(string id)
        {
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
                return Json(new { Success = false, Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }



        /**
         * PJCD数据格式
         * 
         * 仓库：{
         * pjcdlist:"980015327,468465416,465465468",
         * 980015327:"{chbranch:develop,jpbranch:develop,jpurl:"https://asdfadsf.git"}"
         * 465465468:"{chbranch:develop,jpbranch:develop,jpurl:"https://asdfasdfasdf"}"
         * }
         */

        [AcceptVerbs(HttpVerbs.Post)]
        public JsonResult setPJCD(string id, string pjcd, string chbranch, string jpurl, string jpbranch)
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);
                string pjcdList = getPJCDList(id);
                if (pjcdList.IndexOf(pjcd) == -1)
                {
                    var putpjcdresult = httpClient.PutAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + id + "/custom_attributes/pjcdlist", new FormUrlEncodedContent(
                        new List<KeyValuePair<string, string>> {
                                new KeyValuePair<string, string>("value", pjcdList == "" ? pjcd : pjcdList + "," + pjcd)
                        }
                    )).Result;
                }
                HttpContent httpContent = new FormUrlEncodedContent(
                    new List<KeyValuePair<string, string>> {
                            new KeyValuePair<string, string>("value", "{\"chbranch\":\"" + chbranch + "\",\"jpbranch\":\"" + jpbranch +"\",\"jpurl\":\"" + jpurl + "\"}")
                    }
                );
                var response = httpClient.PutAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + id + "/custom_attributes/" + pjcd, httpContent).Result;
                return Json(new { Success = true });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /**
         * 获取项目所有属性
         * 传仓库id，与项目id
         */
        [AcceptVerbs(HttpVerbs.Get)]
        public string getOne(string id, string pjcd)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);
            var response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + id + "/custom_attributes/" + pjcd).Result;
            string result = response.Content.ReadAsStringAsync().Result;
            return result;
        }



        /**
         * 获取项目所有属性
         * 传仓库id，与项目id
         */
        [AcceptVerbs(HttpVerbs.Post)]
        public string getPJCommits(string id)
        {
            Dictionary<string, Commits> commitsMap = new Dictionary<string, Commits>();
            commitsMap = getCommitList(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + id + "/repository/commits?per_page=100", commitsMap);
            if (commitsMap.Count == 0)
            {
                return "";
            }
            return JsonConvert.SerializeObject(commitsMap);
        }

        public Dictionary<string, Commits> getCommitList(String url, Dictionary<string, Commits> commitsMap)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);
            var responseforback = httpClient.GetAsync(url).Result;
            var result = responseforback.Content.ReadAsStringAsync().Result;
            IEnumerable<string> oo;
            responseforback.Headers.TryGetValues("link", out oo);
            List<Commits> commits = JsonConvert.DeserializeObject<List<Commits>>(result);
            foreach (var commit in commits)
            {
                commitsMap[commit.author_name] = commit;
            }
            if (oo != null)
            {
                String nexturl = nextUrl(oo.First());
                if (nexturl != "")
                {
                    commitsMap = getCommitList(nexturl, commitsMap);
                }
            }
            return commitsMap;
        }

        public String nextUrl(String head)
        {
            if (head != "" && head.IndexOf("next") != -1)
            {
                string[] urlList = head.Split(new string[] { ">; rel=\"next\"" }, StringSplitOptions.None).First().Split('<');
                return urlList.Last();
            }
            return "";
        }

        /**
         * 获取仓库所有项目list
         * 传仓库id
         */
        [AcceptVerbs(HttpVerbs.Get)]
        public string getPJCDList(string id)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);
            var response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + id + "/custom_attributes/pjcdlist").Result;
            string result = response.Content.ReadAsStringAsync().Result;
            if (result.IndexOf("message") == -1)
            {
                string[] urlList = result.Split(new string[] { "\"}" }, StringSplitOptions.None).First().Split(new string[] { "value\":\"" }, StringSplitOptions.None);
                return urlList[1];
            }
            else
            {
                return "";
            }
        }

        /**
         * 获取仓库所有属性
         * 传仓库id
         */
        [AcceptVerbs(HttpVerbs.Get)]
        public string getProjectsAttributes(string id)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);
            var response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + id + "/custom_attributes").Result;
            string result = response.Content.ReadAsStringAsync().Result;
            return result;
        }

        /**
         * 删除仓库属性
         * 传仓库id
         * 传属性key
         */
        [AcceptVerbs(HttpVerbs.Post)]
        public string delProjectsAttributes(string id, string key)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);
            var response = httpClient.DeleteAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + id + "/custom_attributes/" + key).Result;
            string result = response.Content.ReadAsStringAsync().Result;
            return result;
        }

    }
}
