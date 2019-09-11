using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace Paragraph.Service
{
    /// <summary>
    /// Класс, реализующий работу с локальной очередью сообщений.
    /// </summary>
    public class LocalQueue
    {
        /// <summary>
        /// Имя файла для хранения очереди.
        /// </summary>
        private const string QueueFileName = "LocalQueue.json";
        /// <summary>
        /// Локальная очередь сообщений.
        /// </summary>
        public Queue<string> LocalTempQueue { get; private set; }
        /// <summary>
        /// Количество сообщений в очереди.
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// Создание локальной очереди сообщений.
        /// </summary>
        public LocalQueue()
        {
            LocalTempQueue = new Queue<string>();
        }

        /// <summary>
        /// Очистить локальную очередь сообщений.
        /// </summary>
        public void Clear()
        {
            LocalTempQueue.Clear();
            Count = 0;
        }

        /// <summary>
        /// Добавить сообщение очередь.
        /// </summary>
        /// <param name="Message"></param>
        public void Enqueue(string Message)
        {
            LocalTempQueue.Enqueue(Message);
            Count = LocalTempQueue.Count;
        }

        /// <summary>
        /// Извлечь сообщение из очереди.
        /// </summary>
        /// <returns></returns>
        public string Dequeue()
        {
            Count = LocalTempQueue.Count;
            return LocalTempQueue.Dequeue();
        }
        /// <summary>
        /// Определить сохранена ли очередь на диске.
        /// </summary>
        /// <returns></returns>
        public bool QueueFileExists()
        {
            if (File.Exists(QueueFileName))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Сохранить локальную очередь сообщений на диске.
        /// </summary>
        /// <returns></returns>
        public bool SaveQueue()
        {
            // Сохранить очередь на диске, если она не пустая.
            if (Count > 0)
            {
                try
                {
                    string queue = JsonConvert.SerializeObject(LocalTempQueue);
                    using (var sw = new StreamWriter(QueueFileName, false, System.Text.Encoding.Default))
                    {
                        var content = JsonConvert.SerializeObject(LocalTempQueue);
                        sw.WriteLine(content);
                        Console.WriteLine($"Save {Count} item(s) local queue to {QueueFileName}.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LocalQueueError. {ex.Message}");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("LocalQueueError. Queue is empty.");
                return false;
            }
        }

        /// <summary>
        /// Считать очередь сообщений из диска, если очередь пустая.
        /// </summary>
        /// <returns></returns>
        public bool LoadQueue()
        {
            // Считать информацию из временного файла в очередь, если она пустая.
            if (Count == 0)
            {
                try
                {
                    if (File.Exists(QueueFileName))
                    {
                        using (var sr = new StreamReader(QueueFileName, System.Text.Encoding.Default))
                        {
                            var content = sr.ReadToEnd();
                            LocalTempQueue = JsonConvert.DeserializeObject<Queue<string>>(content);
                            sr.Close();
                            File.Delete(QueueFileName);
                            Count = LocalTempQueue.Count;
                            Console.WriteLine($"Load {Count} local message(s) from {QueueFileName}. {QueueFileName} deleted.");
                        }
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("LocalQueueError. File not exists.");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LocalQueueError. {ex.Message}");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Load Local Queue error. Queue is not empty.");
                return false;
            }
        }
    }
}
