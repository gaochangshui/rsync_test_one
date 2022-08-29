using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GitLabManager.Models
{
    [Table("project_sync_settings", Schema = "public")]

    public class ProjectSyncSettings
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int id { get; set; }
        public int project_id { get; set; }
        public string sync_branches { get; set; }
        public string remote_url { get; set; }
        public string remote_user { get; set; }
        public string remote_token { get; set; }
        public DateTime last_sync_at { get; set; }
        public bool last_sync_succeeded { get; set; }
        public string last_sync_error { get; set; }
        public DateTime last_successful_sync_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    [Table("agreements", Schema = "public")]
    public class Agreements
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int id { get; set; }
        public string agreement_cd { get; set; }
        public string agreement_name { get; set; }
        public int status { get; set; }
        public string plan_mandays { get; set; }
        public string plan_begin_date { get; set; }
        public string plan_end_date { get; set; }
        public string repository_ids { get; set; }
        public string member_ids { get; set; }
        public DateTime updated_at { get; set; }
        public string updated_by { get; set; }
        public string manager_id { get; set; }
        public string manager_name { get; set; }
        public string project_count { get; set; }
    }

    public class QcdProjectShow
    {
        public int pageSize { get; set; }
        public int pageNum { get; set; }
        public int pageNumAll { get; set; }
        public List<Agreements> qcdProject { get; set; }
    }

    public class QCDProjectSettingsReq
    {
        public string userId { get; set; }
        public string id { get; set; }
        public string count { get; set; }
        public List<Projects> gitlabProject { get; set; }
    }

    [Table("projects", Schema = "public")]
    public class Projects
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
    }

    public class QCDCodeReviewReq
    {
        public string id { get; set; }
        public string userId { get; set; }
        public string name { get; set; }
        public string reviewInfo { get; set; }
        public string expecteDate{ get; set; }
        public string comment { get; set; }

        public List<CodeReviewItem> reviewItem { get; set; }
    }

    public class CodeReviewItem
    {
        public string projectId { get; set; }
        public string branchName { get; set; }
        public string branchUrl { get; set; }
        public string language { get; set; }
        public string dataBase { get; set; }
    }

    public class AgreementInfo
    {
        public List<ProjectInfo> projectInfos { get; set; }
        public List<MemberInfo> memberInfos { get; set; }
    }
    public class ProjectInfo
    {
        public int ProjectCD { get; set; }
        public string ProjectName { get; set; }
        public int status { get; set; }
        public string StatusName { get; set; }
        public string LeaderCD { get; set; }
        public string LeaderName { get; set; }
        public string Manday { get; set; }
        public string BeginDate { get; set; }
        public string EndDate { get; set; }
    }

    public class MemberInfo
    {
        public int ProjectCD { get; set; }
        public string MemberID { get; set; }
        public string MemberName { get; set; }
        public string avatar { get; set; }
    }

}