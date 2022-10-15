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

            // 图像数据做成
            var wareData = new List<WareSumView>();
            foreach(var h in history)
            {
                wareData.Add(new WareSumView
                {
                    project_id = h.project_id,
                    project_name = h.project_name,
                    committed_date = h.committed_date.ToShortDateString(),
                    committed_date_id = h.committed_date.ToString("yyyyMMdd"),
                    additions = h.additions,
                    deletions =h.deletions,
                    total =h.total
                }) ;
            }

           var sumData = from s in wareData
                group s by new { s.project_id,s.project_name,s.committed_date ,s.committed_date_id} into g
                    select new { 
                        g.Key.project_id,
                        g.Key.project_name,
                        g.Key.committed_date_id,
                        g.Key.committed_date,
                        additions = g.Sum(i => i.additions),
                        deletions = g.Sum(i => i.deletions),
                        total = g.Sum(i => i.total)
                    } ;

            var projectID = (from s in sumData select new { s.project_id, s.project_name }).Distinct().OrderBy(i =>i.project_id);
            var committedDate = (from s in sumData select new { s.committed_date_id, s.committed_date }).Distinct().OrderBy(i => i.committed_date_id).Select(c =>c.committed_date);

            var graphList = new List<GraphView>();

            foreach (var p in projectID)
            {
                var dataTotal= new List<int>();
                var dataAdditions = new List<int>();
                var dataDeletions = new List<int>();
                var dataList = sumData.Where(s => s.project_id == p.project_id);
                foreach (var d in committedDate)
                {
                    var result = dataList.Where(s => s.committed_date == d).First();
                    if (result != null)
                    {
                        dataTotal.Add(result.total);
                        dataAdditions.Add(result.additions);
                        dataDeletions.Add(result.deletions);
                    }
                    else
                    {
                        dataTotal.Add(0);
                        dataAdditions.Add(0);
                        dataDeletions.Add(0);
                    }
                }

                graphList.Add(new GraphView
                {
                    name = p.project_name,
                    type = "line",
                    totalTitle = "Total",
                    totalData = dataTotal,
                    additionsTitle = "Additions",
                    additionsData = dataAdditions,
                    deletionsTitle = "Deletions",
                    deletionsData = dataDeletions,
                });
            }

            return Json(new
            {
                graphData = new { date = committedDate, data = graphList },
                detailData = history
            });
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

            // 图像数据做成
            var memberData = new List<MemberSumView>();
            foreach (var h in history)
            {
                memberData.Add(new MemberSumView
                {
                    committer_id = h.committer_id,
                    committer_name = h.committer_name,
                    committed_date = h.committed_date.ToShortDateString(),
                    committed_date_id = h.committed_date.ToString("yyyymmdd"),
                    additions = h.additions,
                    deletions = h.deletions,
                    total = h.total
                }); ; ; ;
            }

            var sumData = from s in memberData
                group s by new { s.committer_id, s.committer_name, s.committed_date,s.committed_date_id } into g
                select new
                {
                    g.Key.committer_id,
                    g.Key.committer_name,
                    g.Key.committed_date,
                    g.Key.committed_date_id,
                    additions = g.Sum(i => i.additions),
                    deletions = g.Sum(i => i.deletions),
                    total = g.Sum(i => i.total)
                };

            var memberID = (from s in sumData select new { s.committer_id, s.committer_name }).Distinct();
            var committedDate = (from s in sumData select new { s.committed_date_id, s.committed_date }).Distinct().OrderBy(i => i.committed_date_id).Select(c => c.committed_date);
                                                                                                                     
            var graphList = new List<GraphView>();

            foreach (var p in memberID)
            {
                var dataTotal = new List<int>();
                var dataAdditions = new List<int>();
                var dataDeletions = new List<int>();
                var dataList = sumData.Where(s => s.committer_id == p.committer_id);
                foreach (var d in committedDate)
                {
                    var result = dataList.Where(s => s.committed_date == d).First();
                    if (result != null)
                    {
                        dataTotal.Add(result.total);
                        dataAdditions.Add(result.additions);
                        dataDeletions.Add(result.deletions);
                    }
                    else
                    {
                        dataTotal.Add(0);
                        dataAdditions.Add(0);
                        dataDeletions.Add(0);
                    }
                }

                graphList.Add(new GraphView
                {
                    name = p.committer_name,
                    type = "line",
                    totalTitle = "Total",
                    totalData = dataTotal,
                    additionsTitle = "Additions",
                    additionsData = dataAdditions,
                    deletionsTitle = "Deletions",
                    deletionsData = dataDeletions,
                });
            }

            return Json(new
            {
                graphData = new { date = committedDate, data = graphList },
                detailData = history
            });
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

    public class WareSumView
    {
        public int project_id { get; set; }
        public string project_name { get; set; }
        public string committed_date { get; set; }
        public string committed_date_id { get; set; }
        public int additions { get; set; }
        public int deletions { get; set; }
        public int total { get; set; }
    }

    public class MemberSumView
    {
        public string committer_id { get; set; }
        public string committer_name { get; set; }
        public string committed_date { get; set; }
        public string committed_date_id { get; set; }
        public int additions { get; set; }
        public int deletions { get; set; }
        public int total { get; set; }
    }

    public class GraphView
    {
        public string name { get; set; }
        public string type  { get; set; }
        public string totalTitle { get; set; }
        public List<int> totalData { get; set; }
        public string additionsTitle { get; set; }
        public List<int> additionsData { get; set; }
        public string deletionsTitle { get; set; }
        public List<int> deletionsData { get; set; }
    }
}