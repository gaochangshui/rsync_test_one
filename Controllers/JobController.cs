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

namespace GitLabManager.Controllers
{
    public class JobController
    {
        public static AgoraDbContext db_agora = new AgoraDbContext();

        public static void Run()
        {
            Timer timer = new Timer(60 * 1000); // 1分钟
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Elapsed += (s, e) =>
            {
                DateTime dt = DateTime.Now;
                if (dt.Minute == 0 && dt.Minute < 10)
                {
                    QCDProjectSync();
                }
                else if (dt.Minute >= 10 && dt.Minute.ToString().Substring(1,1) == "0") // 每10分钟执行
                {
                     QCDProjectSync();
                }
                else
                {
                    
                }
            };
        }

        private static void QCDProjectSync()
        {
            string parentFolder = System.AppDomain.CurrentDomain.BaseDirectory
                    + "\\LOG";
            string logFile = parentFolder + "\\run_log.txt";
            StreamWriter sws = null;

            try
            {
                Directory.CreateDirectory(parentFolder);
                sws = new StreamWriter(logFile, true, System.Text.Encoding.UTF8);

                HttpClient httpClient = new HttpClient();
                var syncList = httpClient.GetAsync("http://qcd.trechina.cn/qcdapi/projects?filter=all-projects").Result;
                var result = syncList.Content.ReadAsStringAsync().Result;

                //API的项目信息取得
                List<QcdProjects> pjList = JsonConvert.DeserializeObject<List<QcdProjects>>(result);

                //数据库中的数据取得
                List<Agreements> agreList = db_agora.Agreements.ToList();

                int dbstate = 0;

                if (agreList != null)
                {
                    //数据库中的最大ID取得（新规数据的情况下使用）
                    int maxId = agreList.Count > 0 ? agreList.Max(i => i.id) : 0;

                    for (int i = 0; i < pjList.Count; i++)
                    {
                        Agreements _agre = agreList.Where(cd => cd.agreement_cd == pjList[i].ProjectCode.ToString()).FirstOrDefault();

                        int updateStatus = 0; // 初期状态

                        if (_agre == null)
                        {
                            updateStatus = 1; // 新规数据
                            _agre = new Agreements();
                            _agre.id = ++maxId;
                        }
                        else
                        {
                            if (_agre.agreement_name != pjList[i].ProjectName
                                || _agre.status != pjList[i].status)
                            {
                                updateStatus = 2; // 数据变更
                            }
                        }

                        if (updateStatus != 0)
                        {
                            _agre.agreement_cd = pjList[i].ProjectCode.ToString();
                            _agre.agreement_name = pjList[i].ProjectName;
                            _agre.status = pjList[i].status;
                            _agre.updated_at = DateTime.Now;
                            if (updateStatus == 1)
                            {
                                db_agora.Agreements.Add(_agre);
                            }
                            else
                            {
                                db_agora.Entry(_agre).State = EntityState.Modified;
                            }
                        }
                    }

                    dbstate = db_agora.SaveChanges();
                    string logTxt = "同期件数：" + pjList.Count + ",同期时间:" + DateTime.Now.ToString();
                    sws.WriteLine(logTxt);
                }
            }
            catch (Exception ex)
            {
                string errTxt = "同期失败，失败信息：" + ex.Message;
                sws.WriteLine(errTxt);
            }
            finally
            {
                sws.Close();
            }
        }
     }
}