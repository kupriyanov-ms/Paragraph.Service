namespace Paragraph.Service
{
    /// <summary>
    /// Структура информационного сообщения для отправки в очередь.
    /// </summary>
    class MessageInfo
    {
        /// <summary>
        /// ИД оборудования.
        /// </summary>
        public long IdMachine { get; set; }

        /// <summary>
        /// Тип ЧПУ оборудования (13 - Paragraph PL20).
        /// </summary>
        public int CNCType { get; set; }

        /// <summary>
        /// Состояние оборудования.
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
