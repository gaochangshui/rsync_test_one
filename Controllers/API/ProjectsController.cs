using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using GitlabManager.DataContext;
using GitLabManager.DataContext;
using GitLabManager.Models;
using System.Configuration;
using Newtonsoft.Json;

namespace GitLabManager.Controllers.API
{
    public class ProjectsController : ApiController
    {
        public static ApplicationDbContext db = new ApplicationDbContext();

        public static AgoraDbContext db_agora = new AgoraDbContext();

        [HttpGet]
        public IHttpActionResult GetRootGroup()
        {
            // string id = HttpContext.Current.Request.QueryString["id"];
            // var api = ConfigurationManager.AppSettings["gitlab_instance"];
            // var token = ConfigurationManager.AppSettings["gitlab_token1"];

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
                    resultJson = ChildrenData(rootGroup[i], allGroups, resultJson,i , rootGroup.Count);
                }

                resultJson += StringJson("end");

                return Json(nameSpaces);
            }
            catch (Exception ex)
            {
                return Json(new NameSpaces { });
            }
        }

        private string ChildrenData(Models.NameSpaces ns ,List<Models.NameSpaces> allGroups,string resultJson,int current,int allCount)
        {
            resultJson += StringJson("body_start", ns.id.ToString(), ns.name);

            var subGroup = allGroups.Where(i => i.parent_id == ns.id.ToString()).ToList();
            if (subGroup == null || subGroup.Count == 0)
            {
                if (current != allCount - 1)
                {
                    resultJson += StringJson("comma");
                }
            }
            else
            {
                for (var i = 0; i < subGroup.Count; i++)
                {
                    resultJson = ChildrenData(subGroup[i], allGroups, resultJson,i, subGroup.Count);
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