using CodingTest.DepthCharts.Logic;
using CodingTest.DepthCharts.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CodingTest.DepthCharts.Handlers;

public class DepthChartCommandHandlers<TPosition> :
    IRequestHandler<AddPlayerToDepthChartCommand<TPosition>>,
    IRequestHandler<RemovePlayerFromDepthChartCommand<TPosition>>,
    IRequestHandler<GetFullDepthChartQuery<TPosition>, Dictionary<TPosition, List<PlayerPosition>>>,
    IRequestHandler<GetPlayersUnderQuery<TPosition>, List<PlayerPosition>>
    where TPosition : struct, Enum
{
    private readonly DepthChartService<TPosition> _service;

    public DepthChartCommandHandlers(DepthChartService<TPosition> service)
    {
        _service = service;
    }

    public Task<Dictionary<TPosition, List<PlayerPosition>>> Handle(GetFullDepthChartQuery<TPosition> request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_service.GetFullDepthChart());
    }

    public Task<List<PlayerPosition>> Handle(GetPlayersUnderQuery<TPosition> request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_service.GetPlayersUnder(request.PlayerName, request.Position));
    }

    public Task Handle(AddPlayerToDepthChartCommand<TPosition> request, CancellationToken cancellationToken)
    {
        _service.AddPlayerPosition(request.PlayerPosition, request.Position);
        return Task.FromResult(Unit.Value);
    }

    public Task Handle(RemovePlayerFromDepthChartCommand<TPosition> request, CancellationToken cancellationToken)
    {
        _service.RemovePlayerPosition(request.PlayerName, request.Position);
        return Task.FromResult(Unit.Value);
    }
}
