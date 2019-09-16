using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace Paragraph.Service
{
    /// <summary>
    /// Объект для взаимодействия с Web Api.
    /// </summary>
    public class WebApi
    {
        private static readonly HttpClient HttpClient;
        private UriBuilder Url;

        static WebApi()
        {
            HttpClient = new HttpClient();
            HttpClient.Timeout = new TimeSpan(hours: 0, minutes: 0, seconds: 5);
        }

        public WebApi(Configuration config)
        {
            Url = new UriBuilder();
            Url.Scheme = config.APIScheme;
            Url.Host = config.APIHost;
            Url.Port = config.APIPort;
        }

        /// <summary>
        /// Получить словарь подключенного оборудования.
        /// </summary>
        /// <param name="cncType">Тип оборудования.</param>
        /// <param name="dictMachineTool">Словарь с оборудованием в мониторинге.</param>
        /// <remarks>Ключом словаря ИД оборудования.</remarks>
        /// <returns>True - успешное выполнение запроса.</returns>
        public bool GetMachineTools(CNCType? cncType, out Dictionary<long, MachineTool> dictMachineTool)
        {
            var isOk = false;
            dictMachineTool = new Dictionary<long, MachineTool>();

            Url.Path = "/api/Monitoring/spr/machines";
            if (cncType != null)
            {
                Url.Query = $"type={(int)cncType.Value}";
            }

            // Загрузка оборудования из web API
            using (var response = HttpClient.GetAsync(Url.ToString()).Result)
            {
                isOk = response.StatusCode == System.Net.HttpStatusCode.OK;
                if (isOk)
                {
                    var resp = response.Content.ReadAsStringAsync();
                    var tools = JArray.Parse(resp.Result);
                    foreach (var tool in tools)
                    {
                        if (!dictMachineTool.ContainsKey((long)tool["id_machine"]))
                        {
                            dictMachineTool.Add((long)tool["id_machine"], new MachineTool
                            {
                                CNCHost = tool["host"].ToString().Trim(),
                                CNCPort = (int)tool["port"],
                                CNCType = (CNCType)(int)tool["type"]
                            });
                        }
                    }
                }
            }

            return isOk;
        }
    }
}
