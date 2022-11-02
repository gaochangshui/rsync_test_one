using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Diagnostics;
using System.IO;
using GitLabManager.DataContext;
using System.Web;
using GitLabManager.Models;
using static ICSharpCode.SharpZipLib.Zip.ExtendedUnixData;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using System.Collections;

namespace GitLabManager.Controllers
{
    public class FeatureHistoryController : ApiController
    {
        [HttpGet]
        public IHttpActionResult GetMark()
        {
            try
            {
                string userId = HttpContext.Current.Request.QueryString["user_id"];
                if (userId == null || userId == "")
                {
                    return BadRequest("请输入用户ID");
                }

                var mst = DBCon.db_agora_two.FeaturesMst.Where(i => i.enabled == true).ToList();
                var history = DBCon.db_agora_two.UserFeatureHistory.Where(i => i.user_id == userId).Select(i =>i.feature_id).ToList();

                var mstNew = (from m in mst where !(history.Any(h => h == m.feature_id)) select m).ToList();
                var rootGroup = mstNew.Where(i => i.parent_id == null && i.enabled == true).ToList();

                string resultJson = StringJson("start");
                for (var i = 0; i < rootGroup.Count; i++)
                {
                    // 子节点组数据取得
                    resultJson += StringJson("body_start", rootGroup[i].feature_id, rootGroup[i].feature_name);
                    resultJson = ChildrenData(rootGroup[i], mstNew, resultJson);

                    if (i != rootGroup.Count - 1)
                    {
                        resultJson += StringJson("comma");
                    }
                }

                resultJson += StringJson("end");

                return Json(new { data = resultJson });
   
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        private string ChildrenData(FeaturesMst ns, List<FeaturesMst> allGroups, string resultJson)
        {
            var subGroup = allGroups.Where(i => i.parent_id == ns.feature_id.ToString()).OrderBy(i => i.feature_id).ToList();
            if (subGroup == null || subGroup.Count == 0)
            {
                resultJson += StringJson("body_end");
            }
            else
            {
                for (var i = 0; i < subGroup.Count; i++)
                {
                    resultJson += StringJson("body_start", subGroup[i].feature_id.ToString(), subGroup[i].feature_name);
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

        private string StringJson(string flag, string value = "", string label = "")
        {
            string jsonPart = "";
            switch (flag)
            {
                case "start":
                    jsonPart = "[";
                    break;
                case "body_start":
                    jsonPart = "{\"value\": \"" + value + "\",\"label\": \"" + label + "\",\"children\":[";
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