using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using GitlabManager.App_Start;
using GitLabManager.DataContext;
using GitlabManager.Models;
using GitLabManager.Models;
using System.Configuration;
using Newtonsoft.Json;
using LibGit2Sharp;
using System.IO;
using System.Web;

namespace GitLabManager.Controllers.API
{
    [ApiAuthorize]
    public class ProjectsController : ApiController
    {
        // public static ApplicationDbContext db = new ApplicationDbContext();

        // public static AgoraDbContext db_agora = new AgoraDbContext();

        [HttpGet]
        public IHttpActionResult GetLocationGroup()
        {
            try
            {
                var flag = HttpContext.Current.Request.QueryString["flag"];

                // 有效的所有群组
                var allGroups = DBCon.db.NameSpaces.Where(i => i.type == "Group" && i.id != 2 && i.id != 10 && i.id != 15).ToList();
                var projects = DBCon.db.Projects.ToList();
                // 顶级群组
                var rootGroup = allGroups.Where(i => i.parent_id == null).ToList();

                List<NameSpaces> nameSpaces = new List<NameSpaces>();
                string resultJson = StringJson("start");

                for (var i = 0; i < rootGroup.Count; i++)
                {
                    // 顶级群组内容表示变换
                    var ns = new NameSpaces
                    {
                        id = rootGroup[i].id,
                        name = rootGroup[i].name,
                        nameView = NameView(rootGroup[i].id)
                    };
                    nameSpaces.Add(ns);

                    // 子节点组数据取得
                    resultJson += StringJson("body_start", ns.id.ToString(), ns.name);
                    if (flag != null && flag != "" && flag == "pj")
                    {
                        var subPj = GetProjectsList(ns.id, projects);
                        if (subPj != "")
                        {
                            resultJson += subPj + "," ;
                        }
                    }
                    resultJson = ChildrenData(rootGroup[i], allGroups, resultJson, projects, flag);

                    if (i != rootGroup.Count - 1)
                    {
                        resultJson += StringJson("comma");
                    }
                }

                resultJson += StringJson("end");

                return Json(new { location = nameSpaces, group = resultJson });
            }
            catch
            {
                return Json(new NameSpaces { });
            }
        }

        /// <summary>
        /// Git 忽略文件列表做成
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IHttpActionResult GetIgnoreList()
        {
            try
            {
                string folder = AppDomain.CurrentDomain.BaseDirectory + "\\Template\\gitignore";
                Directory.CreateDirectory(folder);
                var files = Directory.GetFiles(folder);
                var ignoreList = new List<string>();

                foreach (var file in files)
                {
                    ignoreList.Add(Path.GetFileName(file));
                }

                return Json(ignoreList);
            }
            catch
            {
                return Json(new List<string>());
            }
        }
        /// <summary>
        /// 创建仓库
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IHttpActionResult SetWareHouse(WHCreateReq req)
        {
            try
            {
                // 判断仓库名是否存在
                if (IsWareHouseExists(req.name,req.location) == true)
                {
                    return Json(new ReturnResult() { flag = false, message = "仓库名字已经存在！" });
                }

                // 1.调用api创建仓库(设定成员权限、使用期限)
                var ret = CreateWareHouse(req.name, req.location, req.description,req.user_id,req.expiryDate);

                if (ret != null && ret.flag == true)
                {
                    // 2.初期化仓库(readme，gitignore,创建默认分支)
                    InitProject(ret.http_url_to_repo, req);
                }
                else
                {
                    return Json(ret);
                }

                // 3.关联QCD项目
                SetQcdProject(req,ret);

                return Json(ret);
            }
            catch (Exception ex)
            {
                return Json(new ReturnResult() { flag = false,message = ex.Message});
            }
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

        private bool IsWareHouseExists(string name,string ns_id)
        {
            var pj = DBCon.db.Projects.Where(i => i.namespace_id == ns_id && i.name == name).ToList();
            if (pj.Count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
       
        private string GetProjectsList(int nsId,List<Projects> projects)
        {
            string result = "";
            var pj = (from p in projects where p.namespace_id == nsId.ToString() select new {p.id,p.name}).ToList() ;
            if (pj != null && pj.Count > 0)
            {
                for (var i = 0; i < pj.Count; i++)
                {
                    result += "{\"value\": \"" + pj[i].id + "\",\"label\": \"" + pj[i].name + "\"}";

                    if (i < pj.Count - 1)
                    {
                        result += ",";
                    }
                }
            }

            return result;
        }

        private void SetQcdProject(WHCreateReq req,ReturnResult rr)
        {
            // 根据id检索出既存信息
            var _agre = DBCon.db_agora.Agreements.Where(i => i.agreement_cd == req.qcdId).FirstOrDefault();

            if (_agre != null )
            {
                var pjList = new List <Projects>();

                if (_agre.repository_ids != null)
                {
                    // json数据反序列化
                    pjList = JsonConvert.DeserializeObject<List<Projects>>(_agre.repository_ids);
                }

                string[] path = rr.web_url.Split('/');
                var dir = path [path.Length - 2];

                // 添加新仓库关联信息
                var newPj = new Projects
                {
                    id = Convert.ToInt16(rr.id),
                    namespace_id = req.location,
                    name = dir + " / " + req.name,
                    description = req.description
                };

                pjList.Add(newPj);

                //无效的课题删除
                var idList = new List <int>();
                foreach(var pj in pjList)
                {
                    idList.Add(pj.id);
                }

                var result = from p in DBCon.db.Projects where idList.Contains(p.id) select new { p.id };

                var pjListNew = new List<Projects>();
                foreach (var pj in pjList)
                {
                    var cnt = result.Where(i => i.id == pj.id).ToList().Count;
                    if (cnt > 0)
                    {
                        pjListNew.Add(pj);
                    }
                }

                //对象数据序列化
                var repositoryIds = JsonConvert.SerializeObject(pjListNew);

                // 设定变更内容
                _agre.repository_ids = repositoryIds;
                _agre.project_count = _agre.project_count == null ? "1" : (pjListNew.Count).ToString();
                _agre.updated_by = req.user_id;
                _agre.updated_at = DateTime.Now;

                // 标记数据更新状态
                DBCon.db_agora.Entry(_agre).State = EntityState.Modified;

                // 保存数据变更
                int dbstate = DBCon.db_agora.SaveChanges();
            }
        }

        private void InitProject(string web_url, WHCreateReq req)
        {
            string tmpWork = AppDomain.CurrentDomain.BaseDirectory + "\\.TempData";
            string baseFolder = tmpWork + "\\" + Guid.NewGuid().ToString("N");
            string token = ConfigurationManager.AppSettings["gitlab_token1"];

            try
            {
                // 1.创建工作目录
                Directory.CreateDirectory(baseFolder);
                Directory.Delete(baseFolder, true);
                // 2. 从gitlab网站上取得项目代码，放入工作目录里面（workFolder）
                var co = new CloneOptions()
                {
                    CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = "oauth2", Password = token }
                };
                string logMsg = Repository.Clone(web_url, @baseFolder, co);

                //3. language文件做成
                var sws = new StreamWriter(baseFolder + "\\language.conf", true, System.Text.Encoding.UTF8);
                foreach (var la in req.language.Split(','))
                {
                    sws.WriteLine(la);
                }
                sws.Close();

                // 4. readme 文件做成
                string source = AppDomain.CurrentDomain.BaseDirectory + "\\Template\\readme\\" + req.readmePrefix + "_README.md";
                File.Copy(source, baseFolder + "\\README.md",true);

                // 5. gitignore 文件做成
                source = AppDomain.CurrentDomain.BaseDirectory + "\\Template\\gitignore\\" + req.gitignore;
                File.Copy(source, baseFolder + "\\.gitignore",true);

                // 6.main 分支代码push
                var isSuccess = PushMainBranch(token, baseFolder,req.qcdId);

                if (isSuccess)
                {
                    // 7.根据分支策略创建其他分支
                    PushOrtherBranch(token, baseFolder, req.branchType);
                }
            } 
            catch{}
            finally
            {
                // 删除作业文件夹
                DirectoryInfo dir = new DirectoryInfo(@baseFolder);
                SetGitFilesNormal(dir);
                try { Directory.Delete(@baseFolder, true); }catch
                {
                    SetGitFilesNormal(dir);
                    try { Directory.Delete(@baseFolder, true); }catch { } 
                }
            }
        }

        private bool PushMainBranch(string token,string repository,string qcdid = "")
        {
            try
            {
                // 根据id检索出既存信息
                var _agre = DBCon.db_agora.Agreements.Where(i => i.agreement_cd == qcdid).FirstOrDefault();
                var commitMsg = "main 初始化（关联项目" + _agre.agreement_cd + " " + _agre.agreement_name + ")";
                using (var repo = new Repository(@repository))
                {
                    var remote = repo.Network.Remotes["origin"];

                    string pushRefSpec = @"refs/heads/main";

                    // 创建做成者(提交者)
                    var author = new Signature("Administrator", "gitlab-admin@cn.tre-inc.com", DateTimeOffset.Now);

                    // 暂存变更文件
                    Commands.Stage(repo, "*");

                    // 本地提交
                    repo.Commit(commitMsg, author, author);

                    // push认证信息
                    var pushOption = new PushOptions()
                    {
                        CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = "oauth2", Password = token }
                    };

                    // 推送代码到远程
                    repo.Network.Push(remote, pushRefSpec, pushOption);

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool PushOrtherBranch(string token, string repository,string branchType)
        {
            try
            {
                using (var repo = new Repository(@repository))
                {
                    //取得远程源信息
                    Remote remote = repo.Network.Remotes["origin"];

                    var branches = BranchList(branchType);
                    if (branches == null || branches.Count == 0)
                    {
                        return true;
                    }

                    foreach (var branch in branches)
                    {
                        // 取得本地分支
                        var localBranch = repo.Branches[branch];

                        if (localBranch == null)
                        {
                            // 本地分支不存在的情况下则创建本地分支
                            localBranch = repo.CreateBranch(branch);
                        }

                        // 关联远程分支
                        repo.Branches.Update(localBranch,
                            b => b.Remote = remote.Name,
                            b => b.UpstreamBranch = localBranch.CanonicalName);

                        // 认证信息
                        var pushOption = new PushOptions()
                        {
                            CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = "oauth2", Password = token }
                        };

                        // 推送代码到远程仓库
                        repo.Network.Push(localBranch, pushOption);
                    }

                    return true;
                }
            }
            catch
            {
                return false;
            }
          
        }

        private ReturnResult CreateWareHouse(string name, string location_id, string description,string user_id, string expDate)
        {
            string result = "";
            string errMsg = "仓库创建失败;";
            string token = ConfigurationManager.AppSettings["gitlab_token1"];
            string api = ConfigurationManager.AppSettings["gitlab_instance"] + "projects";

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", token);
            var rr = new ReturnResult();

            try
            {
                // 1. 仓库创建
                var httpContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("name", name),
                        new KeyValuePair<string, string>("namespace_id", location_id),
                        new KeyValuePair<string, string>("description", description),
                        new KeyValuePair<string, string>("default_branch","main"),
                        new KeyValuePair<string, string>("initialize_with_readme","true")
                });
                var response = httpClient.PostAsync(api, httpContent).Result;
                result = response.Content.ReadAsStringAsync().Result;
                rr = JsonConvert.DeserializeObject<ReturnResult>(result);
                
                // 2. 添加成员，设定权限和使用期限
                errMsg = "仓库创建成功，添加成员，设定有效期限失败;";
                User user = DBCon.db.Users.Where(i => i.username.Equals(user_id)).FirstOrDefault();
                httpContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("user_id", user.id.ToString()),
                        new KeyValuePair<string, string>("expires_at", expDate),
                        new KeyValuePair<string, string>("access_level", "40")
                });
                response = httpClient.PostAsync(rr._links.members, httpContent).Result;
                result = response.Content.ReadAsStringAsync().Result;

                rr.message = "仓库创建成功";
                rr.flag = true;
                return rr;
            }
            catch
            {
                rr.message = errMsg + result;
                rr.flag = false;
                return rr;
            }
        }

        private string ChildrenData(Models.NameSpaces ns, List<Models.NameSpaces> allGroups, string resultJson,List<Projects> projects,string flag)
        {
            var subGroup = allGroups.Where(i => i.parent_id == ns.id.ToString()).ToList();
            if (subGroup == null || subGroup.Count == 0)
            {
                if (flag != null && flag != "" && flag == "pj")
                {
                    resultJson += GetProjectsList(ns.id, projects);
                }
                resultJson += StringJson("body_end");
            }
            else
            {
                for (var i = 0; i < subGroup.Count; i++)
                {
                    resultJson += StringJson("body_start", subGroup[i].id.ToString(), subGroup[i].name);
                    resultJson = ChildrenData(subGroup[i], allGroups, resultJson, projects, flag);
                    if (i != subGroup.Count - 1)
                    {
                        resultJson += StringJson("comma");
                    }
                    else
                    {
                        resultJson += StringJson("body_end");
                    }
                }
            }
            return resultJson;
        }

        private class NameSpaces
        {
            public int id { get; set; }
            public string name { get; set; }
            public string nameView { get; set; }
        }

        public class WHCreateReq
        {
            public string name { get; set; }
            public string location { get; set; }
            public string description { get; set; }
            public string qcdId { get; set; }
            public string branchType { get; set; }
            public string language { get; set; }
            public string gitignore { get; set; }
            public string readmePrefix { get; set; }
            public string expiryDate { get; set; }
            public string user_id { get; set; }
        }

        private class ReturnResult
        {
            public string id { get; set; }
            public string web_url { get; set; }
            public string http_url_to_repo { get; set; }
            public Links _links { get; set; }
            public string message { get; set; }
            public bool flag { get; set; }
        }

        public class Links
        {
            public string members { get; set; }
        }

        private string NameView(int id)
        {
            string nameView = "";
            switch (id)
            {
                case 8:
                    nameView = "公司内部研发项目";
                    break;
                case 16:
                    nameView = "公司内部开源项目";
                    break;
                case 17:
                    nameView = "日本事业集团项目";
                    break;
                case 18:
                    nameView = "国内事业研发项目";
                    break;
                case 19:
                    nameView = "国内市场开发项目";
                    break;
                default:
                    break;
            }
            return nameView;
        }

        public List<string> BranchList(string branchType)
        {
           List<string> branchList = null;
            switch (branchType)
            {
                case "1":
                    branchList = new List<string> {};
                    break;
                case "2":
                    branchList = new List<string> { "develop" };
                    break;
                case "3":
                    branchList = new List<string> { "develop","feature" };
                    break;
                case "4":
                    branchList = new List<string> {  "develop", "feature","release" };
                    break;
                case "5":
                    branchList = new List<string> { "develop", "feature", "release","hotfix"};
                    break;
                default:
                    break;
            }
            return branchList;
        }

        private string StringJson(string flag,string value ="",string label = "")
        {
            string jsonPart = "";
            switch (flag)
            {
                case "start":
                    jsonPart = "[";
                    break;
                case "body_start":
                    jsonPart = "{\"value\": \""+ value + "\",\"label\": \"" + label + "\",\"children\":[";
                    break;
                case "body_end":
                    jsonPart = "]}";
                    break;
                case "comma":
                    jsonPart = ",";
                    break;
                case "end":
                    jsonPart = "]";
                    break;
                default:
                    break;
            }
            return jsonPart;
        }
    }
}