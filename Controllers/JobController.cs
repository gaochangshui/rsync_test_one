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
                if (dt.Minute % 20 == 0) //每20分钟 同期一次
                {
                    QCDProjectSync();
                }

                // 昨日代码未推送人员钉钉通知
                if (dt.Hour == 10 && dt.Minute == 0) // 每天10点执行
                {
                    var wac = new WarehouseApiController();
                    wac.SendDingDingMsg();
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
                var syncList = httpClient.GetAsync("http://qcd.trechina.cn/qcdapi/Agreements").Result;

                var result = syncList.Content.ReadAsStringAsync().Result;

                //API的项目信息取得
                var pjList = JsonConvert.DeserializeObject<AgreementInfo>(result);

                //数据库中的数据取得
                List<Agreements> agreList = db_agora.Agreements.ToList();

                QcdApiController qcdApi = new QcdApiController();
                var userUrl = qcdApi.GetMemberUrl();

                int dbstate = 0;
                var existList = new List<Agreements>();

                if (agreList != null)
                {
                    //数据库中的最大ID取得（新规数据的情况下使用）
                    int maxId = agreList.Count > 0 ? agreList.Max(i => i.id) : 0;

                    for (int i = 0; i < pjList.projectInfos.Count; i++)
                    {
                        Agreements _agre = agreList.Where(cd => cd.agreement_cd == pjList.projectInfos[i].ProjectCD.ToString()).FirstOrDefault();

                        int updateStatus = 0; // 初期状态

                        if (_agre == null)
                        {
                            updateStatus = 1; // 新规数据
                            _agre = new Agreements();
                            _agre.id = ++maxId;
                        }

                        var member = pjList.memberInfos.Where(m => m.ProjectCD == pjList.projectInfos[i].ProjectCD).ToList();
                        member = qcdApi.MemberConvert(member, userUrl);
                        _agre.updated_at = DateTime.Now;

                        if (updateStatus == 1)
                        {
                            _agre.agreement_cd = pjList.projectInfos[i].ProjectCD.ToString();
                            _agre.agreement_name = pjList.projectInfos[i].ProjectName;
                            _agre.status = pjList.projectInfos[i].status;
                            _agre.plan_mandays = pjList.projectInfos[i].Manday;
                            _agre.plan_begin_date = pjList.projectInfos[i].BeginDate;
                            _agre.plan_end_date = pjList.projectInfos[i].EndDate;
                            _agre.manager_id = pjList.projectInfos[i].LeaderCD;
                            _agre.manager_name = pjList.projectInfos[i].LeaderName;
                            _agre.member_ids = JsonConvert.SerializeObject(member);

                            db_agora.Agreements.Add(_agre);
                        }
                        else
                        {
                            //保存同期前存在的项目
                            existList.Add(_agre);

                            if (_agre.agreement_name != pjList.projectInfos[i].ProjectName)
                            {
                                _agre.agreement_name = pjList.projectInfos[i].ProjectName;
                                updateStatus = 2; // 数据变更
                            }

                            if (_agre.status.ToString() != pjList.projectInfos[i].status.ToString())
                            {
                                _agre.status = pjList.projectInfos[i].status;
                                updateStatus = 3; // 数据变更
                            }

                            if (_agre.plan_mandays != pjList.projectInfos[i].Manday)
                            {
                                _agre.plan_mandays = pjList.projectInfos[i].Manday;
                                updateStatus = 4; // 数据变更
                            }

                            if (_agre.plan_begin_date != pjList.projectInfos[i].BeginDate)
                            {
                                _agre.plan_begin_date = pjList.projectInfos[i].BeginDate;
                                updateStatus = 5; // 数据变更
                            }

                            if (_agre.plan_end_date != pjList.projectInfos[i].EndDate)
                            {
                                _agre.plan_end_date = pjList.projectInfos[i].EndDate;
                                updateStatus = 6; // 数据变更
                            }

                            if (_agre.manager_id != pjList.projectInfos[i].LeaderCD)
                            {
                                _agre.manager_id = pjList.projectInfos[i].LeaderCD;
                                updateStatus = 7; // 数据变更
                            }

                            if (_agre.manager_name != pjList.projectInfos[i].LeaderName)
                            {
                                _agre.manager_name = pjList.projectInfos[i].LeaderName;
                                updateStatus = 8; // 数据变更
                            }

                            if (_agre.member_ids != JsonConvert.SerializeObject(member))
                            {
                                _agre.member_ids = JsonConvert.SerializeObject(member);
                                updateStatus = 9; // 数据变更
                            }

                            // 仓库数量计算
                            _agre.project_count = qcdApi.GetWareHouseCount(_agre.repository_ids);

                            if (updateStatus != 0)
                            {
                                db_agora.Entry(_agre).State = EntityState.Modified;
                            }
                        }
                    }

                    // 删除项目
                    foreach (var a in agreList)
                    {
                        var delItem = existList.Where(i => i.agreement_cd == a.agreement_cd).ToList();
                        if (delItem == null || delItem.Count == 0)
                        {
                            db_agora.Entry(a).State = EntityState.Deleted;
                        }
                    }

                    dbstate = db_agora.SaveChanges();
                    string logTxt = "同期件数：" + pjList.projectInfos.Count + ",同期时间:" + DateTime.Now.ToString();
                    sws.WriteLine(logTxt);
                }
            }
            catch (Exception ex)
            {
                string errTxt = "同期失败，失败信息：" + ex.Message + ",失败时间：" + DateTime.Now.ToString();
                sws.WriteLine(errTxt);
            }
            finally
            {
                sws.Close();
            }
        }
     }
}