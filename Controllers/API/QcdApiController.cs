using GetUserAvatar.Models;
using GitLabManager.DataContext;
using System.IO;
using GitlabManager.Models;
using GitLabManager.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using GitLabManager.BLL;
using System.Configuration;
using System.Text;
using GitlabManager.App_Start;

namespace GitLabManager.Controllers.API
{
    [ApiAuthorize]
    public class QcdApiController : ApiController
    {
        //public static ApplicationDbContext db = new ApplicationDbContext();
        //public static AgoraDbContext db_agora = new AgoraDbContext();
        public static SmtpClientBLL smtp = new SmtpClientBLL();

        private List<Project> ProjectsByID(string id)
        {
            try
            {
                List<Project> list = new List<Project>();
                string sql = "select temp.id,temp.name " +
                        " from" +
                        "(select " +
                        "p.id ,p.name " +
                        ",cast(namespace_id as VARCHAR) group_id,n.name group_name " +
                        ",string_agg(distinct concat_ws(':',u1.username,case when m1.access_level=50 then 'Owner' " +
                        "when m1.access_level=40 then 'M' " +
                        "when m1.access_level=30 then 'D' " +
                        "when m1.access_level=20 then 'R' " +
                        "when m1.access_level=10 then '' end ),',') AS project_member " +
                        ",string_agg(distinct concat_ws(':',u2.username,case when m2.access_level=50 then 'Owner' " +
                        "when m2.access_level=40 then 'M' " +
                        "when m2.access_level=30 then 'D' " +
                        "when m2.access_level=20 then 'R' " +
                        "when m2.access_level=10 then 'G' end),',') AS group_member " +
                        "From " +
                        "public.projects as p " +
                        "inner join public.namespaces as n on p.namespace_id=n.id " +
                        "left join public.members as m1 on p.id=m1.source_id and m1.source_type='Project' " +
                        "left join public.members as m2 on n.id=m2.source_id and m2.source_type='Namespace' " +
                        "left join public.users as u1 on m1.user_id=u1.id " +
                        "left join public.users as u2 on m2.user_id=u2.id " +
                        "group by p.id,p.name, p.namespace_id,n.name " +
                        ") temp " +
                        "where temp.id in (" + id + ") ";
                list = DBCon.db.Database.SqlQuery<Project>(sql).ToList();
                return list;
            }
            catch
            {
                return new List<Project>();
            }
        }

        public static Page_Warehouses GetWarehouses(string pj_name, string group_name, string pageNum, string pageSize, List<Project> projects)
        {
            string gitlabUrl = ConfigurationManager.AppSettings["gitlab_url"];
            string persionFace = ConfigurationManager.AppSettings["persion_face"].Replace("match(gitlab_url)", gitlabUrl);
            string defaultFace = ConfigurationManager.AppSettings["default_face"].Replace("match(gitlab_url)", gitlabUrl);

            String sql = "select " +
                        "cast(p.id as VARCHAR) as id,p.name as pj_name " +
                        ",cast(p.creator_id as VARCHAR) as creator_id " +
                        ",u3.name as creator_name " +
                        ",to_char(p.created_at at time zone '+8:00','yyyy-MM-dd hh24:mi:ss') created_at " +
                        ",to_char(p.updated_at at time zone '+8:00','yyyy-MM-dd hh24:mi:ss') updated_at " +
                        ",to_char(p.last_activity_at at time zone '+8:00','yyyy-MM-dd hh24:mi:ss') last_activity_at " +
                        ",p.description as description" +
                        ",p.archived as archived" +
                        ",cast(namespace_id as VARCHAR) group_id,n.name group_name " +
                        ", replace('{' || string_agg(" +
                        "     distinct concat_ws(" +
                        "         ':'''," +
                        "         '''id''', u1.id || ''''," +
                        "         ',''name''', u1.name || ''''," +
                        "         ',''access_level''', case when m1.access_level=50 then 'Owner' when m1.access_level=40 then 'M' when m1.access_level=30 then 'D' when m1.access_level=20 then 'R' when m1.access_level=10 then 'G' end || ''''," +
                        "         ',''avatar''', '"+ persionFace + "' || u1.id || '/' || u1.avatar || ''''" +
                        "     ), '},{'" +
                        " ) || '}', ':'',', ',')AS project_member" +

                        ", replace('{' || string_agg(" +
                        "     distinct concat_ws(" +
                        "         ':'''," +
                        "         '''id''', u2.id || ''''," +
                        "         ',''name''', u2.name || ''''," +
                        "         ',''access_level''', case when m2.access_level=50 then 'Owner' when m2.access_level=40 then 'M' when m2.access_level=30 then 'D' when m2.access_level=20 then 'R' when m2.access_level=10 then 'G' end || ''''," +
                        "         ',''avatar''', '" + persionFace + "' || u2.id || '/' || u2.avatar || ''''" +
                        "     ), '},{'" +
                        " ) || '}', ':'',', ',')AS group_member" +

                        ",cast(t.sync_time as VARCHAR) AS sync_time " +
                        "From " +
                        "public.projects as p " +
                        "inner join public.namespaces as n on p.namespace_id=n.id " +
                        "left join public.members as m1 on p.id=m1.source_id and m1.source_type='Project' and m1.user_id is not null " +
                        "left join public.members as m2 on n.id=m2.source_id and m2.source_type='Namespace' " +
                        "left join public.users as u1 on m1.user_id=u1.id " +
                        "left join public.users as u2 on m2.user_id=u2.id " +
                        "left join public.users as u3 " +
                        "on p.creator_id=u3.id " +
                        "left join (select project_id,max(sync_time)sync_time From sync_info group by project_id) as t " +
                        "on p.id=t.project_id " +
                        " where 1=1";

            String sqlEnd = "group by p.id,p.name,p.description,p.creator_id,u3.name,p.created_at,p.updated_at, " +
                       "namespace_id,n.name,sync_time " +
                       "order by p.updated_at desc ";
            int ps = 50;
            int pn = 1;
            if (!string.IsNullOrEmpty(pageSize))
            {
                ps = Convert.ToInt32(pageSize);
            }
            if (!string.IsNullOrEmpty(pageNum))
            {
                pn = Convert.ToInt32(pageNum);
            }
            string sqlPage = "limit " + ps + " offset " + (pn - 1) * ps;
            //sqlEnd = sqlEnd + sqlPage;
            if (pn == -1)
            {
                sqlPage = "";
            }
            List<Warehouse> list;
            String msql = "";
            int dataCnt = 0;
            if (projects != null && projects.Count > 0)
            {
                string pjstr = " and p.id in ( ";
                foreach (Project pj in projects)
                {
                    pjstr += pj.id + ",";
                }
                pjstr = pjstr.Substring(0, pjstr.Length - 1) + ")";
                sql = sql + pjstr;
            }
            if (string.IsNullOrEmpty(pj_name) && string.IsNullOrEmpty(group_name))
            {
                dataCnt = DBCon.db.Database.SqlQuery<Warehouse>(sql + sqlEnd).Count();
                //int ppp = db.Database.SqlQuery<Warehouse>(sql + sqlEnd).Count();
                list = DBCon.db.Database.SqlQuery<Warehouse>(sql + sqlEnd + sqlPage).ToList();
            }
            else if (string.IsNullOrEmpty(pj_name) && !string.IsNullOrEmpty(group_name))
            {
                msql = sql + " and  n.name ilike '%" + group_name + "%' ";
                dataCnt = DBCon.db.Database.SqlQuery<Warehouse>(msql + sqlEnd).Count();
                list = DBCon.db.Database.SqlQuery<Warehouse>(msql + sqlEnd + sqlPage).ToList();
            }
            else if (!string.IsNullOrEmpty(pj_name) && string.IsNullOrEmpty(group_name))
            {
                msql = sql + " and  p.name ilike '%" + pj_name + "%' ";
                dataCnt = DBCon.db.Database.SqlQuery<Warehouse>(msql + sqlEnd).Count();
                list = DBCon.db.Database.SqlQuery<Warehouse>(msql + sqlEnd + sqlPage).ToList();
            }
            else
            {
                msql = sql + " and  n.name ilike '%" + group_name + "%' or p.name ilike '%" + pj_name + "%' ";
                dataCnt = DBCon.db.Database.SqlQuery<Warehouse>(msql + sqlEnd).Count();
                list = DBCon.db.Database.SqlQuery<Warehouse>(msql + sqlEnd + sqlPage).ToList();
            }

            foreach (Warehouse li in list)
            {
                //空对象处理
                string nullString = "{'id','name','access_level','avatar'}";
                if (nullString.Equals(li.project_member))
                {
                    li.project_member = "";
                }
                //空头像处理
                string nullAvatar = ",'avatar'}";
                if (li.group_member.Contains(nullAvatar))
                {
                    string stra = li.group_member;
                    li.group_member = stra.Replace(nullAvatar, ",'avatar':'" + defaultFace + "'}");
                }
                if (li.project_member.Contains(nullAvatar))
                {
                    string stra = li.project_member;
                    li.project_member = stra.Replace(nullAvatar, ",'avatar':'" + defaultFace + "'}");
                }
            }
            Page_Warehouses page_ = new Page_Warehouses();
            page_.Warehouses = list;
            page_.rowCount = dataCnt;
            page_.pageSize = Convert.ToInt32(ps);
            page_.pageNum = Convert.ToInt32(pn);
            //page_.pageNumAll = (int)Math.Ceiling((double)dataCnt / page_.pageSize);
            return page_;
        }

        /// <summary>
        ///  成员头像地址取得
        /// </summary>
        /// <returns></returns>
        public List<memberinfo> GetMemberUrl()
        {
            var allMembers = new List<memberinfo>();
            string api = ConfigurationManager.AppSettings["gitlab_instance"];
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token5"]);

            var response = httpClient.GetAsync(api + "users?per_page=100").Result;
            int TotalPages = int.Parse(response.Headers.GetValues("X-Total-Pages").First());
            int Page = int.Parse(response.Headers.GetValues("X-Page").First());
            while (Page <= TotalPages)
            {
                response = httpClient.GetAsync(api + "users?per_page=100&page=" + Page.ToString()).Result;
                if (Page < TotalPages)
                {
                    Page = int.Parse(response.Headers.GetValues("X-Next-Page").First());
                }
                else
                {
                    Page = TotalPages + 1;
                }
                var result = response.Content.ReadAsStringAsync().Result;
                var list = JsonConvert.DeserializeObject<List<memberinfo>>(result);
                foreach (var m in list)
                {
                    allMembers.Add(m);
                }
            }

            return allMembers;
        }

        public List<MemberInfo> MemberConvert(List<MemberInfo> member, List<memberinfo> user)
        {
            string gitlabUrl = ConfigurationManager.AppSettings["gitlab_url"];
            string defaultFace = ConfigurationManager.AppSettings["default_face"].Replace("match(gitlab_url)", gitlabUrl);

            foreach (var m in member)
            {
                var url = user.Where(u => u.username == m.MemberID).ToList();
                if (url == null || url.Count == 0 || url[0].avatar_url == null || url[0].avatar_url == "")
                {
                    // 成员头像不存在的情况下，用指定图片地址代替。
                    m.avatar = defaultFace;
                }
                else
                {
                    m.avatar = url[0].avatar_url;
                }
            }
            return member;
        }

        public string GetWareHouseCount(string data)
        {
            if (data == null)
            {
                return "0";
            }
            else
            {
                List<Projects> v = JsonConvert.DeserializeObject<List<Projects>>(data);
                return v != null ? v.Count.ToString() : "0";
            }
        }

        public class memberinfo
        {
            public string username { get; set; }
            public string avatar_url { get; set; }
        }

        public static void QCDProjectSync()
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
                List<Agreements> agreList = DBCon.db_agora.Agreements.ToList();

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

                            DBCon.db_agora.Agreements.Add(_agre);
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
                                DBCon.db_agora.Entry(_agre).State = EntityState.Modified;
                            }
                        }
                    }

                    // 删除项目
                    foreach (var a in agreList)
                    {
                        var delItem = existList.Where(i => i.agreement_cd == a.agreement_cd).ToList();
                        if (delItem == null || delItem.Count == 0)
                        {
                            DBCon.db_agora.Entry(a).State = EntityState.Deleted;
                        }
                    }

                    dbstate = DBCon.db_agora.SaveChanges();
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

        private bool memberCheck(string data, string userID)
        {
            if (data == null || userID == null) return false;
            var members = JsonConvert.DeserializeObject<List<MemberInfo>>(data);
            var result = members.Where(i => i.MemberID == userID);
            if (result != null && result.Count() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [HttpGet]
        public IHttpActionResult QCDProjectShow()
        {
            try
            {
                // 数据种类 0：所有项目，1：进行中的，2：已结束的 3：我参与的（見積中，見積提出済，受注済）4:标星项目
                string type = HttpContext.Current.Request.QueryString["type"];

                // 用户ID
                string userId = HttpContext.Current.Request.QueryString["userId"];

                // 页面显示条数
                string pageSize = HttpContext.Current.Request.QueryString["pageSize"];
                // 当前页面号码
                string pageNum = HttpContext.Current.Request.QueryString["pageNum"];

                // 搜索项目信息（CD/名称）
                string projectInfo = HttpContext.Current.Request.QueryString["projectInfo"];

                if (pageSize == null || pageSize == String.Empty)
                {
                    pageSize = "20";
                }

                if (pageNum == null || pageNum == String.Empty)
                {
                    pageNum = "1";
                }

                // 标星项目检索
                var starList = DBCon.db_agora.UsersStarAgreements.Where(i => i.user_id == userId).ToList();

                var agreList = new List<Agreements>();

                //数据库中的数据取得
                if (type == "0")
                {
                    // 所有项目
                    agreList = DBCon.db_agora.Agreements.ToList();
                }
                else if (type == "1")
                {
                    // 进行中的项目
                    agreList = DBCon.db_agora.Agreements.Where(i => i.status == 1 || i.status == 2 || i.status == 3).ToList();
                }
                else if (type == "2")
                {
                    //结束和终止的项目
                    agreList = DBCon.db_agora.Agreements.Where(i => i.status == 4 || i.status == 5).ToList();
                }
                else if (type == "3")
                {
                    // 我参与的
                    var all = DBCon.db_agora.Agreements.ToList();
                    foreach (var a in all)
                    {
                        if ((a.status == 1 || a.status == 2 || a.status == 3)
                            && (a.manager_id == userId || memberCheck(a.member_ids, userId)))
                        {
                            agreList.Add(a);
                        }
                    }
                }
                else
                {
                    // 标星项目
                    // var all = db_agora.Agreements.ToList();
                    foreach (var s in starList)
                    {
                        var agreById = DBCon.db_agora.Agreements.Where(i => i.agreement_cd == s.agreement_cd).FirstOrDefault();
                        if (agreById != null)
                        {
                            agreList.Add(agreById);
                        }
                    }
                }

                if (projectInfo != null && projectInfo != String.Empty)
                {
                    // 课题CD模糊匹配
                    agreList = agreList.Where(i => i.agreement_cd.IndexOf(projectInfo) >= 0 || i.agreement_name.ToLower().IndexOf(projectInfo.ToLower()) >= 0).ToList();
                }

                agreList = agreList.OrderByDescending(i => i.updated_at).ToList();

                int dataCount = agreList.Count;
                //分页表示
                agreList = agreList.Skip((Convert.ToInt32(pageNum) - 1) * Convert.ToInt32(pageSize)).Take(Convert.ToInt32(pageSize)).ToList();
                var agreListReturn = new List<AgreementsWithStar>();

                foreach (var a in agreList)
                {
                    var addStar = new AgreementsWithStar();
                    // 没有设定成员的处理
                    if (a.member_ids == null) { a.member_ids = "[]"; }
                    addStar.agreement = a;

                    //标星项目处理
                    var star = starList.Where(i => i.agreement_cd == a.agreement_cd).FirstOrDefault();
                    addStar.IsStar = (star != null);

                    agreListReturn.Add(addStar);
                }

                QcdProjectShow pj = new QcdProjectShow
                {
                    qcdProject = agreListReturn,
                    pageSize = Convert.ToInt32(pageSize),
                    pageNum = Convert.ToInt32(pageNum),
                    pageNumAll = dataCount
                };
                return Json(pj);
            }
            catch(Exception ex)
            {
                return null;
            }
        }
        [HttpGet]
        public IHttpActionResult QCDProjectCount()
        {
            try
            {
                // 用户ID
                string userId = HttpContext.Current.Request.QueryString["userId"];

                List<Agreements> agreList = new List<Agreements>();
                //全部的课题数量
                var all = DBCon.db_agora.Agreements.ToList();
                int allCount = all.Count;

                //进行中的课题数量
                int doingCount = DBCon.db_agora.Agreements.Where(i => i.status == 1 || i.status == 2 || i.status == 3).ToList().Count;

                //结束和终止的课题数量
                int endCount = DBCon.db_agora.Agreements.Where(i => i.status == 4 || i.status == 5).ToList().Count;

                // 我参与的
                foreach (var a in all)
                {
                    if ((a.status == 1 || a.status == 2 || a.status == 3)
                        && (a.manager_id == userId || memberCheck(a.member_ids, userId)))
                    {
                        agreList.Add(a);
                    }
                }
                int myCount = agreList.Count;

                //标星项目
                var starCount = 0;
                var starList = DBCon.db_agora.UsersStarAgreements.Where(i => i.user_id == userId).ToList();

                foreach (var s in starList)
                {
                    var agreById = all.Where(i => i.agreement_cd == s.agreement_cd).FirstOrDefault();
                    if (agreById != null)
                    {
                        starCount += 1;
                    }
                }

                return Json(new { allCount = allCount, doingCount = doingCount, endCount = endCount, myCount = myCount, starCount = starCount });
            }
            catch
            {
                return null;
            }
        }

        [HttpGet]
        public IHttpActionResult GitlabProjectInfo()
        {
            try
            {
                // 用户ID
                string userId = HttpContext.Current.Request.QueryString["userId"];

                // 项目名称
                string name = HttpContext.Current.Request.QueryString["name"];

                List<Projects> pjInfo = DBCon.db.Projects.ToList();

                if (name != null && name != String.Empty)
                {
                    //模糊匹配
                    pjInfo = pjInfo.Where(i => i.name.IndexOf(name) >= 0).ToList();
                }

                // 课题的命名空间取得
                var nameSpaces = DBCon.db.NameSpaces.ToList();
                var retList = new List<ReturnProjectView>();

                foreach (var p in pjInfo)
                {
                    string spaceName = nameSpaces.Where(i => i.id.ToString() == p.namespace_id).First().name;
                    var pv = new ReturnProjectView
                    {
                        id = p.id,
                        name = spaceName + " / " + p.name,
                        description = p.description,
                        spacename = spaceName
                    };
                    retList.Add(pv);
                }

                return Json(retList);
            }
            catch
            {
                return null;
            }
        }

        [HttpPost]
        public IHttpActionResult QCDProjectSetting(QCDProjectSettingsReq req)
        {
            try
            {
                // 根据id检索出既存信息
                Agreements _agre = DBCon.db_agora.Agreements.Where(i => i.agreement_cd == req.id).FirstOrDefault();

                //对象数据序列化
                string repositoryIds = JsonConvert.SerializeObject(req.gitlabProject);

                // 设定变更内容
                _agre.repository_ids = req.gitlabProject.Count == 0 ? null : repositoryIds;
                _agre.project_count = req.count;
                _agre.updated_by = req.userId;
                _agre.updated_at = DateTime.Now;

                // 标记数据更新状态
                DBCon.db_agora.Entry(_agre).State = EntityState.Modified;

                // 保存数据变更
                int dbstate = DBCon.db_agora.SaveChanges();

                return Json(new { Success = dbstate > 0, state = dbstate });
            }
            catch
            {
                return null;
            }
        }

        [HttpPost]
        public IHttpActionResult QCDProjectStarSetting(UserStarReq req)
        {
            try
            {
                // 根据id检索出既存信息
                var _star = DBCon.db_agora.UsersStarAgreements.Where(i => i.user_id == req.userId && i.agreement_cd == req.agreement_cd).FirstOrDefault();

                if (req.flag == false && _star != null) // 取消标星
                {
                    DBCon.db_agora.Entry(_star).State = EntityState.Deleted;
                }
                else if (_star == null && req.flag == true)
                {
                    var _all = DBCon.db_agora.UsersStarAgreements.ToList();
                    int maxId = _all.Count > 0 ? _all.Max(i => i.id) : 0;

                    _star = new UsersStarAgreements();
                    _star.id = ++maxId;
                    _star.user_id = req.userId;
                    _star.agreement_cd = req.agreement_cd;
                    _star.created_at = DateTime.Now;
                    _star.updated_at = DateTime.Now;
                    DBCon.db_agora.Entry(_star).State = EntityState.Added;
                }

                // 保存数据变更
                int dbstate = DBCon.db_agora.SaveChanges();
                return Json(new { Success = dbstate > 0, state = dbstate });
            }
            catch
            {
                return null;
            }
        }

        [HttpGet]
        public IHttpActionResult QCDProjectInfoDetail()
        {
            try
            {
                // 用户ID
                string userId = HttpContext.Current.Request.QueryString["userId"];
                string id = HttpContext.Current.Request.QueryString["id"];

                // 根据id检索出既存信息
                Agreements _agre = DBCon.db_agora.Agreements.Where(i => i.agreement_cd == id).FirstOrDefault();

                if (_agre != null && _agre.repository_ids != null)
                {
                    // json数据反序列化
                    List<Projects> pjList = JsonConvert.DeserializeObject<List<Projects>>(_agre.repository_ids);
                    var retList = new List<ReturnProjectView>();

                    foreach (var p in pjList)
                    {
                        Projects pj = DBCon.db.Projects.Where(i => i.id == p.id).FirstOrDefault();
                        string spaceName = DBCon.db.NameSpaces.Where(i => i.id.ToString() == pj.namespace_id).First().name;
                        var pv = new ReturnProjectView
                        {
                            id = p.id,
                            name = spaceName + " / " + pj.name,
                            description = p.description,
                            spacename = spaceName
                        };
                        retList.Add(pv);
                    }

                    return Json(retList);
                }
                else
                {
                    return Json(new ReturnProjectView { });
                }
            }
            catch
            {
                return null;
            }
        }

        [HttpGet]
        public IHttpActionResult QCDProjectDetail()
        {
            try
            {
                // 没有数据的情况下，空结果返回
                Page_Warehouses page_ = new Page_Warehouses
                {
                    Warehouses = null,
                    rowCount = 0,
                    pageSize = 20,
                    pageNum = 1
                };

                // 用户ID
                string userId = HttpContext.Current.Request.QueryString["userId"];
                string id = HttpContext.Current.Request.QueryString["id"];
                string pj_name = HttpContext.Current.Request.QueryString["pj_name"];
                string group_name = HttpContext.Current.Request.QueryString["group_name"];

                string pagesize = HttpContext.Current.Request.QueryString["pageSize"];
                string pageNum = HttpContext.Current.Request.QueryString["pageNum"];

                // 根据id检索出既存信息
                Agreements _agre = DBCon.db_agora.Agreements.Where(i => i.agreement_cd == id).FirstOrDefault();

                if (_agre != null && _agre.repository_ids != null)
                {
                    // json数据反序列化
                    List<Projects> pjList = JsonConvert.DeserializeObject<List<Projects>>(_agre.repository_ids);

                    string pjID = "";
                    for (int i = 0; i < pjList.Count; i++)
                    {
                        if (i == pjList.Count - 1)
                        {
                            pjID = pjID + pjList[i].id;
                        }
                        else
                        {
                            pjID = pjID + pjList[i].id + ",";
                        }
                    }

                    List<Project> projects = ProjectsByID(pjID);
                    if (projects == null || projects.Count == 0)
                    {
                        return Json(page_);
                    }
                    else
                    {
                        return Json(GetWarehouses(pj_name, group_name, pageNum, pagesize, projects));
                    }
                }
                else
                {
                    return Json(page_);
                }
            }
            catch
            {
                return null;
            }
        }

        private void ProjectPermissionSet(QCDCodeReviewReq req)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);

            int codereviewer = DBCon.db.Users.Where(i => i.username.Equals("codereviewer")).FirstOrDefault().id;  // 审查成员id
            string end_date = DateTime.Parse(req.expecteDate).AddDays(3).ToString("yyyy-MM-dd");            // 最晚完成日期（比预定日期晚三天）

            // 权限设定
            for (int i = 0; i < req.reviewItem.Count; i++)
            {
                HttpContent httpContent = new FormUrlEncodedContent(
                new List<KeyValuePair<string, string>> {
                                            new KeyValuePair<string, string>("user_id", codereviewer.ToString()),
                                            new KeyValuePair<string, string>("expires_at", end_date),
                                            new KeyValuePair<string, string>("access_level", "40")//Maintainer
                });
                var response = httpClient.PostAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + req.reviewItem[i].projectId + "/members", httpContent).Result;
                var resultAuthority = response.Content.ReadAsStringAsync().Result;
            }
        }

        [HttpGet]
        public IHttpActionResult GetUserEmail()
        {
            string userId = HttpContext.Current.Request.QueryString["userId"];
            string id = HttpContext.Current.Request.QueryString["id"];

            var agreList = DBCon.db_agora.Agreements.Where(i => i.agreement_cd == id).FirstOrDefault();
            var memlist = JsonConvert.DeserializeObject<List<MemberInfo>>(agreList.member_ids);

            var retUsers = new List<ReturnUser>();
            var defaultUser = new List<string>();

            try
            {
                // GitLab 用户列表取得
                var users = from u in DBCon.db.Users 
                         join i in DBCon.db.Identities on u.id equals i.user_id
                         where u.state == "active" && i.provider == "ldapmain"
                         select u;

                // 数据加工
                foreach (var user in users)
                {
                    var u = new ReturnUser
                    {
                        id = user.id,
                        username = user.username,
                        emailShow = user.name + "(" + user.username + ")",
                        email = user.email
                    };
                    retUsers.Add(u);
                }

                // 仓库成员取得（除去非GitLab用户）
                foreach (var mem in memlist)
                {
                    var chk = retUsers.Where(i => i.username == mem.MemberID).ToList();
                    if (chk != null && chk.Count > 0)
                    {
                        defaultUser.Add(mem.MemberID);
                    }
                }
            }
            catch(Exception ex)
            {
                return Json(new { User = "[]", List = "[]" ,error = ex.Message});
            }

            return Json(new { User = defaultUser, List = retUsers });
        }

        [HttpPost]
        public IHttpActionResult QCDCodeReview(QCDCodeReviewReq req)
        {
            string releaseFlag = ConfigurationManager.AppSettings["msg_send"];
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["gitlab_token1"]);

            StringBuilder sb = new StringBuilder();
            User user = DBCon.db.Users.Where(i => i.username.Equals(req.userId)).FirstOrDefault();
            
            // 项目成员邮件取得(CC人员)
            string memberEmails = (req.ccMail == null || req.ccMail == "")?"" : req.ccMail;

            // 邮件标题
            string title = "【" + req.id + ":" + req.name + " 】项目核心代码审查申请";

            //邮件正文（头部）
            sb.AppendLine("技术委员会负责人，您好：" + "<br/>");
            sb.AppendLine("对项目【" + req.id + ":" + req.name + " 】申请技术委员会支持，对核心代码进行审查。" + "<br/>");
            sb.AppendLine("请技术委员会协调相关人员做一次核心代码审查。" + "<br/>");
            sb.AppendLine("<br/>");
            sb.AppendLine("申请者：" + user.name + "<br/>");
            sb.AppendLine("仓库相关申请信息如下：" + "<br/>");
            sb.AppendLine("<br/>");

            //邮件正文（主要内容：代码相关信息）
            for (int i = 0; i < req.reviewItem.Count; i++)
            {
                //代码地址取得
                var response = httpClient.GetAsync(ConfigurationManager.AppSettings["gitlab_instance"] + "projects/" + req.reviewItem[i].projectId).Result;
                var result = response.Content.ReadAsStringAsync().Result;
                SingleProject project = JsonConvert.DeserializeObject<SingleProject>(result);

                sb.AppendLine("地址" + (i + 1).ToString() + "：<a href=" + project.web_url + ">" + project.web_url + "</a><br/>");
                sb.AppendLine("分支：" + req.reviewItem[i].branchName + "(<a href = " + req.reviewItem[i].branchUrl + ">" + req.reviewItem[i].branchUrl + "</a>)<br/>");
                sb.AppendLine("语言：" + req.reviewItem[i].language + "<br/>");
                sb.AppendLine("数据库：" + req.reviewItem[i].dataBase + "<br/>");
                sb.AppendLine("<br/>");
            }

            //邮件正文（评审相关信息）
            sb.AppendLine("评审信息：" + req.reviewInfo + "<br/>");
            sb.AppendLine("期望完成日期：" + req.expecteDate + "<br/>");
            if (req.comment != null && req.comment != "")
            {
                sb.AppendLine("备注：" + req.comment + "<br/>");
            }

            //邮件结尾
            sb.AppendLine("<br/>");
            sb.AppendLine("有疑问或者相关确认事项，请联系申请者。");

            try
            {
                // 收件人(多人用“,”分开)
                string strTo = "technicalcommittee@cn.tre-inc.com";
                // 抄送人(多人用“,”分开)
                string strCc = "";

                if (releaseFlag == "true")
                {
                    if (memberEmails != "")
                    {
                        strCc = memberEmails + "," + "qualityassurance@cn.tre-inc.com";
                    }
                    else
                    {
                        strCc = "qualityassurance@cn.tre-inc.com";
                    }
                }
                else
                {
                    strTo = "2200714gao_changshui@cn.tre-inc.com"; //测试用，暂时保留
                    strCc = memberEmails;
                }

                //邮件发送
               var result = smtp.SendMail(user.email, strTo, strCc, title, sb.ToString());
                if (result == true)
                {
                    // 审查者权限设定
                    ProjectPermissionSet(req);
                    return Json(new { Success = true, Message = "处理成功！" });
                }
                else
                {
                    return Json(new { Success = false, Message = "处理失败！" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = ex.Message });
            }
        }

        public class ReturnProjectView
        {
            public int id { get; set; }
            public string name { get; set; }
            public string description { get; set; }
            public string spacename { get; set ;}
        }

        [HttpPost]
        public IHttpActionResult QCDProjectIsUseGitlab(IsUseGitlabReq req)
        {
            try
            {
                var _agre = DBCon.db_agora.Agreements.Where(i => i.agreement_cd == req.agreement_cd).FirstOrDefault();

                _agre.repo_flg = req.is_use;
                _agre.updated_by = req.user_id;
                _agre.updated_at = DateTime.Now;

                // 标记数据更新状态
                DBCon.db_agora.Entry(_agre).State = EntityState.Modified;

                // 保存数据变更
                int dbstate = DBCon.db_agora.SaveChanges();
                return Json(new {success = true});
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        public string StatusName(int StatusCode)
        {
            string StatusName = "";
            switch (StatusCode)
            {
                case 1:
                    StatusName = "1)見積中";
                    break;
                case 2:
                    StatusName = "2)見積提出済";
                    break;
                case 3:
                    StatusName = "3)受注済";
                    break;
                case 4:
                    StatusName = "4)課題完了";
                    break;
                case 5:
                    StatusName = "5)課題中止";
                    break;
                default:
                    break;
            }
            return StatusName;
        }

        private class ReturnUser
        {
            public int id { get; set; }
            public string emailShow { get; set; }
            public string email { get; set; }
            public string username { get; set; }
        }
    }
}
