# Paragraph.Service 

Сервис опроса устройств-регистраторов Параграф PL20.
После запуска определяет список оборудования типа Параграф (WebAPI) и начинает 
его опрос по протоколу MODBUS RTU.
Прочитанные данные отправляются в очередь RabbitMQ.

## Параграф PL20
Описание регистров прибора https://www.kipspb.ru/download/documentation/Network_devices_protocol_MODBUS1.pdf