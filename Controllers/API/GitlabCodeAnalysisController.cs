using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Configuration;
using System.Net.Http;
using GitlabManager.App_Start;
using System.Data.Entity;
using System;
using GitLabManager.DataContext;
using System.Web.Http;
using System.Web;
using GitLabManager.Models;
using System.IO;
using GetUserAvatar.Models;
using GitLabManager.Common.Log;

namespace GitLabManager.Controllers
{
    [ErrorExceptionFilter]
    [ApiAuthorize]
    public class GitlabCodeAnalysisController : ApiController
    {
        public  static void GetDataRsync2()
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory + "\\LOG";
            Directory.CreateDirectory(folder);
            string errlog = "";
            string logFile = folder + "\\commit_log.txt";
            var sws = new StreamWriter(logFile, true, System.Text.Encoding.UTF8);

            var days = -700;
            var api = ConfigurationManager.AppSettings["gitlab_instance"];
            var token = ConfigurationManager.AppSettings["gitlab_token1"];

            var st = Convert.ToDateTime(DateTime.Now.AddDays(Convert.ToInt32(days)).ToShortDateString());
            var ed = Convert.ToDateTime(DateTime.Now.ToShortDateString());

            sws.WriteLine("start:" + DateTime.Now.ToString());
            
            var projects = DBCon.db.Projects.Where(i => i.last_activity_at > st && i.last_activity_at <= ed).OrderBy(i => i.id).ToList();

            var id_his = DBCon.db_agora.CommitHistory.Select(i => i.project_id).Distinct().ToList();
            foreach (var id in id_his)
            {
                var pj = projects.Where(p => p.id == id).FirstOrDefault();
                projects.Remove(pj);
            }

            if (projects == null || projects.Count == 0)
            {
                sws.WriteLine("没有提交履历");
                sws.WriteLine("end:" + DateTime.Now.ToString());
                sws.Close();
                return;
            }
            var users = DBCon.db.Users.ToList();

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", token);
            sws.Close();

            var doing = "";
            foreach (var pj in projects)
            {
                if (pj.id == 1986)
                {
                    continue;
                }
                sws = new StreamWriter(logFile, true, System.Text.Encoding.UTF8);
                sws.WriteLine("start:" + pj.name);
                try
                {
                    doing = pj.id + "_" + pj.name;
                    var commitIdList = new List<CommitInfo>();
                    var commitList = new List<CommitDetail>();

                    //分支名取得
                    var response = httpClient.GetAsync(api + "projects/" + pj.id + "/repository/branches?per_page=50").Result;
                    if (response.StatusCode.ToString() != "OK")
                    {
                        sws.WriteLine("end:" + pj.name + "没有分支");
                        sws.Close();
                        continue;
                    }

                    int TotalPages = int.Parse(response.Headers.GetValues("X-Total-Pages").First());
                    int Page = int.Parse(response.Headers.GetValues("X-Page").First());
                    while (Page <= TotalPages)
                    {
                        response = httpClient.GetAsync(api + "projects/" + pj.id + "/repository/branches?per_page=50&page=" + Page.ToString()).Result;
                        var result = response.Content.ReadAsStringAsync().Result;
                        try
                        {
                            if (response.StatusCode.ToString() == "OK")
                            {
                                var branchList = JsonConvert.DeserializeObject<List<BranchInfo>>(result);
                                foreach (var b in branchList)
                                {
                                    // 代码提交ID
                                    try
                                    {
                                        Thread.Sleep(20);
                                        var responseSub = httpClient.GetAsync(api + "projects/" + pj.id + "/repository/commits?ref_name=" + b.name + "&per_page=9999").Result;
                                        result = responseSub.Content.ReadAsStringAsync().Result;
                                        var commits = JsonConvert.DeserializeObject<List<CommitInfo>>(result);
                                        foreach (var c in commits)
                                        {
                                            commitIdList.Add(new CommitInfo { Id = c.Id });
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        sws.WriteLine("bran1:" + pj.name + ex.Message + ex.StackTrace);
                                        continue;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            sws.WriteLine("bran2:" + pj.name + ex.Message + ex.StackTrace);
                            continue;
                        }

                        // 分支分页
                        if (Page < TotalPages)
                        {
                            Page = int.Parse(response.Headers.GetValues("X-Next-Page").First());
                        }
                        else
                        {
                            Page = TotalPages + 1;
                        }
                    }

                    // 课题别所有提交ID取得
                    var commitAll = commitIdList.Where((x, i) => commitIdList.FindIndex(s => s.Id == x.Id) == i).ToList();

                    if (commitAll == null || commitAll.Count == 0)
                    {
                        sws.WriteLine("end:" + pj.name + "没有分支2");
                        sws.Close();
                        continue;
                    }

                    // 代码提交履历取得
                    foreach (var c in commitAll)
                    {
                        var url_sha = api + "projects/" + pj.id + "/repository/commits/" + c.Id;

                        response = httpClient.GetAsync(url_sha).Result;
                        var result = response.Content.ReadAsStringAsync().Result;
                        var commitDetail = JsonConvert.DeserializeObject<CommitDetail>(result);
                        commitDetail.commit_id = commitDetail.id;
                        commitDetail.project_name = pj.name;
                        //commitDetail.message = "";

                        if (commitDetail.committer_email != null)
                        {
                            try
                            {
                                var user = users.Where(i => i.email == commitDetail.committer_email).FirstOrDefault();
                                if (user != null)
                                {
                                    commitDetail.committer_id = user.username;
                                    commitDetail.committer_name = user.name;
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
                        if (commitDetail.stats != null && commitDetail.committed_date != null)
                        {
                            commitList.Add(commitDetail);
                        }
                    }

                    var history = DBCon.db_agora.CommitHistory.Where(c => c.project_id == pj.id);

                    //旧的数据删除
                    if (history != null && history.Count() > 0)
                    {
                        DBCon.db_agora.CommitHistory.RemoveRange(history);
                        DBCon.db_agora.SaveChanges();
                    }

                    // 添加新的数据
                    DBCon.db_agora.CommitHistory.AddRange(commitList);
                    DBCon.db_agora.SaveChanges();

                    sws.WriteLine("end:" + pj.name);
                    sws.Close();
                }
                catch (Exception ex)
                {
                    errlog = pj.name + ";" + ex.StackTrace + ex.Message + ";\n";
                    sws.WriteLine(errlog);
                    sws.Close();
                    continue;
                }
            }

            sws = new StreamWriter(logFile, true, System.Text.Encoding.UTF8);
            sws.WriteLine("total:" + projects.Count.ToString() + ",max：" + projects[projects.Count - 1].id + ";using:" + doing);
            sws.WriteLine("end:" + DateTime.Now.ToString());
            sws.Close();
            return;
        }

        public  static void GetDataRsync()
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory + "\\LOG";
            Directory.CreateDirectory(folder);
            string errlog = "";
            string logFile = folder + "\\commit_log.txt";
            var sws = new StreamWriter(logFile, true, System.Text.Encoding.UTF8);

            var days = -1;
            var api = ConfigurationManager.AppSettings["gitlab_instance"];
            var token = ConfigurationManager.AppSettings["gitlab_token1"];
            var st = Convert.ToDateTime(DateTime.Now.AddDays(Convert.ToInt32(days)).ToShortDateString());

            sws.WriteLine("start:" + DateTime.Now.ToString());

            var projects = DBCon.db.Projects.Where(i => i.last_activity_at > st).OrderBy(i => i.id).ToList();

            if (projects == null || projects.Count == 0)
            {
                sws.WriteLine("没有提交履历");
                sws.WriteLine("end:" + DateTime.Now.ToString());
                sws.Close();
                return;
            }
            var users = DBCon.db.Users.ToList();

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", token);
            sws.Close();

            var doing = "";
            foreach (var pj in projects)
            {
                if (pj.id == 1986)
                {
                    continue;
                }
                sws = new StreamWriter(logFile, true, System.Text.Encoding.UTF8);
                sws.WriteLine("start:" + pj.name);
                try
                {
                    doing = pj.id + "_" + pj.name;
                    var commitIdList = new List<CommitInfo>();
                    var commitList = new List<CommitDetail>();

                    //分支名取得
                    var response = httpClient.GetAsync(api + "projects/" + pj.id + "/repository/branches?per_page=50").Result;
                    if(response.StatusCode.ToString() != "OK")
                    {
                        sws.WriteLine("end:" + pj.name + "没有分支");
                        sws.Close();
                        continue ;
                    }

                    int TotalPages = int.Parse(response.Headers.GetValues("X-Total-Pages").First());
                    int Page = int.Parse(response.Headers.GetValues("X-Page").First());
                    while (Page <= TotalPages)
                    {
                        response = httpClient.GetAsync(api + "projects/" + pj.id + "/repository/branches?per_page=50&page=" + Page.ToString()).Result;
                        var result = response.Content.ReadAsStringAsync().Result;
                        try
                        {
                            if (response.StatusCode.ToString() == "OK")
                            {
                                var branchList = JsonConvert.DeserializeObject<List<BranchInfo>>(result);
                                foreach (var b in branchList)
                                {
                                    // 代码提交ID
                                    try
                                    {
                                        Thread.Sleep(20);
                                        var responseSub = httpClient.GetAsync(api + "projects/" + pj.id + "/repository/commits?ref_name=" + b.name + "&per_page=9999").Result;
                                        result = responseSub.Content.ReadAsStringAsync().Result;
                                        var commits = JsonConvert.DeserializeObject<List<CommitInfo>>(result);
                                        foreach (var c in commits)
                                        {
                                            commitIdList.Add(new CommitInfo { Id = c.Id });
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        sws.WriteLine("bran1:" + pj.name + ex.Message + ex.StackTrace);
                                        continue;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            sws.WriteLine("bran2:" + pj.name + ex.Message + ex.StackTrace);
                            continue;
                        }

                        // 分支分页
                        if (Page < TotalPages)
                        {
                            Page = int.Parse(response.Headers.GetValues("X-Next-Page").First());
                        }
                        else
                        {
                            Page = TotalPages + 1;
                        }
                    }

                    // 课题别所有提交ID取得
                    var commitAll = commitIdList.Where((x, i) => commitIdList.FindIndex(s => s.Id == x.Id) == i).ToList();
                    if (commitAll == null || commitAll.Count == 0)
                    {
                        sws.WriteLine("end:" + pj.name + "没有分支2");
                        sws.Close();
                        continue;
                    }

                    // 代码提交履历取得
                    foreach (var c in commitAll)
                    {
                        var url_sha = api + "projects/" + pj.id + "/repository/commits/" + c.Id;
                        response = httpClient.GetAsync(url_sha).Result;
                        var result = response.Content.ReadAsStringAsync().Result;
                        var commitDetail = JsonConvert.DeserializeObject<CommitDetail>(result);
                        commitDetail.commit_id = commitDetail.id;
                        commitDetail.project_name = pj.name;

                        if (commitDetail.committer_email != null)
                        {
                            try
                            {
                                var user = users.Where(i => i.email == commitDetail.committer_email).FirstOrDefault();
                                if (user != null)
                                {
                                    commitDetail.committer_id = user.username;
                                    commitDetail.committer_name = user.name;
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
                        if (commitDetail.stats != null && commitDetail.committed_date != null)
                        {
                            commitList.Add(commitDetail);
                        }
                    }

                    //旧的数据删除
                    var history = DBCon.db_agora.CommitHistory.Where(c => c.project_id == pj.id);
                    if (history != null && history.Count() > 0)
                    {
                        DBCon.db_agora.CommitHistory.RemoveRange(history);
                        DBCon.db_agora.SaveChanges();
                    }

                    // 添加新的数据
                    DBCon.db_agora.CommitHistory.AddRange(commitList);
                    DBCon.db_agora.SaveChanges();

                    sws.WriteLine("end:" + pj.name);
                    sws.Close();
                }
                catch (Exception ex)
                {
                    errlog = pj.name + ";" + ex.StackTrace + ex.Message + ";\n";
                    sws.WriteLine(errlog);
                    sws.Close();
                    continue;
                }
            }

            sws = new StreamWriter(logFile, true, System.Text.Encoding.UTF8);
            sws.WriteLine("total:" + projects.Count.ToString() + ",max：" + projects[projects.Count - 1].id + ";using:" + doing);
            sws.WriteLine("end:" + DateTime.Now.ToString());
            sws.Close();
            return;
        }
        [HttpGet]
        public IHttpActionResult GetWareHouse()
        {
            string namespace_id = HttpContext.Current.Request.QueryString["namespace_id"];
            var projects = DBCon.db_two.Projects.Where(p => p.namespace_id == namespace_id);
            return Json(projects);
        }

        [HttpGet]
        public IHttpActionResult GetMembers()
        {
            var iden = DBCon.db_two.Identities;
            var users = DBCon.db_two.Users;

            var list = from u in users
                       join i in iden on u.id equals i.user_id
                        where u.state == "active" && i.provider == "ldapmain"
                        select new userView { id = u.id,name = u.name,username = u.username,email = u.email };

            return Json(list);
        }

        private List<userView> GetMembersAvatarUrl()
        {
            string gitlabUrl = ConfigurationManager.AppSettings["gitlab_url"];
            string persionFace = ConfigurationManager.AppSettings["persion_face"].Replace("match(gitlab_url)", gitlabUrl);
            string defaultFace = ConfigurationManager.AppSettings["default_face"].Replace("match(gitlab_url)", gitlabUrl);

            var iden = DBCon.db_two.Identities;
            var users = DBCon.db_two.Users;
            var list = (from u in users
                       join i in iden on u.id equals i.user_id
                       where u.state == "active" && i.provider == "ldapmain"
                       select new userView { id = u.id, name = u.name, username = u.username, email = u.email }).ToList();

            foreach(var user in list)
            {
                if (user.url == null || user.url == "")
                {
                    user.url = defaultFace;
                }
                else
                {
                    user.url = persionFace + user.id + "/" + user.url;
                }
            }
            return list;
        }


        [HttpPost]
        public IHttpActionResult GetGraphData(RequestInput input)
        { 
            var list = input.idList;
            var flag = input.flag;

            var graphList = new List<GraphView>();
            var graphUserList = new List<GraphView>();

            if (list == null || list.Count == 0 ||( list.Count == 1 && list[0] == "null"))
            {
                return Json(new { date = new List<string>() { }, dataProject = graphList, dataUser = graphUserList });
            }

            string gitlabUrl = ConfigurationManager.AppSettings["gitlab_url"];
            string defaultFace = ConfigurationManager.AppSettings["default_face"].Replace("match(gitlab_url)", gitlabUrl);

            try
            {
                var users = GetMembersAvatarUrl();
                var history = new List<CommitView>();
                var commitHistory = DBCon.db_agora_two.CommitHistory.ToList();
                if (flag == "p")
                {
                    history = (from h in commitHistory
                        where list.Any(p => p == h.project_id.ToString())
                        select new CommitView
                        {
                            commit_id = h.commit_id,
                            project_id = h.project_id,
                            project_name = h.project_name,
                            additions = h.stats.additions,
                            deletions = h.stats.deletions,
                            committer_id = h.committer_id,
                            committer_name = h.committer_name,
                            committer_email = h.committer_email,
                            committed_date = h.committed_date
                        }).ToList();
                }
                else
                {
                    history = (from h in commitHistory
                        where list.Any(p => p == h.committer_id)
                        select new CommitView
                        {
                            commit_id = h.commit_id,
                            project_id = h.project_id,
                            project_name = h.project_name,
                            additions = h.stats.additions,
                            deletions = h.stats.deletions,
                            committer_id = h.committer_id,
                            committer_name = h.committer_name,
                            committer_email = h.committer_email,
                            committed_date = h.committed_date
                        }).ToList();
                }

                // 图像数据做成
                var wareData = new List<SumView>();
                foreach(var h in history)
                {
                    wareData.Add(new SumView
                    {
                        project_id = h.project_id,
                        committer_id = h.committer_id,
                        committer_name=h.committer_name,
                        project_name = h.project_name,
                        committed_date = h.committed_date.Substring(0,10),
                        committed_date_id = h.committed_date.Substring(0, 10).Replace("-","").Replace(" ",""),
                        additions = h.additions,
                        deletions = h.deletions,
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
                        counts = g.Count()
                    } ;

                var sumDataUser = from s in wareData
                    group s by new { s.committer_id, s.committer_name, s.committed_date, s.committed_date_id } into g
                    select new
                    {
                        g.Key.committer_id,
                        g.Key.committer_name,
                        g.Key.committed_date_id,
                        g.Key.committed_date,
                        additions = g.Sum(i => i.additions),
                        deletions = g.Sum(i => i.deletions),
                        counts = g.Count()
                    };

                var projects = (from s in sumData group s by new { s.project_id , s.project_name } into g select new ProjectUrls { project_id = g.Key.project_id, project_name = g.Key.project_name }).ToList();
                var userInfo = (from s in sumDataUser select new { s.committer_id, s.committer_name }).Distinct().OrderBy(i => i.committer_id).ToList();
                var committedDate = (from s in sumData select new { s.committed_date_id, s.committed_date }).Distinct().OrderBy(i => i.committed_date_id).Select(c =>c.committed_date).ToList();

                //var urlProject = ProjectURL(projects);

                committedDate = DateConvert(committedDate);

                foreach (var u in userInfo)
                {
                    var dataCounts = new List<int>();
                    var dataAdditions = new List<int>();
                    var dataDeletions = new List<int>();
                    var dataList = sumDataUser.Where(s => s.committer_id == u.committer_id).ToList();
                    if (u.committer_id == null)
                    {
                        dataList = sumDataUser.Where(s => s.committer_name == u.committer_name).ToList();
                    }

                    foreach (var d in committedDate)
                    {
                        var result = dataList.Where(s => s.committed_date == d).FirstOrDefault();
                        if (result != null)
                        {
                            dataCounts.Add(result.counts);
                            dataAdditions.Add(result.additions);
                            dataDeletions.Add(result.deletions);
                        }
                        else
                        {
                            dataCounts.Add(0);
                            dataAdditions.Add(0);
                            dataDeletions.Add(0);
                        }
                    }

                    var url = users.Where(i => i.username == u.committer_id).Select(i =>i.url).FirstOrDefault();

                    if (url == null || url == "")
                    {
                        url = defaultFace;
                    }
                    graphUserList.Add(new GraphView
                    {
                        id = u.committer_id,
                        name = u.committer_name,
                        url = url,
                        type = "line",
                        cntTotal = dataCounts.Sum(),
                        countData = dataCounts,
                        addTotal = dataAdditions.Sum(),
                        additionsData = dataAdditions,
                        delTotal = dataDeletions.Sum(),
                        deletionsData = dataDeletions,
                    });
                }

                foreach (var p in projects)
                {
                    var dataCounts = new List<int>();
                    var dataAdditions = new List<int>();
                    var dataDeletions = new List<int>();
                    var dataList = sumData.Where(s => s.project_id == p.project_id).ToList();

                    foreach (var d in committedDate)
                    {
                        var result = dataList.Where(s => s.committed_date == d).FirstOrDefault();
                        if (result != null)
                        {
                            dataCounts.Add(result.counts);
                            dataAdditions.Add(result.additions);
                            dataDeletions.Add(result.deletions);
                        }
                        else
                        {
                            dataCounts.Add(0);
                            dataAdditions.Add(0);
                            dataDeletions.Add(0);
                        }
                    }

                    graphList.Add(new GraphView
                    {
                        name = p.project_name,
                        url = p.Url,
                        id = p.project_id.ToString(),
                        type = "line",
                        cntTotal = dataCounts.Sum(),
                        countData = dataCounts,
                        addTotal = dataAdditions.Sum(),
                        additionsData = dataAdditions,
                        delTotal = dataDeletions.Sum(),
                        deletionsData = dataDeletions,
                    });
                }
                return Json( new { date = committedDate, dataProject = graphList, dataUser = graphUserList });
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        private List<string> DateConvert(List<string> commit_date)
        {
            var s = Convert.ToDateTime(commit_date.Min());
            var e = Convert.ToDateTime(commit_date.Max());

            var days = DateDiff(s,e);
            var dateList = new List<string>();
            dateList.Add(s.ToString("yyyy-MM-dd"));

            for (var i=1; i <= days; i++)
            {
                dateList.Add(s.AddDays(i).ToString("yyyy-MM-dd"));
            }
            return dateList;
        }

        private int DateDiff(DateTime dateStart, DateTime dateEnd)
        {
            DateTime start = Convert.ToDateTime(dateStart.ToShortDateString());
            DateTime end = Convert.ToDateTime(dateEnd.ToShortDateString());
            TimeSpan sp = end.Subtract(start);
            return sp.Days;
        }

        public class RequestInput
        {
            public List<string> idList { get; set; }
            public string flag { get; set; }
        }
    }

    public class CommitInfo
    {
        public string Id { get; set; }
    }



    public class ProjectUrls
    {
        public int project_id { get; set; }
        public string project_name { get; set; }
        public string Url { get; set; }
    }

    public class BranchInfo
    {
        public string name { get; set; }
        public string message { get; set; }
    }

    public class userView
    {
        public int id { get; set; }
        public string name { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string url { get; set; }
    }

    public class CommitView
    {
        public int project_id { get; set; }
        public string project_name { get; set; }
        public string commit_id { get; set; }
        //public string message { get; set; }
        public string committer_id { get; set; }
        public string committer_name { get; set; }
        public string committer_email { get; set; }
        public string committed_date { get; set; }
        public int additions { get; set; }
        public int deletions { get; set; }
        public int committed_date2 { get; set; }
    }

    public class SumView
    {
        public int project_id { get; set; }
        public string project_name { get; set; }
        public string committed_date { get; set; }
        public string committed_date_id { get; set; }
        public string committer_id { get; set; }
        public string committer_name { get; set; }
        public int additions { get; set; }
        public int deletions { get; set; }
        public int count { get; set; }
    }


    public class GraphView
    {
        public string name { get; set; }
        public string id { get; set; }
        public string url { get; set; }
        public string type  { get; set; }

        public List<int> countData { get; set; }

        public List<int> additionsData { get; set; }

        public List<int> deletionsData { get; set; }

        public int cntTotal { get; set; }
        public int addTotal { get; set; }
        public int delTotal { get; set; }
    }

    public class Identites
    {
        public int id { get; set; }
        public string provider { get; set; }
        public string user_id { get; set; }
    }
}