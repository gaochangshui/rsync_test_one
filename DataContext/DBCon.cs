using GitlabManager.DataContext;

namespace GitLabManager.DataContext
{
    public class DBCon
    {
        public static ApplicationDbContext db = new ApplicationDbContext();
        public static AgoraDbContext db_agora = new AgoraDbContext();

        public static ApplicationDbContext db_two = new ApplicationDbContext();
        public static AgoraDbContext db_agora_two = new AgoraDbContext();
    }
}