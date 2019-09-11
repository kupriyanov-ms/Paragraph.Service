using Modbus.Device;
using Modbus.Utility;
using System;
using System.IO.Ports;

namespace Paragraph.Service
{
    /// <summary>
    /// Чтение параметров прибора Параграф PL20.
    /// ПРИБОР ДЛЯ ИЗМЕРЕНИЯ И РЕГИСТРАЦИИ ТЕМПЕРАТУРЫ И ДРУГИХ ФИЗИЧЕСКИХ ВЕЛИЧИН, А ТАКЖЕ ДЛЯ УПРАВЛЕНИЯ ТЕХНОЛОГИЧЕСКИМИ ПРОЦЕССАМИ.
    /// Интерфейс RS-485 (протокол Modbus RTU).
    /// Встроенная память: 4 МБ(до 1 000 000 значений).
    /// Количество каналов: 2.
    /// </summary>
    class Paragraph
    {
        /// <summary>
        /// Значение температуры, измеренной на 1 и 2 канале.
        /// </summary>
        public float[] Temperatures { get; set; }
        /// <summary>
        /// Тип сенсоров, подключенных к 1 и 2 каналу.
        /// </summary>
        public ushort[] SensorsType { get; set; }
        /// <summary>
        /// Скорость передачи данных по интерфейсу RS485 бит/с.
        /// </summary>
        public ushort rs485_speed { get; set; }
        /// <summary>
        /// Бит данных.
        /// </summary>
        public ushort rs485_bytes { get; set; }
        /// <summary>
        /// Четность: да, нет.
        /// </summary>
        public ushort rs485_parity { get; set; }
        /// <summary>
        /// Количество стоповых бит, RS485.
        /// </summary>
        public ushort rs485_stopbits { get; set; }
        /// <summary>
        /// Дата/время установленные в приборе: dd:mm:yy hh:mm:ss.
        /// </summary>
        public ushort day { get; set; }
        public ushort month { get; set; }
        public ushort year { get; set; }
        public ushort hour { get; set; }
        public ushort minute { get; set; }
        public ushort second { get; set; }

        /// <summary>
        /// Получить название датчика по типу.
        /// </summary>
        /// <param name="sensorType">Тип датчика</param>
        /// <returns>Название датчика.</returns>
        static string GetSensorModel(ushort sensorType)
        {
            var desc = "";
            switch (sensorType)
            {
                default:
                    desc = "Thermocouple";
                    break;
            }

            return desc;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="slaveId"></param>
        public Paragraph(SerialPort port, byte slaveId)
        {
            // Протокол работы с приборами Параграф PL20 MODBUS RTU.
            IModbusSerialMaster master = ModbusSerialMaster.CreateRtu(port);

            // Описание регистров прибора https://www.kipspb.ru/download/documentation/Network_devices_protocol_MODBUS1.pdf
            // Адреса регистров в руководстве указаны в 10-чном виде, в коде нужно указывать в 16-ричном.

            // Значение температуры можно получить с помощью функции Input registers - std modbus function 4.
            // У Параграф PL20 2 измерительных канала. Адрес регистра 1 канала - 0, тип данных Single, разрядность 32 бит.
            ushort[] inputs = master.ReadInputRegisters(slaveId, 0x0, 4);
            this.Temperatures = new float[2];
            // Если значение недоступно, то регистры содержат 0, на дисплее прибора ----.
            this.Temperatures[0] = ModbusUtility.GetSingle(inputs[0], inputs[1]);
            this.Temperatures[1] = ModbusUtility.GetSingle(inputs[2], inputs[3]);
            //Console.WriteLine($"Temp. channel #1: {this.Temperatures[0]} #2: {this.Temperatures[1]}");

            // Тип подключенных к прибору датчиков.
            // TODO m.s.kupriyanov: Установить соответствие датчиков нет возможности.
            this.SensorsType = master.ReadHoldingRegisters(slaveId, 0x30, 2);
            //Console.WriteLine($"Sensor type #1: {this.SensorsType[0]} #2: {this.SensorsType[1]}");

            // Получить информацию о настройке интерфейса RS485.
            ushort[] rs485 = master.ReadHoldingRegisters(slaveId, 0x3E, 5);
            //Console.WriteLine($"RS485. Slave id: {rs485[0]}, Speed: {rs485[1]}, Bytes: {rs485[2]}, Parity: {rs485[3]}, Stop bits: {rs485[4]}");

            // Получить информацию о настройке даты/времени прибора.
            ushort[] rtc = master.ReadInputRegisters(slaveId, 0xC, 7);
            //Console.WriteLine($"Date/time: {rtc[4]}.{rtc[5]}.20{rtc[6]} {rtc[2]}:{rtc[1]}:{rtc[0]}");
        }
    }
}
