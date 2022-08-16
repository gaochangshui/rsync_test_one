using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GetUserAvatar.Models
{
    public class GitLabApi
    {
    }
    public class Project
    {
        public int id { get; set; }
        public string name { get; set; }
        public string web_url { get; set; }
        public List<shared_with_groups> shared_with_groups { get; set; }

    }
    public class ProtectedBranch
    {
        public int id { get; set; }
        public string name { get; set; }
        public bool allow_force_push { get; set; }
        public bool code_owner_approval_required { get; set; }
    }
    public class shared_with_groups
    {
        public int group_id { get; set; }
        public string group_name { get; set; }
        public int group_access_level { get; set; }
        public DateTime? expires_at { get; set; }
    }
    public class ProjectUsers
    {
        public int id { get; set; }
        public string username { get; set; }
        public string name { get; set; }
        public string state { get; set; }
        public string expires_at { get; set; }
        public int access_level { get; set; }
    }
    public class Send
    {
        public string username { get; set; }
        public string name { get; set; }
        public string project_name { get; set; }
        public string project_url { get; set; }
        public string expires_at { get; set; }
    }
    public class Event
    {
        public int id { get; set; }
        public string title { get; set; }
        public int project_id { get; set; }
        public string action_name { get; set; }
        public int author_id { get; set; }
        public DateTime? created_at { get; set; }
        public author author { get; set; }
    }
    public class author
    {
        public string name { get; set; }
        public string username { get; set; }
        public int id { get; set; }
        public string state { get; set; }
        public string avatar_url { get; set; }
        public string web_url { get; set; }
    }
   public class SingleProject
    {
        public int id { get; set; }
        public string description { get; set; }
        public string default_branch { get; set; }
        public string visibility { get; set; }
        public string ssh_url_to_repo { get; set; }
        public string http_url_to_repo { get; set; }
        public string web_url { get; set; }
        public string readme_url { get; set; }
    }
    public class Commits
    {
        public string id { get; set; }
        public string short_id { get; set; }
        public string created_at { get; set; }
        public string description { get; set; }
        public string title { get; set; }
        public string message { get; set; }
        public string author_name { get; set; }
        public string author_email { get; set; }
        public string authored_date { get; set; }
        public string committer_name { get; set; }
        public string committer_email { get; set; }
        public string committed_date { get; set; }
        public string web_url { get; set; }
    }
}