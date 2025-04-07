namespace CodingTest.DepthCharts.Tests
{
    using Moq;
    using Xunit;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using MediatR;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using CodingTest.DepthCharts.Logic;
    using CodingTest.DepthCharts.Models;

    public class DepthChartTests
    {
        private readonly DepthChartService<NflPosition> _service;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IConfiguration> _configMock;

        public DepthChartTests()
        {
            _service = new DepthChartService<NflPosition>();
            _mediatorMock = new Mock<IMediator>();
            _configMock = new Mock<IConfiguration>();
            _configMock.SetupGet(c => c["RabbitMQ:Host"]).Returns("localhost");
            _configMock.SetupGet(c => c["RabbitMQ:Username"]).Returns("guest");
            _configMock.SetupGet(c => c["RabbitMQ:Password"]).Returns("guest");
        }

        [Fact]
        public void AddOrGetPlayer_NewPlayer_AddsAndReturnsPlayer()
        {
            // Arrange
            int playerId = 1;
            string name = "Bob";

            // Act
            var player = _service.AddOrGetPlayer(name);

            // Assert
            Assert.Equal(playerId, player.PlayerId);
            Assert.Equal(name, player.Name);
        }

        [Fact]
        public void AddOrGetPlayer_ExistingPlayer_ReturnsExisting()
        {
            // Arrange
            int playerId = 1;
            string name = "Bob";
            var initialPlayer = _service.AddOrGetPlayer(name);

            // Act
            var player = _service.AddOrGetPlayer(name);

            // Assert
            Assert.Same(initialPlayer, player); // Should be the same instance
        }

        [Fact]
        public void AddPlayerToDepthChart_NewEntry_AddsAtDepth()
        {
            // Arrange
            _service.AddOrGetPlayer("Bob");
            var playerPosition = new PlayerPosition(1, "QB", 0);

            // Act
            _service.AddPlayerPosition(playerPosition, NflPosition.QB);
            var chart = _service.GetFullDepthChart();

            // Assert
            Assert.Single(chart[NflPosition.QB]);
            Assert.Equal(1, chart[NflPosition.QB][0].PlayerId);
            Assert.Equal(0, chart[NflPosition.QB][0].PositionDepth);
        }

        [Fact]
        public void AddPlayerToDepthChart_ExistingPlayer_UpdatesPosition()
        {
            // Arrange
            _service.AddOrGetPlayer("Bob");
            _service.AddPlayerPosition(new PlayerPosition(1, "QB", 1), NflPosition.QB);
            var updatedPosition = new PlayerPosition(1, "QB", 0);

            // Act
            _service.AddPlayerPosition(updatedPosition, NflPosition.QB);
            var chart = _service.GetFullDepthChart();

            // Assert
            Assert.Single(chart[NflPosition.QB]);
            Assert.Equal(1, chart[NflPosition.QB][0].PlayerId);
            Assert.Equal(0, chart[NflPosition.QB][0].PositionDepth);
        }

        [Fact]
        public void RemovePlayerFromDepthChart_RemovesPlayer()
        {
            // Arrange
            _service.AddOrGetPlayer("Bob");
            _service.AddPlayerPosition(new PlayerPosition(1, "QB", 0), NflPosition.QB);

            // Act
            _service.RemovePlayerPosition("Bob", NflPosition.QB);
            var chart = _service.GetFullDepthChart();

            // Assert
            Assert.Empty(chart[NflPosition.QB]);
        }

        [Fact]
        public void GetPlayersUnder_ReturnsPlayersBelow()
        {
            // Arrange
            _service.AddOrGetPlayer("Bob");
            _service.AddOrGetPlayer("Alice");
            _service.AddPlayerPosition(new PlayerPosition(1, "QB", 0), NflPosition.QB);
            _service.AddPlayerPosition(new PlayerPosition(2, "QB", 1), NflPosition.QB);

            // Act
            var underPlayers = _service.GetPlayersUnder("Bob", NflPosition.QB);

            // Assert
            Assert.Single(underPlayers);
            Assert.Equal(2, underPlayers[0].PlayerId);
        }
    }
}