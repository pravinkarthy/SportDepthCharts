using CodingTest.DepthCharts.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodingTest.DepthCharts.Logic;

public class DepthChartService<TPosition> where TPosition : struct, Enum
{
    private readonly Dictionary<TPosition, List<PlayerPosition>> _depthCharts = new();
    private readonly List<Player> _players = new()
    {
        new Player(1, "Bob"),
        new Player(2, "Alice"),
        new Player(3, "Charlie")
    }; // Centralized list of players

    public DepthChartService()
    {
        foreach (TPosition position in Enum.GetValues(typeof(TPosition)))
        {
            _depthCharts[position] = new List<PlayerPosition>();
        }
    }

    public Player AddOrGetPlayer(string name)
    {
        var existingPlayer = _players.FirstOrDefault(p => p.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        if (existingPlayer != null)
        {
            return existingPlayer;
        }

        var newPlayer = new Player(_players.Count + 1, name);
        _players.Add(newPlayer);
        return newPlayer;
    }

    public void AddPlayerPosition(PlayerPosition playerPosition, TPosition position)
    {
        var chart = _depthCharts[position];
        // Check if player exists in the player list
        if (!_players.Any(p => p.PlayerId == playerPosition.PlayerId))
        {
            throw new InvalidOperationException($"Player '{playerPosition.PlayerId}' does not exist. Add the player first.");
        }

        // Remove any existing entry for this player in this position to avoid duplicates
        chart.RemoveAll(p => p.PlayerId == playerPosition.PlayerId);

        if (playerPosition.PositionDepth.HasValue && playerPosition.PositionDepth.Value >= 0 
            && playerPosition.PositionDepth.Value <= chart.Count)
        {
            chart.Insert(playerPosition.PositionDepth.Value, playerPosition);
        }
        else
        {
            chart.Add(playerPosition);
        }
    }

    // Remove a player from the depth chart
    public void RemovePlayerPosition(string playerName, TPosition position)
    {
        var player = _players.FirstOrDefault(p => p.Name.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));
        var chart = _depthCharts[position];

        chart.RemoveAll(p => p.PlayerId == player?.PlayerId);
    }

    // Get the full depth chart
    public Dictionary<TPosition, List<PlayerPosition>> GetFullDepthChart()
    {
        return new Dictionary<TPosition, List<PlayerPosition>>(_depthCharts);
    }

    // Get players under a specific player in the depth chart
    public List<PlayerPosition> GetPlayersUnder(string playerName, TPosition position)
    {
        var player = _players.FirstOrDefault(p => p.Name.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));
        var chart = _depthCharts[position];
        int index = chart.FindIndex(p => p.PlayerId == player?.PlayerId);
        return index >= 0 ? chart.Skip(index + 1).ToList() : new List<PlayerPosition>();
    }
}
