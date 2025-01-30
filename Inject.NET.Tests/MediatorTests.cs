using Inject.NET.Attributes;
using MediatR;

namespace Inject.NET.Tests;

public partial class MediatorTests
{
    [Test]
    public async Task Test()
    {
        var serviceProvider = await MediatorServiceProvider.BuildAsync();
        
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        
        var response = await mediator.Send(new Ping { Message = "Ping" });
        
        await Assert.That(response.Message).IsEqualTo("Ping Pong");
    }

    [ServiceProvider]
    [Singleton<IMediator, Mediator>]
    [Scoped(typeof(IRequestHandler<,>), typeof(PingHandler))]
    [Scoped(typeof(INotificationHandler<>), typeof(PingedHandler))]
    [Scoped(typeof(INotificationHandler<>), typeof(PingedAlsoHandler))]
    public partial class MediatorServiceProvider;

    public class Ping : IRequest<Pong>
    {
        public string Message { get; set; }
    }

    public class Pong
    {
        public string Message { get; set; }
    }

    public class Pinged : INotification
    {
    }

    public class PingHandler : IRequestHandler<Ping, Pong>
    {
        /* Impl */
        public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Pong { Message = request.Message + " Pong" });
        }
    }

    public class PingedHandler : INotificationHandler<Pinged>
    {
        /* Impl */
        public Task Handle(Pinged notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public class PingedAlsoHandler : INotificationHandler<Pinged>
    {
        /* Impl */
        public Task Handle(Pinged notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public class GenericHandler : INotificationHandler<INotification>
    {
        /* Impl */
        public Task Handle(INotification notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}