using MediatR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Linq;
using CodingTest.DepthCharts.Models;
using CodingTest.DepthCharts.Logic;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
public class RabbitMqReceiver<TPosition> where TPosition : struct, Enum
{
    private readonly IMediator _mediator; 
    private readonly DepthChartService<TPosition> _service;
    private readonly IConfiguration _configuration;
    
    private static readonly JsonSerializerSettings settings = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
    };

    public string QueueName { get; set; } = "depth_chart_queue";

    public RabbitMqReceiver(IMediator mediator, DepthChartService<TPosition> service, IConfiguration configuration)
    {
        _mediator = mediator;
        _service = service;
        _configuration = configuration;
    }

    public async Task StartListeningAsync()
    {
        var factory = new ConnectionFactory() { 
            HostName = _configuration["RabbitMQ:Host"], 
            UserName= _configuration["RabbitMQ:UserName"], 
            Password= _configuration["RabbitMQ:Password"] };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            await ProcessMessageAsync(message);
        };

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: true,
            consumer: consumer);

        Console.WriteLine($"Listening for messages for {typeof(TPosition).Name} on queue {QueueName}...");
        await Task.Delay(Timeout.Infinite);
    }

    public async Task ProcessMessageAsync(string jsonMessage)
    {
        try
        {
            var command = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonMessage);
            if (!command.ContainsKey("type")) return;

            switch (command["type"].ToString())
            {
                case "add_player":
                    var playerData = JsonConvert.DeserializeObject<AddPlayerCommandData>(jsonMessage, settings);
                    _service.AddOrGetPlayer(playerData.Name);

                    Console.WriteLine($"Added Player {playerData.Name} with Id: {playerData.PlayerId.Value}");
                    break;

                case "add":
                    var addData = JsonConvert.DeserializeObject<AddCommandData>(jsonMessage, settings);

                    var player = _service.AddOrGetPlayer(addData.Name); // Ensure player exists

                    var playerPosition = new PlayerPosition(player.PlayerId, addData.Position, addData.Depth);
                    var addPosition = Enum.Parse<TPosition>(addData.Position);

                    await _mediator.Send(new AddPlayerToDepthChartCommand<TPosition>(playerPosition, addPosition));

                    Console.WriteLine($"Added Player {addData.Name} with position '{addData.Position}' to depth: {addData.Depth}");
                    break;

                case "remove":
                    var removeData = JsonConvert.DeserializeObject<RemoveCommandData>(jsonMessage, settings);
                    var removePosition = Enum.Parse<TPosition>(removeData.Position);
                    await _mediator.Send(new RemovePlayerFromDepthChartCommand<TPosition>(removeData.Name, removePosition));

                    Console.WriteLine($"Removed Player {removeData.Name} with position '{removeData.Position}'");
                    break;

                case "get_full":
                    var fullChart = await _mediator.Send(new GetFullDepthChartQuery<TPosition>());
                    Console.WriteLine("\nDepth Chart:\n" + string.Join("\n", fullChart.Where(x => x.Value.Count > 0).Select(kvp => 
                        $"{kvp.Key}: [{string.Join(", ", kvp.Value.Select(p => p.PlayerId))}]"
                    )));
                    break;

                case "get_under":
                    var underData = JsonConvert.DeserializeObject<GetUnderCommandData>(jsonMessage, settings);
                    var underPosition = Enum.Parse<TPosition>(underData.Position);
                    var underPlayers = await _mediator.Send(new GetPlayersUnderQuery<TPosition>(underData.Name, underPosition));
                    Console.WriteLine($"\nPlayers Under {underData.Name} with position '{underPosition}':\n" + JsonConvert.SerializeObject(underPlayers.Select(p => p.PlayerId)));
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
        }
    }
}