using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQConsumer.Services
{
    public class TaskCreatedConsumerService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TaskCreatedConsumerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private IConnection _connection;
        private IChannel _channel;

        public TaskCreatedConsumerService(
            IConfiguration configuration,
            ILogger<TaskCreatedConsumerService> logger,
            IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 重试连接，直到成功或取消
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 尝试连接 RabbitMQ
                    var factory = new ConnectionFactory
                    {
                        HostName = _configuration["RabbitMQ:Host"] ?? "rabbitmq",
                        Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                        UserName = _configuration["RabbitMQ:UserName"] ?? "admin",
                        Password = _configuration["RabbitMQ:Password"] ?? "123456"
                    };

                    _connection = await factory.CreateConnectionAsync();
                    _channel = await _connection.CreateChannelAsync();

                    // 1. 异步声明死信交换机（DLX）
                    // 作用：创建了一个名为 task.dlx.exchange 的交换机，专门负责接收“坏消息”（死信）。
                    // 比喻：这是“退货处理中心”，所有无法正常投递的包裹都送到这里。
                    await _channel.ExchangeDeclareAsync(
                        exchange: "task.dlx.exchange",
                        type: ExchangeType.Direct,
                        durable: true
                    );

                    // 2. 异步声明死信队列（DLQ）
                    // 作用：创建了一个名为 task.dlq 的普通队列，用于存储死信消息。
                    // 比喻：这是“退货存放仓库”，所有坏消息最终被放在这里，等待人工 / 系统处理。
                    await _channel.QueueDeclareAsync(
                        queue: "task_created.dlq",
                        durable: true, //持久化，优先级低，exclusive和autoDelete必须都是false
                        exclusive: false, //true私有队列/false共享队列，true时durable无效。临时、非核心、数据丢失无影响下才有可能用true
                        autoDelete: false, //自动删除，true时durable无效
                        cancellationToken: stoppingToken //有stoppingToken，服务能收到停止通知，等待当前消息处理完毕，再安全关闭。
                                                         //无stoppingToken，服务强制终止，可能丢失正在处理的消息或导致连接泄漏。
                    );
                    await _channel.QueueDeclareAsync(
                       queue: "task_updated.dlq",
                       durable: true,
                       exclusive: false,
                       autoDelete: false,
                       cancellationToken: stoppingToken
                   );

                    // 3. 绑定死信队列到死信交换机
                    // 作用：告诉死信交换机 task.dlx.exchange：“如果收到路由键为 task.failed 的消息，请把它放进 task.dlq 队列。”
                    // 比喻：在退货处理中心和退货仓库之间建立一条通道，约定用“失败标记”作为转运凭证。
                    await _channel.QueueBindAsync(
                        queue: "task_created.dlq",
                        exchange: "task.dlx.exchange",
                        routingKey: "task_created.failed"
                    );
                    await _channel.QueueBindAsync(
                       queue: "task_updated.dlq",
                       exchange: "task.dlx.exchange",
                       routingKey: "task_updated.failed"
                    );

                    // 4. 声明主队列（并指定死信参数）—— 最关键的一步！
                    // 作用：配置主队列（task_created）的死信策略：“当本队列中的消息变成死信时，自动把它转发到 task.dlx.exchange 交换机，并使用路由键 task.failed。”
                    // 比喻：在快递柜（主队列）上贴了一张告示：“如果包裹因拒收、超时或溢出无法投递，请自动转交给退货处理中心（DLX），并注明‘失败’标签。”
                    var argsCreated = new Dictionary<string, object?>
                    {
                        ["x-dead-letter-exchange"] = "task.dlx.exchange",      // 指定死信转发到的交换机
                        ["x-dead-letter-routing-key"] = "task_created.failed"          // 转发时使用的路由键
                    };
                    var argsUpdated = new Dictionary<string, object?>
                    {
                        ["x-dead-letter-exchange"] = "task.dlx.exchange",
                        ["x-dead-letter-routing-key"] = "task_updated.failed"
                    };
                    // 队列由消费者声明，不要在生产者中声明
                    await _channel.QueueDeclareAsync(
                        queue: "task_created",
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: argsCreated,
                        cancellationToken: stoppingToken
                    );
                    await _channel.QueueDeclareAsync(
                        queue: "task_updated",
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: argsUpdated,
                        cancellationToken: stoppingToken
                    );

                    // 创建Created消费者...
                    var consumerCreated = new AsyncEventingBasicConsumer(_channel);
                    consumerCreated.ReceivedAsync += async (model, ea) =>
                    {
                        await ProcessMessageAsync("task_created", ea);
                    };

                    // 创建Updated消费者...
                    var consumerUpdated = new AsyncEventingBasicConsumer(_channel);
                    consumerUpdated.ReceivedAsync += async (model, ea) =>
                    {
                        await ProcessMessageAsync("task_updated", ea);
                    };

                    await _channel.BasicConsumeAsync(
                        queue: "task_created",
                        autoAck: false,
                        consumer: consumerCreated,
                        cancellationToken: stoppingToken
                    );
                    await _channel.BasicConsumeAsync(
                       queue: "task_updated",
                       autoAck: false,
                       consumer: consumerUpdated,
                       cancellationToken: stoppingToken
                    );

                    // 延时消息步骤：
                    // 声明延迟交换机 ExA
                    // 声明 QA 的死信参数，指定死信交换机为 ExB，死信路由为 KeyB，过期时间
                    // 声明延迟队列 QA ，并绑定死信参数
                    // 绑定 QA、ExA，设置路由键 KeyA
                    // 声明死信交换机 ExB
                    // 声明死信队列 QB
                    // 绑定 QB、ExB，设置路由键 KeyB

                    // QA 不可以被消费，如果消费则不会触发TTL过期，消息无法进入死信队列被消费
                    // 发送消息到 ExA，通过 KeyA 路由消息入队 QA
                    // QA 中的这条消息过期时，会被MQ判定为死信
                    // MQ通过 QA 绑定的死信参数，将消息发送到 ExB，通过路由 KeyB 消息入队 QB
                    // 创建 QB 的消费者，用来测试延时消息过期死信是否成功


                    // 延时消息
                    await _channel.ExchangeDeclareAsync("order.delay.exchange", ExchangeType.Direct, durable: true);
                    var delayArgs = new Dictionary<string, object?>
                    {
                        ["x-message-ttl"] = 5 * 1000, // 30 分钟
                        ["x-dead-letter-exchange"] = "order.dlx.exchange",
                        ["x-dead-letter-routing-key"] = "order.cancel"
                    };
                    await _channel.QueueDeclareAsync("order.delay.queue", durable: true, exclusive: false, arguments: delayArgs);
                    await _channel.QueueBindAsync("order.delay.queue", "order.delay.exchange", "order.delay");

                    // 4. 延时消息的“死信”消费者（专门处理超时取消）
                    await _channel.ExchangeDeclareAsync("order.dlx.exchange", ExchangeType.Direct, durable: true);
                    await _channel.QueueDeclareAsync("order.cancel.queue", durable: true, exclusive: false);
                    await _channel.QueueBindAsync("order.cancel.queue", "order.dlx.exchange", "order.cancel");
                    // 创建消费者...
                    var consumerorderCancel = new AsyncEventingBasicConsumer(_channel);
                    consumerorderCancel.ReceivedAsync += async (model, ea) =>
                    {
                        await ProcessMessageAsync("order.cancel.queue", ea);
                    };
                    await _channel.BasicConsumeAsync(
                        queue: "order.cancel.queue",
                        autoAck: false,
                        consumer: consumerorderCancel,
                        cancellationToken: stoppingToken
                    );



                    await _channel.QueueDeclareAsync(
                        queue: "task_created.dlq",
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null,
                        cancellationToken: stoppingToken
                    );

                    var consumerDlq = new AsyncEventingBasicConsumer(_channel);
                    consumerDlq.ReceivedAsync += async (model, ea) =>
                    {
                        await ProcessMessageAsync("task_created.dlq", ea);
                    };

                    await _channel.BasicConsumeAsync(
                        queue: "task_created.dlq",
                        autoAck: false,
                        consumer: consumerDlq,
                        cancellationToken: stoppingToken
                    );




                    _logger.LogInformation("✅ 消费者已成功连接到 RabbitMQ 并开始监听");
                    break; // 连接成功，退出重试循环
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "连接 RabbitMQ 失败，10秒后重试...");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }

            // 保持服务运行
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }


        private async Task ProcessMessageAsync(string queue, BasicDeliverEventArgs ea)
        {
            _logger.LogInformation($"🔥 消费 {queue} 消息");
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                switch (queue)
                {
                    case "task_created":

                        var dataCreated = JsonSerializer.Deserialize<TaskCreatedMessage>(json);

                        if (dataCreated.Title.Contains("fail"))
                        {
                            throw new Exception("模拟业务异常：消息包含 'fail' 关键字");
                        }

                        // 1. 打印消息到控制台（通过日志）
                        _logger.LogInformation($"📩 收到任务创建消息: TaskId={dataCreated?.TaskId}, ProjectId={dataCreated?.ProjectId}, Title={dataCreated?.Title}");

                        break;
                    case "task_updated":

                        var dataUpdated = JsonSerializer.Deserialize<TaskUpdatedMessage>(json);

                        // 1. 打印消息到控制台（通过日志）
                        _logger.LogInformation($"📩 收到任务更新消息: TaskId={dataUpdated?.TaskId}, Updated={dataUpdated?.Updated}");

                        break;
                    case "order.cancel.queue":
                        // 1. 打印消息到控制台（通过日志）
                        var dataCreatedDelay = JsonSerializer.Deserialize<TaskCreatedMessage>(json);
                        _logger.LogInformation($"📩 收到延迟死信: TaskId={dataCreatedDelay?.TaskId}, ProjectId={dataCreatedDelay?.ProjectId}, Title={dataCreatedDelay?.Title}");
                        break;
                    case "task_created.dlq":
                        // 1. 打印消息到控制台（通过日志）
                        var dataCreated2 = JsonSerializer.Deserialize<TaskCreatedMessage>(json);
                        _logger.LogInformation($"📩 收到死信队列消息: TaskId={dataCreated2?.TaskId}, ProjectId={dataCreated2?.ProjectId}, Title={dataCreated2?.Title}");

                        break;
                }

                // 2. 模拟处理（比如发邮件、更新统计等）
                // 这里先只是打印，之后我们会添加邮件发送逻辑

                // 3. 确认消息（告诉 RabbitMQ 已成功处理）
                await _channel.BasicAckAsync(ea.DeliveryTag, false, CancellationToken.None);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "处理消息失败");
                //// 不确认，消息会重新入队（根据业务需求决定是否 requeue）
                //await _channel.BasicNackAsync(ea.DeliveryTag, false, true, CancellationToken.None);


                // 检查消息头中是否已有重试次数
                var retryCount = 0;
                if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.TryGetValue("x-retry-count", out var val))
                {
                    retryCount = Convert.ToInt32(val);
                }

                if (retryCount < 3)
                {
                    // 重试：构造新消息，增加重试计数，重新发送到原队列
                    var properties = new BasicProperties();
                    properties.Headers = new Dictionary<string, object?>
                    {
                        ["x-retry-count"] = retryCount + 1
                    };
                    // 注意：这里不能用 BasicNack(requeue=true)，因为那样会无限循环
                    // 正确做法：重新 Publish 到原队列，然后 Ack 掉当前消息
                    await _channel.BasicPublishAsync(
                        exchange: "",
                        routingKey: "task_created",
                        body: ea.Body,
                        basicProperties: properties,
                        mandatory: false
                    );
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    _logger.LogWarning("消息重试 {RetryCount}/3", retryCount + 1);
                }
                else
                {
                    // 重试耗尽：Nack 并拒绝重新入队 -> 消息自动进入死信队列！
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    _logger.LogError("消息已进入死信队列");
                }
            }
        }
    }

    public class TaskCreatedMessage
    {
        public int TaskId { get; set; }
        public int ProjectId { get; set; }
        public string Title { get; set; }
    }
    public class TaskUpdatedMessage
    {
        public int TaskId { get; set; }
        public string Updated { get; set; }
    }
}
