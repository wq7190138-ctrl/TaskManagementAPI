using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace TaskManagementAPI.Services.RabbitMQ
{
    public class RabbitMQService : IRabbitMQService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMQService> _logger;
        private readonly Lazy<Task<IConnection>> _connectionLazy;

        public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionLazy = new Lazy<Task<IConnection>>(async () =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                    Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                    UserName = configuration["RabbitMQ:UserName"] ?? "admin",
                    Password = configuration["RabbitMQ:Password"] ?? "123456"
                };
                var connection = await factory.CreateConnectionAsync();
                _logger.LogInformation("✅ RabbitMQ 连接已建立");
                return connection;
            });
        }

        /// <summary>
        /// 发布消息+生产者确认
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task PublishWithConfirmAsync<T>(string queue, T message)
        {
            try
            {
                // 第一次访问懒加载时创建连接
                //懒加载：避免在应用启动时就创建连接（可能 RabbitMQ 还没完全启动）。
                //确保整个应用生命周期内只有一个连接（连接复用，节省资源）。
                var connection = await _connectionLazy.Value;

                // ✅ 创建通道时启用发布确认
                var options = new CreateChannelOptions(
                    publisherConfirmationsEnabled: true, // 开启发布确认
                    publisherConfirmationTrackingEnabled: true, // 开启内部追踪，让 BasicPublishAsync 可以等待确认
                    outstandingPublisherConfirmationsRateLimiter: null, // 限制未确认消息数量，防止内存暴涨（通常设为 null 使用默认）
                    consumerDispatchConcurrency: 1 // 消费者并发度（设为 1 保证消息顺序）
                );

                // 每次创建通道（短生命周期）
                //从连接中创建一个通道（Channel）。
                //关键设计：通道是轻量级的，可以频繁创建和释放。每次发送消息都创建一个新通道，用完即丢，这是官方推荐的做法（通道不是线程安全的，每次使用独立通道避免竞争）。
                await using var channel = await connection.CreateChannelAsync(options);

                var json = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(json);

                //第二层：消息持久化（Message Persistent）
                //消息持久化（关键步骤）
                //⚠️ 性能警告（重要！）
                //消息持久化意味着 “同步写硬盘”。如果你每秒发送成千上万条消息，频繁写硬盘会严重拖垮吞吐量。
                //交易数据（支付、订单）：必须开启，消息绝对不能丢。
                //日志记录、非关键通知：可以考虑关闭，丢几条无所谓，速度优先。
                var properties = new BasicProperties
                {
                    DeliveryMode = DeliveryModes.Persistent // 设置为持久化
                };

                //发布消息
                //第三层：交换机持久化（Exchange Durable）
                //exchange: ""：使用默认交换机（Direct Exchange），直接根据 routingKey（即队列名）路由消息。
                //routingKey: queue：指定目标队列名。
                //body: body：消息内容（字节数组）。
                await channel.BasicPublishAsync(
                     exchange: "",
                     routingKey: queue, //默认交换机的routingKey与消息队列名称是相同的
                     mandatory: false,                  // 👈 显式传递 mandatory 参数
                     basicProperties: properties,
                     body: body,
                     cancellationToken: CancellationToken.None
                 );

                _logger.LogInformation($"✅ 消息已发送到队列 [{queue}]");


                //终极保障：发布确认（Publisher Confirms）
                //即使你设置了持久化，也存在一个时间窗口：消息刚发出，还没写入磁盘，服务器就挂了。为了彻底解决这个问题，RabbitMQ 提供了 发布确认（Publisher Confirms） 机制，确保消息确确实实被服务器写入磁盘后再返回给生产者成功信号
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发送消息到队列 [{queue}] 失败");
                throw;
            }
        }

        /// <summary>
        /// 发布延迟消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exchange"></param>
        /// <param name="routingKey"></param>
        /// <param name="message"></param>
        /// <param name="ttlMilliseconds">设置TTL，该值与队列TTL取最小值</param>
        /// <returns></returns>
        public async Task PublishDelayedAsync<T>(string exchange, string routingKey, T message, int ttlMilliseconds = 0)
        {
            var connection = await _connectionLazy.Value;
            await using var channel = await connection.CreateChannelAsync();
            var properties = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent,
                Expiration = ttlMilliseconds > 0 ? ttlMilliseconds.ToString() : null
            };
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            await channel.BasicPublishAsync(exchange, routingKey, false, properties, body);
        }
    }
}
