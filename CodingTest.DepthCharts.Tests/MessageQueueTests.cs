using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Microsoft.Extensions.Configuration;
using CodingTest.DepthCharts.Handlers;
using CodingTest.DepthCharts.Logic;
using CodingTest.DepthCharts.Models;
using Moq;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace CodingTest.DepthCharts.Tests;

public class MessageQueueTests
{
    private readonly RabbitMqReceiver<NflPosition> _receiver;
    private readonly DepthChartService<NflPosition> _service;
    private readonly IMediator _mediator;

    public MessageQueueTests()
    {
        // Set up a real DI container
        var services = new ServiceCollection();

        // Mock IConfiguration for RabbitMQ settings
        var configMock = new Mock<IConfiguration>();
        configMock.SetupGet(c => c["RabbitMQ:Host"]).Returns("localhost");
        configMock.SetupGet(c => c["RabbitMQ:Username"]).Returns("guest");
        configMock.SetupGet(c => c["RabbitMQ:Password"]).Returns("guest");

        // Register real services with explicit handler interfaces
        services.AddSingleton<DepthChartService<NflPosition>>();

        // Explicitly register all IRequestHandler interfaces
        services.AddTransient<DepthChartCommandHandlers<NflPosition>>();
        services.AddTransient<IRequestHandler<AddPlayerToDepthChartCommand<NflPosition>>, DepthChartCommandHandlers<NflPosition>>();
        services.AddTransient<IRequestHandler<RemovePlayerFromDepthChartCommand<NflPosition>>, DepthChartCommandHandlers<NflPosition>>();
        services.AddTransient<IRequestHandler<GetFullDepthChartQuery<NflPosition>, Dictionary<NflPosition, List<PlayerPosition>>>, DepthChartCommandHandlers<NflPosition>>();
        services.AddTransient<IRequestHandler<GetPlayersUnderQuery<NflPosition>, List<PlayerPosition>>, DepthChartCommandHandlers<NflPosition>>();

        // Add MediatR (optional with explicit registrations, but kept for consistency)
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DepthChartTests).Assembly));

        // Register RabbitMqReceiver
        services.AddSingleton(provider => new RabbitMqReceiver<NflPosition>(
            provider.GetService<IMediator>(),
            provider.GetService<DepthChartService<NflPosition>>(),
            configMock.Object));

        var provider = services.BuildServiceProvider();

        // Resolve instances
        _receiver = provider.GetRequiredService<RabbitMqReceiver<NflPosition>>();
        _service = provider.GetRequiredService<DepthChartService<NflPosition>>();
        _mediator = provider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task ProcessMessageAsync_AddPlayer_AddsPlayer()
    {
        // Arrange
        var json = JsonConvert.SerializeObject(new { type = "add_player", playerId = 1, name = "Bob" });

        // Act// Act
        var sw = new StringWriter();
        Console.SetOut(sw);
        await _receiver.ProcessMessageAsync(json);
        var output = sw.ToString().Trim();
        sw.Dispose();

        // Assert
        var player = _service.AddOrGetPlayer("Bob"); // Should return existing
        Assert.Equal(1, player.PlayerId);
        Assert.Equal("Bob", player.Name);
    }

    [Fact]
    public async Task ProcessMessageAsync_Add_AddsToDepthChart()
    {
        // Arrange
        await _receiver.ProcessMessageAsync(JsonConvert.SerializeObject(new { type = "add_player", playerId = 1, name = "Bob" }));
        var json = JsonConvert.SerializeObject(new { type = "add", playerId = 1, name = "Bob", position = "QB", depth = 0 });

        // Act
        await _receiver.ProcessMessageAsync(json);

        // Assert
        var chart = _service.GetFullDepthChart();
        Assert.Single(chart[NflPosition.QB]);
        Assert.Equal(1, chart[NflPosition.QB][0].PlayerId);
        Assert.Equal(0, chart[NflPosition.QB][0].PositionDepth);
    }

    [Fact]
    public async Task ProcessMessageAsync_GetFull_ReturnsChart()
    {
        // Arrange
        await _receiver.ProcessMessageAsync(JsonConvert.SerializeObject(new { type = "add_player", playerId = 1, name = "Bob" }));
        await _receiver.ProcessMessageAsync(JsonConvert.SerializeObject(new { type = "add", playerId = 1, name = "Bob", position = "QB", depth = 0 }));
        var json = JsonConvert.SerializeObject(new { type = "get_full" });

        // Act
        using var sw = new StringWriter();
        Console.SetOut(sw);
        await _receiver.ProcessMessageAsync(json);
        var output = sw.ToString().Trim();

        // Assert
        var chart = _service.GetFullDepthChart();
        var expectedOutput = "Depth Chart:\nQB: [1]";
        Assert.Equal(expectedOutput, output);
    }

    [Fact]
    public async Task ProcessMessageAsync_InvalidJson_HandlesException()
    {
        // Arrange
        var json = "invalid json";

        // Act
        using var sw = new StringWriter();
        Console.SetOut(sw);
        await _receiver.ProcessMessageAsync(json);
        var output = sw.ToString().Trim();

        // Assert
        Assert.Contains("Error processing message", output);
    }

    [Fact]
    public async Task ProcessMessageAsync_MissingType_DoesNothing()
    {
        // Arrange
        var json = JsonConvert.SerializeObject(new { playerId = 1, name = "Bob" }); // No type

        // Act
        await _receiver.ProcessMessageAsync(json);

        // Assert
        var chart = _service.GetFullDepthChart();
        Assert.All(chart.Values, list => Assert.Empty(list)); // No changes to depth chart
    }
}
