// Program.cs
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TaskManagementAPI.Data;
using TaskManagementAPI.Hubs;
using TaskManagementAPI.Middleware;
using TaskManagementAPI.Repositories.Implementations;
using TaskManagementAPI.Repositories.Interfaces;
using TaskManagementAPI.Services;
using TaskManagementAPI.Services.Background;
using TaskManagementAPI.Services.Implementations;
using TaskManagementAPI.Services.Interfaces;
using TaskManagementAPI.Services.RabbitMQ;
using TaskManagementAPI.Services.Redis;

var builder = WebApplication.CreateBuilder(args);

// 注册 DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// 注册 Redis 连接
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var redisConnectionString = config["Redis:ConnectionString"]
        ?? "172.18.0.9:26379,172.18.0.10:26379,172.18.0.8:26379,serviceName=mymaster,password=123456,defaultDatabase=0";

    var options = ConfigurationOptions.Parse(redisConnectionString);
    options.AbortOnConnectFail = false;
    // 注意：不要设置 CommandMap.Sentinel，因为你连接的是哨兵节点，而非哨兵模式本身
    return ConnectionMultiplexer.Connect(options);
});

// 注册本地缓存
builder.Services.AddMemoryCache();
builder.Services.AddHostedService<CacheInvalidationSubscriber>();

// 注册 Repositories
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();

// 注册 Services
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")  // 前端开发地址
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();  // SignalR 必须 AllowCredentials
        });
});
// 注册 Redis 服务
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<IRedisCartService, RedisCartService>();
builder.Services.AddScoped<IRedisRankService, RedisRankService>();
builder.Services.AddHostedService<RankSnapshotJob>();
builder.Services.AddScoped<IRedisListService, RedisListService>();
builder.Services.AddScoped<IRedisSetService, RedisSetService>();
// 注册 Redis布隆过滤器 服务
//builder.Services.AddSingleton<IRedisBloomFilterService, RedisBloomFilterService>(sp =>
//{
//    var connection = sp.GetRequiredService<IConnectionMultiplexer>();
//    var logger = sp.GetRequiredService<ILogger<RedisBloomFilterService>>();
//    // 创建过滤器，指定名称、误判率（1%）、容量（10万）
//    return new RedisBloomFilterService(connection, logger, "bloom:project", 0.01, 100000);
//});

// 注册 缓存预热 服务
builder.Services.AddHostedService<CacheWarmupService>();

// 注册 RabbitMQ 服务
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// 注册健康检查，并添加对数据库和 Redis 的检查
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .AddRedis(builder.Configuration["Redis:ConnectionString"]);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");
app.MapHub<CacheNotificationHub>("/api/cacheHub");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
//// 开发环境可以保留异常页面
//if (app.Environment.IsDevelopment())
//{
//    app.UseDeveloperExceptionPage();
//}
//else
//{
//    app.UseExceptionHandler("/error");
//}

// 映射健康检查端点
app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = _ => false // 只检查应用是否活着，不检查依赖
});
app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    // 默认会检查所有已注册的检查项（数据库、Redis等）
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // 自动迁移
    dbContext.Database.Migrate();
}

// 预热布隆过滤器
//using (var scope = app.Services.CreateScope())
//{
//    var bloomFilter = scope.ServiceProvider.GetRequiredService<IRedisBloomFilterService>();
//    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    try
//    {
//        var allProjectIds = dbContext.TbProjects.Select(p => $"project:{p.Id}").ToList();
//        await bloomFilter.WarmupAsync(() => allProjectIds);
//    }
//    catch (Exception ex)
//    {
//        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
//        logger.LogWarning(ex, "布隆过滤器预热失败，继续启动");
//    }
//}


app.Run();