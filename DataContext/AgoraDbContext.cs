using GitLabManager.Models;
using System.Data.Entity;

namespace GitLabManager.DataContext
{
    public class AgoraDbContext : DbContext
    {

        public AgoraDbContext() : base(nameOrConnectionString: "AgoraDbConnection")
        {

        }

        public virtual DbSet<ProjectSyncSettings> ProjectSyncSettings { get; set; }

        public virtual DbSet<Agreements> Agreements { get; set; }

        public virtual DbSet<UsersStarAgreements> UsersStarAgreements { get; set; }

        public virtual DbSet<CommitDetail> CommitHistory { get; set; }

    }
}