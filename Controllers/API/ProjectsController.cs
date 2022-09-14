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
                foreach (var nameSpace in rootGroup)
                {
                    // 顶级群组内容表示变换
                    var ns = new NameSpaces
                    {
                        id = nameSpace.id,
                        name = nameSpace.name,
                        nameView = NameView(nameSpace.id)
                    };
                    nameSpaces.Add(ns);

                    // 子节点组数据取得
                    var _ = ChildrenData(nameSpace.id.ToString(), allGroups);
                }

                return Json(nameSpaces);
            }
            catch (Exception ex)
            {
                return Json(new NameSpaces { });
            }
        }

        private string ChildrenData(string id ,List<Models.NameSpaces> allGroups)
        {
            var subGroup = allGroups.Where(i => i.parent_id == id).ToList();
            if (subGroup == null || subGroup.Count == 0)
            {
                // todo
            }
            else
            {
                foreach (var s in subGroup)
                {
                    ChildrenData(s.id.ToString(), allGroups);
                }
            }

            return "";
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

    }
}