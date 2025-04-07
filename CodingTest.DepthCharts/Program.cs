using CodingTest.DepthCharts.Handlers;
using CodingTest.DepthCharts.Logic;
using CodingTest.DepthCharts.Models;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodingTest.DepthCharts;

class Program
{
    static async Task Main()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Define sport configurations
        var sportConfigs = new List<object>
        {
            new SportConfiguration<NflPosition>("nfl_depth_chart_queue"),
            new SportConfiguration<MlbPosition>("mlb_depth_chart_queue")
            // Add more sports here as needed, e.g., new SportConfiguration<NbaPosition>("nba_depth_chart_queue")
        };

        // Register services for all sports
        foreach (var config in sportConfigs)
        {
            var positionType = config.GetType().GetProperty("PositionType").GetValue(config) as Type;
            var methodInfo = typeof(Program).GetMethod(nameof(RegisterSportServices), BindingFlags.Static | BindingFlags.NonPublic);
            var genericMethod = methodInfo.MakeGenericMethod(positionType);
            genericMethod.Invoke(null, new object[] { services, config, configuration });
        }

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

        var provider = services.BuildServiceProvider();

        // Start listeners for all sports
        var listenerTasks = new List<Task>();
        foreach (var config in sportConfigs)
        {
            var positionType = config.GetType().GetProperty("PositionType").GetValue(config) as Type;
            var receiverType = typeof(RabbitMqReceiver<>).MakeGenericType(positionType);
            var receiver = provider.GetService(receiverType) as dynamic;
            listenerTasks.Add(receiver.StartListeningAsync());
        }

        // Interactive loop for sending messages via queuing service
        await RunInteractiveLoop(configuration, "nfl_depth_chart_queue");

        await Task.WhenAll(listenerTasks);
    }
    private static async Task RunInteractiveLoop(IConfiguration configuration, string queueName)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"],
            UserName = configuration["RabbitMQ:Username"],
            Password = configuration["RabbitMQ:Password"]
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        // Declare the queue
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        Console.WriteLine($"Connected to RabbitMQ. Enter JSON messages to send to {queueName} (or type 'exit' to quit):");

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Please enter a valid JSON message.");
                continue;
            }

            if (input.Trim().ToLower() == "exit")
            {
                break;
            }

            try
            {
                // Validate JSON (optional, but helps catch errors early)
                Newtonsoft.Json.JsonConvert.DeserializeObject(input);
                var body = Encoding.UTF8.GetBytes(input);
                await channel.BasicPublishAsync(exchange: "", routingKey: queueName, body: body);
                Console.WriteLine($"Sent to {queueName}: {input}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        Console.WriteLine("Exiting interactive mode...");
    }

    private static void RegisterSportServices<TPosition>(IServiceCollection services, object config, IConfiguration configuration)
        where TPosition : struct, Enum
    {
        if (config is SportConfiguration<TPosition> typedConfig)
        {
            services.AddSingleton<DepthChartService<TPosition>>();

            // Register the handler class once for all its implemented interfaces
            services.AddTransient<DepthChartCommandHandlers<TPosition>>();
            services.AddTransient<IRequestHandler<AddPlayerToDepthChartCommand<TPosition>>, DepthChartCommandHandlers<TPosition>>();
            services.AddTransient<IRequestHandler<RemovePlayerFromDepthChartCommand<TPosition>>, DepthChartCommandHandlers<TPosition>>();
            services.AddTransient<IRequestHandler<GetFullDepthChartQuery<TPosition>, Dictionary<TPosition, List<PlayerPosition>>>, DepthChartCommandHandlers<TPosition>>();
            services.AddTransient<IRequestHandler<GetPlayersUnderQuery<TPosition>, List<PlayerPosition>>, DepthChartCommandHandlers<TPosition>>();

            services.AddSingleton(provider => new RabbitMqReceiver<TPosition>(
                provider.GetService<IMediator>(),
                provider.GetService<DepthChartService<TPosition>>(),
                configuration)
            {
                QueueName = typedConfig.QueueName // Pass queue name dynamically
            });
        }
    }
}
