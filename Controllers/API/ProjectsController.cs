using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using GitlabManager.DataContext;
using GitLabManager.DataContext;
using GitlabManager.Models;
using System.Configuration;
using Newtonsoft.Json;
using LibGit2Sharp;
using System.IO;

namespace GitLabManager.Controllers.API
{
    public class ProjectsController : ApiController
    {
        public static ApplicationDbContext db = new ApplicationDbContext();

        public static AgoraDbContext db_agora = new AgoraDbContext();

        [HttpGet]
        public IHttpActionResult GetLocationGroup()
        {
            try
            {
                // 有效的所有群组
                var allGroups = db.NameSpaces.Where(i => i.type == "Group" && i.id != 2 && i.id != 10 && i.id != 15).ToList();

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
                    resultJson = ChildrenData(rootGroup[i], allGroups, resultJson);

                    if (i != rootGroup.Count - 1)
                    {
                        resultJson += StringJson("comma");
                    }
                }

                resultJson += StringJson("end");

                return Json(new { location = nameSpaces, group = resultJson });
            }
            catch (Exception ex)
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
            catch (Exception ex)
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
                string http_url_to_repo = "http://172.17.5.146/public-playground/2200714/newProject.git";
                InitProject(http_url_to_repo, req);

                return null;
                // 1.调用api创建仓库(设定成员、使用期限)
                var ret = CreateWareHouse(req.name, req.location, req.description,req.user_id,req.expiryDate);

                if (ret != null && ret.flag == true)
                {
                    // 2.初期化仓库(readme，gitignore)
                    InitProject(ret.http_url_to_repo, req);
                }

                return Json(ret);
            }
            catch (Exception ex)
            {
                return Json(new List<string>());
            }
        }

        private void InitProject(string web_url, WHCreateReq req)
        {
            string tmpWork = AppDomain.CurrentDomain.BaseDirectory + "\\TempWork";
            string baseFolder = tmpWork + "\\" + Guid.NewGuid().ToString("N");
            string token = ConfigurationManager.AppSettings["gitlab_token1"];

            try
            {
                // 1.创建工作目录
                Directory.CreateDirectory(baseFolder);

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
                string source = AppDomain.CurrentDomain.BaseDirectory + "\\Template\\readme\\" + req.readmePrefix + "_readme.md";
                File.Copy(source, baseFolder + "\\readme.md");

                // 5. gitignore 文件做成
                source = AppDomain.CurrentDomain.BaseDirectory + "\\Template\\gitignore\\" + req.gitignore;
                File.Copy(source, baseFolder + "\\.gitignore");

                // 6.main 分支代码push
                PushMainBranch(token, baseFolder);

                // 7.根据分支策略创建其他分支
                PushOrtherBranch();

                // 删除作业文件夹
                Directory.Delete(baseFolder, true);
            } 
            catch(Exception ex)
            {
                Directory.Delete(baseFolder, true);
            }
        }

        private void PushMainBranch(string token,string repository)
        {
            // push认证信息
            var pushOption = new PushOptions()
            {
                CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = "oauth2", Password = token }
            };
        }
        private void PushOrtherBranch()
        {

        }
        private ReturnResult CreateWareHouse(string name, string location_id, string description,string user_id, string expDate)
        {
            string  result = "";
            string token = ConfigurationManager.AppSettings["gitlab_token1"];
            string api = ConfigurationManager.AppSettings["gitlab_instance"] + "projects";

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", token);

            //返回结果定义
            var rr = new ReturnResult();

            try
            {
                var httpContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("name", name),
                        new KeyValuePair<string, string>("namespace_id", location_id),
                        new KeyValuePair<string, string>("description", description) });

                var response = httpClient.PostAsync(api, httpContent).Result;
                result = response.Content.ReadAsStringAsync().Result;

                rr = JsonConvert.DeserializeObject<ReturnResult>(result);
                rr.message = "仓库创建成功";
            }
            catch (Exception ex)
            {
                rr.message = "仓库创建失败;" + result;
                rr.flag = false;
                return rr;
            }

            try 
            { 
                // 取得申请者信息
                User user = db.Users.Where(i => i.username.Equals(user_id)).FirstOrDefault();

                // 仓库添加成员(申请者)，设定仓库有效期限
                var httpContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("user_id", user.id.ToString()),
                        new KeyValuePair<string, string>("expires_at", expDate),
                        new KeyValuePair<string, string>("access_level", "40")
                });

                var response = httpClient.PostAsync(rr._links.members, httpContent).Result;
                result = response.Content.ReadAsStringAsync().Result;

                rr.flag = true;
                return rr;
            }
            catch (Exception ex)
            {
                rr.message = "仓库创建成功，添加成员，设定有效期限失败！;" + ex.Message;
                rr.flag = false;
                return rr;
            }
        }

        private string ChildrenData(Models.NameSpaces ns, List<Models.NameSpaces> allGroups, string resultJson)
        {
            var subGroup = allGroups.Where(i => i.parent_id == ns.id.ToString()).ToList();
            if (subGroup == null || subGroup.Count == 0)
            {
                resultJson += StringJson("body_end");
            }
            else
            {
                for (var i = 0; i < subGroup.Count; i++)
                {
                    resultJson += StringJson("body_start", subGroup[i].id.ToString(), subGroup[i].name);
                    resultJson = ChildrenData(subGroup[i], allGroups, resultJson);
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