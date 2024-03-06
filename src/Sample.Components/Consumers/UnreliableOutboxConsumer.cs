using MassTransit;
using Microsoft.Extensions.Logging;
using Sample.Components.Contracts;
using Response = Sample.Components.Contracts.Response;

namespace Sample.Components.Consumers;

public class UnreliableOutboxConsumer :
    IConsumer<RegistrationSubmitted>,
    IConsumer<Request>,
    IConsumer<Response>
{
    readonly ILogger<UnreliableOutboxConsumer> _logger;

    public UnreliableOutboxConsumer(ILogger<UnreliableOutboxConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RegistrationSubmitted> context)
    {
        await context.Publish(
            new Request(context.Message.Payment),
            ((Action<PublishContext>)(publishContext =>
            {
                publishContext.RequestId = Guid.NewGuid();
            })).ToPipe());
    }

    public async Task Consume(ConsumeContext<Request> context)
    {
        _logger.LogInformation("---> REQUEST RECEIVED <---");

        await Task.Delay(100);

        //context.Respond(new Response());

        async Task WaitRespond(Response response)
        {
            if (context.Message.Payment % 2 == 0) await Task.Delay(100);
            context.Respond(response);
        }
        context.AddConsumeTask(WaitRespond(new Response()));
    }

    public Task Consume(ConsumeContext<Response> context)
    {
        _logger.LogInformation("---> RESPONSE RECEIVED <---");
        return Task.CompletedTask;
    }
}

public class UnreliableOutboxConsumerDefinition :
    ConsumerDefinition<UnreliableOutboxConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<UnreliableOutboxConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseEntityFrameworkOutbox<RegistrationDbContext>(context);
    }
}