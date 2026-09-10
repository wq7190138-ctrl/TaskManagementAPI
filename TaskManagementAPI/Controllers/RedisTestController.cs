using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Data;
using TaskManagementAPI.DTOs.Project;
using TaskManagementAPI.Entities;
using TaskManagementAPI.Services.Interfaces;
using TaskManagementAPI.Services.RabbitMQ;
using TaskManagementAPI.Services.Redis;

namespace TaskManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RedisTestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IRedisCartService _cart;
        private readonly IRabbitMQService _rabbitMQ;
        private readonly IRedisRankService _rank;
        private readonly IRedisListService _list;
        private readonly IRedisSetService _set;
        private readonly IProjectService _project;
        public RedisTestController(AppDbContext context, IRedisCartService cart, IRabbitMQService rabbitMQ, IRedisRankService rank, IRedisListService list, IRedisSetService set, IProjectService project)
        {
            _cart = cart;
            _rabbitMQ = rabbitMQ;
            _context = context;
            _rank = rank;
            _list = list;
            _set = set;
            _project = project;
        }

        [HttpPost("cart")]
        public async Task<IActionResult> TestCart()
        {
            var userId = "test_user_001";

            // 1. 添加商品
            await _cart.AddItemAsync(userId, 101, 2);
            await _cart.AddItemAsync(userId, 102, 1);
            await _cart.AddItemAsync(userId, 103, 3);

            // 2. 更新数量
            await _cart.UpdateItemQuantityAsync(userId, 101, 5);

            // 3. 获取购物车
            var cart = await _cart.GetCartAsync(userId);

            // 4. 获取商品数量
            var count = await _cart.GetItemCountAsync(userId);

            return Ok(new { cart, count });
        }

        [HttpPost("cart-checkout")]
        public async Task<IActionResult> Checkout(string userId)
        {
            var cart = await _cart.GetCartAsync(userId);
            if (!cart.Any()) return BadRequest("购物车为空");

            // ✅ 只发一条 MQ 消息，包含整个购物车
            await _rabbitMQ.PublishWithConfirmAsync("cart_checkout", new
            {
                UserId = userId,
                Items = cart.Select(kv => new { ProductId = kv.Key, Quantity = kv.Value }),
                CheckoutTime = DateTime.UtcNow
            });

            // 清空 Redis 购物车
            await _cart.ClearCartAsync(userId);

            return Ok(new { message = "订单已提交，正在处理" });
        }

        [HttpGet("rank")]
        public async Task<IActionResult> TestRank()
        {
            var rankList = await _rank.GetTopRankAsync("rank:project:active", 5);
            return Ok(rankList);
        }

        [HttpGet("rank/history")]
        public async Task<IActionResult> GetRankHistory([FromQuery] int days = 7)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);
            var snapshots = await _context.RankSnapshots
                .Where(s => s.SnapshotDate >= startDate)
                .OrderBy(s => s.SnapshotDate)
                .ToListAsync();

            return Ok(snapshots.Select(s => new
            {
                s.SnapshotDate,
                Rankings = JsonSerializer.Deserialize<object>(s.Rankings)
            }));
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs(int count = 20)
        {
            var logs = await _list.GetRecentLogsAsync("logs:task:actions", count);
            return Ok(logs);
        }

        // 添加标签
        [HttpPost("tags")]
        public async Task<IActionResult> AddTags([FromQuery] string projectId, [FromBody] string[] tags)
        {
            await _set.AddTagsAsync($"project:{projectId}:tags", tags);
            return Ok(new { message = "标签添加成功" });
        }

        // 获取标签
        [HttpGet("{projectId}/tags")]
        public async Task<IActionResult> GetTags(string projectId)
        {
            var tags = await _set.GetTagsAsync($"project:{projectId}:tags");
            return Ok(tags);
        }

        // 查找同时包含 tag1 和 tag2 的项目
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                var allProjects = await _project.GetAllProjectsAsync();
                return Ok(allProjects);
            }

            var projects = await _project.GetAllProjectsAsync();
            var result = new List<ProjectResponseDto>();
            foreach (var project in projects)
            {
                var projectTags = await _set.GetTagsAsync($"project:{project.Id}:tags");
                if (tags.All(tag => projectTags.Contains(tag)))
                {
                    result.Add(project);
                }
            }
            //var common = await _set.IntersectAsync($"project:1:tags", $"project:2:tags");
            return Ok(result);
        }
    }
}
