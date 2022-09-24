using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace GitlabManager.Models
{
    [Table("users", Schema = "public")]
    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int id { get; set; }
        public string email { get; set; }
        public string username { get; set; }
        public string name { get; set; }
        public string avatar { get; set; }
        public string preferred_language { get; set; }
        public bool external { get; set; }
        public bool admin { get; set; }
    }
    public class LoginModel
    {
        [Required(ErrorMessage = "Please enter the {0}.")]
        [Display(Name = "UserCode")]
        public string UserCD { get; set; }
        public string UserName { get; set; }
        public string mail { get; set; }
        public string AvatarUrl { get; set; }
        public string WebUrl { get; set; }

        [Required(ErrorMessage = "Please enter the {0}.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        //[Display(Name = "ログインしたままにする")]
        //public bool RememberMe { get; set; }
        //public string ConfirmPassword { get; set; }

    }
}