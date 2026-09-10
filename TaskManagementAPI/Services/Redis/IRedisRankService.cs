using TaskManagementAPI.DTOs.RedisTest;

namespace TaskManagementAPI.Services.Redis
{
    /// <summary>
    /// Redis Sorted Set 数据类型，模拟Rank积分榜，每个项目新增任务 +1分，任务完成 +2分
    /// 适用场景：排行榜、延迟队列、带权重的任务调度
    /// </summary>
    public interface IRedisRankService
    {
        Task IncrementScoreAsync(string rankingKey, string member, double increment);
        Task<List<RankItemDto>> GetTopRankAsync(string rankingKey, int topN);
        Task<double?> GetMemberScoreAsync(string rankingKey, string member);
        Task<long?> GetMemberRankAsync(string rankingKey, string member);
    }
}
