using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GitlabManager.Models
{
    [Table("warehouses", Schema = "public")]
    public class Warehouse
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string id { get; set; }
        public string pj_name { get; set; }
        public string creator_id { get; set; }
        public string creator_name { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public string last_activity_at { get; set; }
        public string description { get; set; }
        public bool archived { get; set; }
        public string group_id { get; set; }
        public string group_name { get; set; }
        public string project_member { get; set; }
        public string group_member { get; set; }
        public string sync_time { get; set; }
    }
    public class Page_Warehouses
    {
        public int pageSize { get; set; }
        public int pageNum { get; set; }
        public int pageNumAll { get; set; }
        public int rowCount { get; set; }
        public List<Warehouse> Warehouses { get; set; }

    }
    public class Sync
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string transfer { get; set; }
        public string userCode { get; set; }
        public string id { get; set; }
        public string pjName { get; set; }
        public string description { get; set; }
        public string creatorId { get; set; }
        public string creatorName { get; set; }
        public string createdAt { get; set; }
        public string updatedAt { get; set; }
        public string groupId { get; set; }
        public string groupName { get; set; }
        public string projectMember { get; set; }
        public string groupMember { get; set; }
        public string syncTime { get; set; }
    }
}