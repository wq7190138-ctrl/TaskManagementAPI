using StackExchange.Redis;
using TaskManagementAPI.DTOs.RedisTest;

namespace TaskManagementAPI.Services.Redis
{
    /// <summary>
    /// Redis Sorted Set 数据类型，模拟Rank积分榜，每个项目新增任务 +1分，任务完成 +2分
    /// 适用场景：排行榜、延迟队列、带权重的任务调度
    /// </summary>
    public class RedisRankService:IRedisRankService
    {
        private readonly IDatabase _db;

        public RedisRankService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        // 增加分数（原子操作，不存在则创建）
        public async Task IncrementScoreAsync(string rankingKey, string member, double increment)
        {
            await _db.SortedSetIncrementAsync(rankingKey, member, increment);
        }

        // 获取 Top N（降序，带分数）
        public async Task<List<RankItemDto>> GetTopRankAsync(string rankingKey, int topN)
        {
            var entries = await _db.SortedSetRangeByRankWithScoresAsync(rankingKey, 0, topN - 1, Order.Descending);
            return entries.Select(e => new RankItemDto
            {
                Member = e.Element.ToString(),
                Score = e.Score
            }).ToList();
        }

        // 获取指定成员的分数
        public async Task<double?> GetMemberScoreAsync(string rankingKey, string member)
        {
            var score = await _db.SortedSetScoreAsync(rankingKey, member);
            return score;
        }

        // 获取指定成员的排名（0 表示第 1 名）
        public async Task<long?> GetMemberRankAsync(string rankingKey, string member)
        {
            // ZREVRANK 返回从 0 开始计数的倒序排名
            var rank = await _db.SortedSetRankAsync(rankingKey, member, Order.Descending);
            return rank;
        }
    }
}
