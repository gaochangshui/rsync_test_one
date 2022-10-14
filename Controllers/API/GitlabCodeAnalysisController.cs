using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Configuration;
using System.Net.Http;
using GitlabManager.App_Start;
using System;
using GitLabManager.DataContext;
using System.Web.Http;
using System.Web;
using GitLabManager.Models;

namespace GitLabManager.Controllers
{
    public class GitlabCodeAnalysisController : ApiController
    {
        [HttpGet]
        public static string GetDataRsync()
        {
            var days = ConfigurationManager.AppSettings["RsyncDay"];

            var api = ConfigurationManager.AppSettings["gitlab_instance"];
            var token = ConfigurationManager.AppSettings["gitlab_token1"];

            var datetime = DateTime.Now.AddDays(Convert.ToInt32(days));

            var commitList = new List<CommitDetail>();
            var delList = new List<int>();
            var projects = DBCon.db.Projects.Where(i => i.last_activity_at > datetime).ToList();
            var users = DBCon.db.Users.ToList();
            
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", token);

            try
            {
                foreach (var pj in projects)
                {
                    Thread.Sleep(300);

                    //分支名取得
                    var response = httpClient.GetAsync(api + "projects/" + pj.id + "/repository/branches").Result;
                    var result = response.Content.ReadAsStringAsync().Result;
                    var commitIdList = new List<CommitInfo>();

                    try
                    {
                        var branchList = JsonConvert.DeserializeObject<List<BranchInfo>>(result);
                        foreach (var b in branchList)
                        {
                            //代码提交ID
                            response = httpClient.GetAsync(api + "projects/" + pj.id + "/repository/commits?ref_name=" + b.name).Result;
                            result = response.Content.ReadAsStringAsync().Result;
                            var commits = JsonConvert.DeserializeObject<List<CommitInfo>>(result);
                            foreach (var c in commits)
                            {
                                commitIdList.Add(new CommitInfo { Id = c.Id });
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        return ex.StackTrace + ";" +ex.Message;
                    }
                    // 课题别所有提交ID取得
                    var commitAll = commitIdList.Where((x, i) => commitIdList.FindIndex(s => s.Id == x.Id) == i).ToList();

                    // 代码提交履历取得
                    foreach (var c in commitAll)
                    {
                        var url_sha = api + "projects/" + pj.id + "/repository/commits/" + c.Id;

                        response = httpClient.GetAsync(url_sha).Result;
                        result = response.Content.ReadAsStringAsync().Result;
                        var commitDetail = JsonConvert.DeserializeObject<CommitDetail>(result);
                        commitDetail.commit_id = commitDetail.id;
                        commitDetail.project_name = pj.name;

                        if (commitDetail.committer_email != null)
                        {
                            try
                            {
                                var user = users.Where(i => i.email == commitDetail.committer_email).First();
                                if (user != null)
                                {
                                    commitDetail.committer_id = user.username;
                                }
                            }
                            catch
                            {
                                commitDetail.committer_id = commitDetail.committer_name;
                            }
                        }
                        else
                        {
                            commitDetail.committer_id = commitDetail.committer_name;
                        }

                        commitDetail.id = null;
                        commitList.Add(commitDetail);
                    }
                }

                foreach(var p in projects)
                {
                    delList.Add(p.id);
                }

                var history = from h in DBCon.db_agora.CommitHistory
                              where delList.Any(p => p == h.project_id)
                              select h;

                //旧的数据删除
                if( history != null && history.Count() > 0)
                {
                    DBCon.db_agora.CommitHistory.RemoveRange(history);
                }

                // 添加新的数据
                DBCon.db_agora.CommitHistory.AddRange(commitList);
                DBCon.db_agora.SaveChanges();
            }
            catch(Exception ex)
            {
                return ex.StackTrace + ";" + ex.Message;
            }

            return "Rsync success!";
        }

        [HttpGet]
        public IHttpActionResult GetWareHouse()
        {
            string namespace_id = HttpContext.Current.Request.QueryString["namespace_id"];
            var projects = DBCon.db.Projects.Where(p => p.namespace_id == namespace_id);
            return Json(projects);
        }

        [HttpGet]
        public IHttpActionResult GetMembers()
        {
            var users = from u in DBCon.db.Users
                        join i in DBCon.db.Identities on u.id equals i.user_id
                        where u.state == "active" && i.provider == "ldapmain"
                        select new userView { id = u.id,name = u.name,username = u.username,email = u.email };
            return Json(users);
        }

        [HttpGet]
        public IHttpActionResult GetDetailWarehouse()
        {
            var list = HttpContext.Current.Request.QueryString["idList"].Split(',');
            string startDate = HttpContext.Current.Request.QueryString["startDate"];
            string endDate = HttpContext.Current.Request.QueryString["endDate"];

            var history = from h in DBCon.db_agora.CommitHistory
                          where list.Any(p => p == h.project_id.ToString())
                          select new CommitView {
                              commit_id = h.commit_id,
                              project_id = h.project_id,
                              project_name = h.project_name,
                              message = h.message,
                              additions = h.stats.additions,
                              deletions = h.stats.deletions,
                              total = h.stats.total,
                              committer_id = h.committer_id,
                              committer_name = h.committer_name,
                              committer_email = h.committer_email,
                              committed_date = h.committed_date
                          };

            if (startDate != null && startDate != "" && endDate != null && endDate != "")
            {
                history = history.Where(h => h.committed_date >= Convert.ToDateTime(startDate) && h.committed_date <= Convert.ToDateTime(endDate));
            }
            return Json(history);
        }

        [HttpGet]
        public IHttpActionResult GetDetailMember()
        {
            var users = HttpContext.Current.Request.QueryString["members"].Split(',');

            string startDate = HttpContext.Current.Request.QueryString["startDate"];
            string endDate = HttpContext.Current.Request.QueryString["endDate"];

            var history = from h in DBCon.db_agora.CommitHistory
                          where users.Any(p => p == h.committer_id)
                          select new CommitView
                          {
                              commit_id = h.commit_id,
                              project_id = h.project_id,
                              project_name = h.project_name,
                              message = h.message,
                              additions = h.stats.additions,
                              deletions = h.stats.deletions,
                              total = h.stats.total,
                              committer_id = h.committer_id,
                              committer_name = h.committer_name,
                              committer_email = h.committer_email,
                              committed_date = h.committed_date
                          };

            if (startDate != null && startDate != "" && endDate != null && endDate != "")
            {
                history = history.Where(h => h.committed_date >= Convert.ToDateTime(startDate) && h.committed_date <= Convert.ToDateTime(endDate));
            }
            return Json(history);
        }

    }

    public class CommitInfo
    {
        public string Id { get; set; }
    }

    public class BranchInfo
    {
        public string name { get; set; }
    }

    public class userView
    {
        public int id { get; set; }
        public string name { get; set; }
        public string username { get; set; }
        public string email { get; set; }
    }

    public class CommitView
    {
        public int project_id { get; set; }
        public string project_name { get; set; }
        public string commit_id { get; set; }
        public string message { get; set; }
        public string committer_id { get; set; }
        public string committer_name { get; set; }
        public string committer_email { get; set; }
        public DateTime committed_date { get; set; }
        public int additions { get; set; }
        public int deletions { get; set; }
        public int total { get; set; }
    }
}