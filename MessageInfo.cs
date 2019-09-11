using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paragraph.Service
{
    /// <summary>
    /// Структура информационного сообщения для отправки в очередь.
    /// </summary>
    class MessageInfo
    {
        /// <summary>
        /// Номер станка.
        /// </summary>
        public long IdMachine { get; set; }
        /// <summary>
        /// Тип ЧПУ станка (10 - Termodat).
        /// </summary>
        public int CNCType { get; set; }
        /// <summary>
        /// Состояние станка.
        /// </summary>
        public int Status { get; set; }
        /// <summary>
        /// Измеренная температура.
        /// </summary>
        public float Temperature { get; set; }
        /// <summary>
        /// Измеренная температура 2 зоны.
        /// </summary>
        public float Temperature2 { get; set; }
        /// <summary>
        /// Измеренная температура 3 зоны.
        /// </summary>
        public float Temperature3 { get; set; }
        /// <summary>
        /// Дата/время события.
        /// </summary>
        public string EventTime { get; set; }
        /// <summary>
        /// Имя сервиса, отправившего событие.
        /// </summary>
        public string ServiceName { get; set; }
    }
}
