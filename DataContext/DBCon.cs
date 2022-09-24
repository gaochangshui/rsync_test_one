using GitlabManager.DataContext;

namespace GitLabManager.DataContext
{
    public class DBCon
    {
        public static ApplicationDbContext db = new ApplicationDbContext();
        public static AgoraDbContext db_agora = new AgoraDbContext();
    }
}