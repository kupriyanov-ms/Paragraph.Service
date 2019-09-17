using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Paragraph.Service
{
    /// <summary>
    /// Тип контроллера оборудования.
    /// </summary>
    public enum CNCType
    {
        #region CNCType
        /// <summary>
        /// Базовый тип ЧПУ. Определяем только состояние оборудования: доступно/недоступно,
        /// включено/выключено. Пока не реализован полноценный мониторинг.
        /// </summary>
        Generic = 0,

        /// <summary>
        /// Fanuc контроллер.
        /// </summary>
        Fanuc = 1,

        /// <summary>
        /// KARAT/MULTILATHE.
        /// </summary>
        OpenControl = 2,

        /// <summary>
        /// Контрольно-измерительная машина КИМ.
        /// </summary>
        CMM = 3,

        /// <summary>
        /// Лазеры DS4, с 6-осевым манипулятором ABB.
        /// </summary>
        Laser = 4,

        /// <summary>
        /// ЧПУ Heidenhain iTNC530.
        /// </summary>
        TNC530 = 5,

        /// <summary>
        /// Okuma multus
        /// </summary>
        Okuma = 6,

        /// <summary>
        /// ЧПУ Mitsubishi M70/700.
        /// </summary>
        Mitsubishi = 7,

        /// <summary>
        /// Мониторинг осуществляется измерителем ICP-DAS на основе транформаторов тока.
        /// </summary>
        PowerMeter = 8,

        /// <summary>
        /// Мониторинг Hurco WinMax с помощью стандартного wcf-сервиса.
        /// </summary>
        Hurco = 9,

        /// <summary>
        /// Мониторинг Термодат по протоколу MODBUS ASCII.
        /// </summary>
        Termodat = 10,

        /// <summary>
        /// Мониторинг устройств KMS - нашей собственной разработки.
        /// </summary>
        KMSMonitor = 11,

        /// <summary>
        /// Мониторинг ведомых устройств RS485 через ведущее устройство Ethernet-RS485.
        /// </summary>
        KMSMaster = 12,

        /// <summary>
        /// Мониторинг приборов-измерителей Параграф PL20.
        /// </summary>
        Paragraph = 13,
        #endregion
    }

    /// <summary>
    /// Стандартные состояния оборудования с ЧПУ.
    /// </summary>
    public enum CNCStatus
    {
        /// <summary>
        /// Состояние оборудования неизвестно, не соотнесено ни с одним из следующих.
        /// </summary>
        UNKNOWN = 0,

        /// <summary>
        /// Работает под управлением программы.
        /// </summary>
        START = 1,

        /// <summary>
        /// Остановлено.
        /// </summary>
        STOP = 2,

        /// <summary>
        /// Оборудование простаивает.
        /// </summary>
        HOLD = 3,

        /// <summary>
        /// Оборудование выключено/недоступно.
        /// </summary>
        OFFLINE = 4,

        /// <summary>
        /// Оборудование включено/есть доступ по сети, но определение состояния не реализовано/невозможно.
        /// </summary>
        ONLINE = 5,
    }

    /// <summary>
    /// Общие настройки для подключения к ЧПУ.
    /// </summary>
    public class MachineTool
    {
        /// <summary>
        /// Тип контроллера ЧПУ.
        /// </summary>
        public CNCType CNCType { get; set; }

        /// <summary>
        /// Адрес для подключения к ЧПУ.
        /// </summary>
        public string CNCHost { get; set; }

        /// <summary>
        /// Порт для подключения к ЧПУ.
        /// </summary>
        public int CNCPort { get; set; }
    }

    /// <summary>
    /// Общие настройки сервиса чтения данных с ЧПУ.
    /// </summary>
    public class Configuration
    {
        #region Configuration Fields

        /// <summary>
        /// Список ЧПУ.
        /// </summary>
        public Dictionary<long, MachineTool> MachineTools;

        /// <summary>
        /// Интервал опроса оборудования, секунд.
        /// </summary>
        public int PollingInterval { get; set; }

        /// <summary>
        /// Адрес IMZ_BUS.
        /// </summary>
        public string ESBHost { get; set; }

        /// <summary>
        /// Номер порта (IMZ_BUS).
        /// </summary>
        public int ESBPort { get; set; }

        /// <summary>
        /// Имя пользователя (IMZ_BUS). 
        /// </summary>
        public string ESBUser { get; set; }
        /// <summary>
        /// Пароль пользователя (IMZ_BUS).
        /// </summary>
        public string ESBPass { get; set; }

        /// <summary>
        /// Имя точки обмена для отправки сообщений.
        /// </summary>
        public string ESBExchange { get; set; }

        /// <summary>
        /// Ключ маршрута.
        /// </summary>
        public string ESBRoutingKey { get; set; }

        /// <summary>
        /// Разрешить публикацию сообщений в очередь.
        /// </summary>
        public bool ESBPublish { get; set; }

        /// <summary>
        /// Имя сервиса, источника сообщений.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// URL адрес web API.
        /// </summary>
        public string APIHost { get; set; }

        /// <summary>
        /// Порт web API.
        /// </summary>
        public int APIPort { get; set; }

        /// <summary>
        /// Тип подключения.
        /// </summary>
        public string APIScheme { get; set; }

        /// <summary>
        /// Виртуальный или физический COM-порт с подключенными Параграф PL20.
        /// </summary>
        public string COMPort { get; set; }
        #endregion

        /// <summary>
        /// Получить настройки приложения из файла Configuration.json.
        /// </summary>
        /// <param name="confFile">Имя конфигурационного файла.</param>
        /// <param name="cncType">Тип оборудования.</param>
        /// <returns>Список настроек оборудования.</returns>
        public static Configuration LoadConfig(string confFile, CNCType cncType)
        {
            if (File.Exists(confFile))
            {
                string configuration = new StreamReader(confFile, System.Text.Encoding.Default).ReadToEnd();
                Configuration tempConfiguration = JsonConvert.DeserializeObject<Configuration>(configuration);

                SetMachineTools(tempConfiguration, cncType);
                return tempConfiguration;
            }
            else
            {
                System.Console.WriteLine($"File '{confFile}' not found!");
                return null;
            }
        }

        /// <summary>
        /// Изменение списка подключенного оборудования на список из web API.
        /// </summary>
        /// <param name="config">Конфигурационный файл.</param>
        /// <remarks>
        /// Если не удалось подключиться к Api, список в конфигурационном файле не изменяется.
        /// </remarks>
        private static void SetMachineTools(Configuration config, CNCType? cncType = null)
        {
            var webApi = new WebApi(config);
            Dictionary<long, MachineTool> apiMachineTools;

            if (webApi.GetMachineTools(cncType, out apiMachineTools))
            {
                if (apiMachineTools.Count > 0)
                {
                    config.MachineTools = apiMachineTools;
                }
            }
        }
    }
}
