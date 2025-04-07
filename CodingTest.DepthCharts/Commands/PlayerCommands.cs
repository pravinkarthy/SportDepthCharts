using MediatR;
using System.Collections.Generic;
using System;
using CodingTest.DepthCharts.Models;
using Newtonsoft.Json;

public record AddPlayerToDepthChartCommand<TPosition>(PlayerPosition PlayerPosition, TPosition Position) : IRequest
    where TPosition : struct, Enum;

public record RemovePlayerFromDepthChartCommand<TPosition>(string PlayerName, TPosition Position) : IRequest
    where TPosition : struct, Enum;

public record GetFullDepthChartQuery<TPosition> : IRequest<Dictionary<TPosition, List<PlayerPosition>>>
    where TPosition : struct, Enum;

public record GetPlayersUnderQuery<TPosition>(string PlayerName, TPosition Position) : IRequest<List<PlayerPosition>>
    where TPosition : struct, Enum;

public abstract record CommandData
{
    [JsonProperty("type")]
    public string Type { get; init; }

    [JsonProperty("playerId")] 
    public int? PlayerId { get; init; }

    [JsonProperty("name")]
    public string Name { get; init; }


    protected CommandData(string type)
    {
        Type = type;
    }
}

public record AddPlayerCommandData : CommandData
{
    [JsonConstructor]
    public AddPlayerCommandData(int playerId, string name) : base("add_player")
    {
        PlayerId = playerId;
        Name = name;
    }
}

public record AddCommandData : CommandData
{
    [JsonProperty("position")]
    public string Position { get; init; }

    [JsonProperty("depth")]
    public int? Depth { get; init; }

    [JsonConstructor]
    public AddCommandData(int playerId, string name, string position, int? depth) : base("add")
    {
        PlayerId = playerId;
        Name = name;
        Position = position;
        Depth = depth;
    }
}

public record RemoveCommandData : CommandData
{
    [JsonProperty("position")]
    public string Position { get; init; }

    [JsonConstructor]
    public RemoveCommandData(string name, string position) : base("remove")
    {
        Name = name;
        Position = position;
    }
}

public record GetUnderCommandData : CommandData
{
    [JsonProperty("position")]
    public string Position { get; init; }

    [JsonConstructor]
    public GetUnderCommandData(string name, string position) : base("get_under")
    {
        Name = name;
        Position = position;
    }
}