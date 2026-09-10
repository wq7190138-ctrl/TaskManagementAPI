namespace TaskManagementAPI.Cache
{
    public static class CacheKeys
    {
        //全部项目缓存
        public const string ProjectAll = "projects:all";
        //项目id查询缓存
        public static string ProjectId(int id) => $"projects:{id}";
        //项目名称搜索缓存
        public static string ProjectSearchPrefix(string name = "") => $"projects:search:{name}";
        //全部任务缓存
        public static string TaskAll(int projectId) => $"projects:{projectId} tasks:all";
        //任务id查询缓存
        public static string TaskId(int id) => $"task:{id}";
    }
}
