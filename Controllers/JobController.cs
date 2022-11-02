using System;
using System.Timers;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using GitLabManager.Models;
using GitLabManager.DataContext;
using System.IO;
using GitLabManager.Controllers.API;
using System.Configuration;

namespace GitLabManager.Controllers
{
    public class JobController
    {
        public static AgoraDbContext db_agora = new AgoraDbContext();

        public static void Run()
        {
            //true:生产环境，false:测试环境
            string prodflg = ConfigurationManager.AppSettings["msg_send"];

            Timer timer = new Timer(60 * 1000); // 1分钟
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Elapsed += (s, e) =>
            {
                DateTime dt = DateTime.Now;

                // 项目契约信息同期
                if (dt.Minute % 20 == 0) //每20分钟 同期一次
                {
                    if (prodflg == "true")
                    {
                        QcdApiController.QCDProjectSync();
                    }
                }

                // 昨日代码未推送人员钉钉通知
                if (dt.Hour == 10 && dt.Minute == 30) // 每天10点执行
                {
                    if (prodflg == "true")
                    {
                        WarehouseApiController.SendDingDingMsg();
                    }
                }

                // 仓库成员未设定有效期限对应
                if (dt.Hour == 14 && dt.Minute == 0) // 每天14点执行
                {
                    if (prodflg == "true")
                    {
                        WarehouseApiController.SetExpiresDate();
                    }
                }

                // 代码提交履历数据做成
                if ((dt.Hour == 7 && dt.Minute == 15) ||(dt.Hour == 11 && dt.Minute == 30) || (dt.Hour == 18 && dt.Minute == 30))
                {
                    if (prodflg == "true")
                    {
                       GitlabCodeAnalysisController.GetDataRsync();
                    }
                }

                // 代码提交履历数据做成
                if (dt.Hour == 22 && dt.Minute == 01)
                {
                    if (prodflg == "true")
                    {
                        GitlabCodeAnalysisController.GetDataRsync2();
                    }
                }
            };
        }
     }
}