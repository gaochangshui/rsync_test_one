using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using Npgsql;
using GitlabManager.Models;
using GitLabManager.Models;

namespace GitlabManager.DataContext
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base(nameOrConnectionString: "GitLabDbConnection")
        {

        }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Projects> Projects { get; set; }
        public virtual DbSet<NameSpaces> NameSpaces { get; set; }
        public virtual DbSet<Members> Members { get; set; }
    }
}