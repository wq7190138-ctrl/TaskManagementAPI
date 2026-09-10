namespace TaskManagementAPI.Services.RabbitMQ
{
    public interface IRabbitMQService
    {
        /// <summary>
        /// 发布普通消息队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task PublishWithConfirmAsync<T>(string queue, T message);

        /// <summary>
        /// 发布延迟消息队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exchange"></param>
        /// <param name="routingKey"></param>
        /// <param name="message"></param>
        /// <param name="ttlMilliseconds"></param>
        /// <returns></returns>
        Task PublishDelayedAsync<T>(string exchange, string routingKey, T message, int ttlMilliseconds = 0);
    }
}
