using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Text;

namespace Paragraph.Service
{
    /// <summary>
    /// Обеспечивает работу с брокером сообщений.
    /// </summary>
    public class MonitoringQueue
    {
        private IConnection connection;
        private IModel channel;
        private string exchange;
        private string routingKey;
        private Configuration config;

        /// <summary>
        /// Интерфейс для логирования.
        /// </summary>
        private readonly ILogger<MonitoringQueue> Log;

        /// <summary>
        /// Конструктор с внедрением интерфейса для логирования.
        /// </summary>
        /// <param name="logger">Интерфейс для логирования.</param>
        public MonitoringQueue(ILogger<MonitoringQueue> logger)
        {
            this.Log = logger;
        }

        /// <summary>
        /// Возвращает true если подключение к очереди установлено.
        /// </summary>
        public bool Connected
        {
            get { return connection.IsOpen; }
        }

        /// <summary>
        /// Подключиться к брокеру сообщений.
        /// </summary>
        /// <param name="configuration"></param>
        public bool Connect(Configuration configuration)
        {
            try
            {
                // Инициализировать подключение к брокеру, используя конфигурацию.
                var factory = new ConnectionFactory()
                {
                    HostName = configuration.ESBHost,
                    Port = configuration.ESBPort,
                    UserName = configuration.ESBUser,
                    Password = configuration.ESBPass,

                    // Разрешить восстановление соединения.
                    AutomaticRecoveryEnabled = true,

                    // Установить интервал для попыток восстановления соединения.
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(60)

                };

                // Установить интервал для попыток восстановления соединения.
                factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(30);

                // Установить точку обмена.
                exchange = configuration.ESBExchange;

                // Установить ключ пути.
                routingKey = configuration.ESBRoutingKey;

                // Создать подключение к брокеру.
                connection = factory.CreateConnection();

                // Создать канал передачи сообщений.
                channel = connection.CreateModel();

                // Объявить точку обмена с типом доставки шаблон (topic).
                channel.ExchangeDeclare(exchange: configuration.ESBExchange,
                    type: "topic");
                Log.LogInformation($"Connect to HostName: {configuration.ESBHost} Port: {configuration.ESBPort} UserName: {configuration.ESBUser} Exchange: {configuration.ESBExchange}");
                this.config = configuration;
                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"ESB Connection Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Отправить сообщение в точку обмена.
        /// </summary>
        /// <param name="message">Строка с ответом оборудования.</param>
        public void Publish(string message)
        {
            try
            {
                // Если публикация сообщений в очередь не разрешена, то выход.
                if (!config.ESBPublish)
                {
                    return;
                }

                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: exchange,
                    routingKey: routingKey,
                    basicProperties: null,
                    body: body);
            }
            catch (Exception ex)
            {
                Log.LogError($"Publish message error: {ex.Message}");
            }
        }

        /// <summary>
        /// Закрыть подключение к очереди.
        /// </summary>
        public void Close()
        {
            channel.Close();
            connection.Close();
        }
    }
}
