using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Configuration;
using System.Net.Http;
using GitlabManager.App_Start;
using System.ComponentModel.DataAnnotations;
using System;
using GitLabManager.DataContext;
using System.Web.Http;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web;

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
    }

    public class CommitInfo
    {
        public string Id { get; set; }
    }

    public class BranchInfo
    {
        public string name { get; set; }
    }

    [Table("commits_history", Schema = "public")]
    public class CommitDetail
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string id { get; set; }

        public int project_id { get; set; }
        public string project_name { get; set; }
        public string commit_id { get; set; }
        public string message { get; set; }
        public string committer_id { get; set; }
        public string committer_name { get; set; }
        public string committer_email { get; set; }
        public string committed_date { get; set; }

        public stats stats { get; set; }
        public string sync_time { get; set; }
    }

    public class stats
    {
        public int additions { get; set; }
        public int deletions { get; set; }
        public int total { get; set; }
    }
}