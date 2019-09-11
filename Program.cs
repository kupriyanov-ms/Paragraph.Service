using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using Serilog.Formatting.Elasticsearch;
using Serilog.Sinks.Elasticsearch;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace Paragraph.Service
{
    class Program
    {
        private static void LoggerService(IServiceCollection services)
        {
            // Сконфигурировать Serilog.
            var serilogConfiguration = new LoggerConfiguration()
                  .Enrich.FromLogContext()
                  .WriteTo.Console();

            serilogConfiguration.WriteTo.Elasticsearch(
                    new ElasticsearchSinkOptions(new Uri("http://haproxy-ha01.baikalinc.ru:9200/"))
                    {
                        AutoRegisterTemplate = true,
                        TemplateName = "devlog-scada-paragraph-service",
                        IndexFormat = "devlog-scada-paragraph-{0:yyyy.MM.dd}",
                        CustomFormatter = new ElasticsearchJsonFormatter()
                    });


            Log.Logger = serilogConfiguration.CreateLogger();
            services.AddLogging(logger => logger.AddSerilog(dispose: true))
                    .AddTransient<MonitoringQueue>();
        }

        static void Main(string[] args)
        {
            // Создать коллекцию сервисов.
            var serviceCollection = new ServiceCollection();
            // Сконфигурировать сервисы.
            LoggerService(serviceCollection);
            // Зарегистрировать провайдер.
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Загрузить сохраненную конфигурацию.
            Configuration configuration = Configuration.LoadConfig("Configuration.json", CNCType.Paragraph);
            if (configuration == null)
            {
                return;
            }

            // Инициализировать подключение к очереди.
            var queue = serviceProvider.GetService<MonitoringQueue>();
            if (!queue.Connect(configuration))
            {
                return;
            }

            // Создать локальную очередь сообщений.
            LocalQueue localQueue = new LocalQueue();
            // Если есть сохраненные сообщения.
            if (localQueue.QueueFileExists())
            {
                // Поместить сообщения в локальную очередь.
                localQueue.LoadQueue();
            }

            // Установить настройки COM-порта для Параграф PL20:
            // скорость передачи – 9600, стартовых бит – 1, число бит данных – 8, стоповых бит – 1, проверка четности отключена.
            SerialPort port = new SerialPort(configuration.COMPort, 9600, Parity.None, 8, StopBits.One);
            try
            {
                port.ReadTimeout = 2000;
                port.WriteTimeout = 2000;
                port.Open();
                Log.Information($"COM port #{configuration.COMPort} is available.");
            }
            catch (Exception ex)
            {
                Log.Error($"COM port #{configuration.COMPort} is not available. Err: {ex.Message}");
                return;
            }

            if (configuration.MachineTools == null)
            {
                Log.Warning("Device list is empty.");
                return;
            }

            Log.Information("Start Paragraph monitoring.");
            // Выполнить обработку оборудования.
            while (true)
            {
                var StartIteration = DateTime.Now;
                var OfflineMachines = 0;
                List<string> Messages = new List<string>();

                Log.Information($"{StartIteration.ToString("dd.MM.yyyy HH:mm:ss")} Start iteration.");

                // Обработать список оборудования.
                foreach (var machineTool in configuration.MachineTools)
                {
                    var start = DateTime.Now.Ticks;
                    var machine = machineTool.Value;
                    int status = (int)CNCStatus.OFFLINE;
                    List<double> temperatures = new List<double>();
                    double temp = 0;

                    string statusDesc = string.Empty;

                    Log.Information($"Paragraph PL20 id{machineTool.Key}-#{machine.CNCHost}.");

                    // Проверить доступность устройства и запросить параметры.
                    try
                    {
                        Paragraph paragraphPL20 = new Paragraph(port, Convert.ToByte(machine.CNCHost));
                        temperatures.Add(Math.Round(paragraphPL20.Temperatures[0], 1));
                        temperatures.Add(Math.Round(paragraphPL20.Temperatures[1], 1));

                        // Для прокалочных печей контролируется 3 зоны, для этого используется 2 2-х канальных измерителя.
                        /*
                         * 1 прибор
                         * 1 канал - ----
                         * 2 канал - 3 зона
                         * 2 прибор
                         * 1 канал - 2 зона
                         * 2 канал - 1 зона
                         */

                        if (Convert.ToInt32(machine.CNCHost) == 1)
                        {
                            Paragraph paragraph2 = new Paragraph(port, 2);
                            temperatures.Add(Math.Round(paragraph2.Temperatures[0], 1));
                            temperatures.Add(Math.Round(paragraph2.Temperatures[1], 1));
                        }

                        temperatures.Sort();
                        temp = temperatures.Last();

                        if (temp > 0)
                        {
                            status = (int)CNCStatus.START;
                        }

                        statusDesc = $"{((CNCStatus)status).ToString()} Temp: {temp} C";
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Paragraph PL20 #{machineTool.Key} offline. Err: {ex.Message}");
                        status = (int)CNCStatus.OFFLINE;
                        temp = 0;
                        statusDesc = CNCStatus.OFFLINE.ToString();
                        ++OfflineMachines;
                    }

                    // Сформировать сообщение для отправки в очередь.
                    var messageInfo = new MessageInfo()
                    {
                        CNCType = (byte)CNCType.Paragraph,
                        EventTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                        Temperature = (float)temp,
                        IdMachine = machineTool.Key,
                        Status = status,
                        ServiceName = $"{machine.CNCType.ToString()}Service",
                    };

                    string message = JsonConvert.SerializeObject(messageInfo);
                    Messages.Add(message);

                    Log.Information(message);

                    Log.Information($"Paragraph id{machineTool.Key}-#{machine.CNCHost}. Status: {statusDesc}. Dur: {(DateTime.Now.Ticks - start) / 10000} ms.");
                }

                var AllMachine = configuration.MachineTools.Count;
                Log.Information($"Paragraph in monitoring: {AllMachine}.");
                Log.Information($"Offline CNC(s): {OfflineMachines}. Online(!): {AllMachine - OfflineMachines}.");
                Log.Information("Send result to queue.");

                foreach (var message in Messages)
                {
                    // TODO m.s.kupriyanov: Здесь нужно агрегировать все полученные результаты и разом отправить в очередь.
                    // Если подключение к очереди доступно.
                    if (queue.Connected)
                    {
                        // Если локальная очередь содержит сообщения, то сначала обработать их.
                        if (localQueue.Count > 0)
                        {
                            Log.Information($"Publish {localQueue.Count} message(s) from local queue to service bus.");
                            foreach (var tempMessage in localQueue.LocalTempQueue)
                            {
                                queue.Publish(tempMessage);
                            }

                            localQueue.Clear();
                        }

                        // Отправить сообщение в очередь.
                        queue.Publish(message);
                    }
                    else
                    {
                        // DONE m.s.kupriyanov: Если отправка сообщения в очередь не удалась, то нужно сохранять сообщения локально, пока очередь не станет доступна.
                        localQueue.Enqueue(message);
                    }
                }

                // Вывести информацию о доступности очереди сообщений.
                string queueStatus = queue.Connected ? "available" : "not available";
                Log.Information($"Service bus: {queueStatus}.");

                // Вывести информацию о наличии сообщений в локальной очереди.
                if (localQueue.Count > 0)
                {
                    Log.Information($"Local Queue: {localQueue.Count} item(s)");

                    // Если очередь не пустая, то сохранять каждую итерацию опроса.
                    localQueue.SaveQueue();
                }

                var EndIteration = DateTime.Now;
                var IterationDelay = EndIteration - StartIteration;
                Log.Information($"{EndIteration.ToString("dd.MM.yyyy HH:mm:ss")} End iteration ({IterationDelay.Seconds} sec.).");
                // Определить разницу между периодом опроса и длительностью итерации опроса.
                var deltaDelay = configuration.PollingInterval - IterationDelay.Seconds;
                if (deltaDelay <= 0)
                {
                    // Установить задержку.
                    deltaDelay = 1;
                }

                // Приостановить выполнение программы.
                // Console.WriteLine($"Pause {DeltaDelay} sec.");
                Log.Information($"Pause {deltaDelay} sec.");
                Thread.Sleep(deltaDelay * 1000);
                Log.Information($"All tasks completed ({(DateTime.Now - StartIteration).Seconds} sec.).");
            }

            port.Close();
            queue.Close();
            Log.Information("Main end.");
            Console.WriteLine("Paragraph monitoring stopped.");
            Console.WriteLine("Press [Enter] for Exit.");
            Console.ReadLine();
        }
    }
}
