using System.Text;
using ItemService.EventProcessing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ItemService.AsyncDataServices;

public class MessageBusSubscriber : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IProcessaEvento _processaEvento;
    private string _nomeDaFila;
    private IConnection _connection;
    private IModel _channel;

    public MessageBusSubscriber(
        IConfiguration configuration,
        IProcessaEvento eventProcessor)
    {
        _configuration = configuration;
        _processaEvento = eventProcessor;
        IniciaRabbitMQ();
    }

    private void IniciaRabbitMQ()
    {
        // docker pull mcr.microsoft.com/dotnet/sdk:6.0
        // docker run -it --rm --name rabbitmq -p 8002:5672 -p 15672:15672 rabbitmq:3.12-management
        _connection = new ConnectionFactory() { HostName = "localhost", Port = 8002 }.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(exchange: "trigger", type: ExchangeType.Fanout);
        _nomeDaFila = _channel.QueueDeclare().QueueName;
        _channel.QueueBind(queue: _nomeDaFila,
            exchange: "trigger",
            routingKey: "");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (ModuleHandle, ea) =>
        {

            var body = ea.Body;
            var mensagem = Encoding.UTF8.GetString(body.ToArray());

            _processaEvento.Processa(mensagem);
        };

        _channel.BasicConsume(queue: _nomeDaFila, autoAck: true, consumer: consumer);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        if (_channel.IsOpen)
        {
            _channel.Close();
            _connection.Close();
        }

        base.Dispose();
    }
}
