
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System.Net.Mime;
using System.Text;
using Wpm.Payment.Api.Services;

namespace Wpm.Payment.Api.IntegrationEvents
{
    public class ConsulationEventHandler : BackgroundService
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusProcessor _processor;
        private readonly ILogger<ConsulationEventHandler> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ConsulationEventHandler(IConfiguration configuration, ILogger<ConsulationEventHandler> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); 
            _scopeFactory = scopeFactory; // Use scope factory to get DB context

            try
            {
                _client = new ServiceBusClient(configuration["ServiceBus:ConnectionString"]);

                _processor = _client.CreateProcessor("saga-topic", new ServiceBusProcessorOptions
                {
                    AutoCompleteMessages = false,
                    MaxConcurrentCalls = 1
                });

                _processor.ProcessMessageAsync += Processor_MessageAsync;
                _processor.ProcessErrorAsync += Processor_ErrorAsync;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error initializing Service Bus Processor: {ex.Message}");
                throw;
            }
        }

        private async Task Processor_MessageAsync(ProcessMessageEventArgs args)
        {
            try
            {
                var body = args.Message.Body.ToString();
                var eventType = args.Message.Subject;
                var theEvent = JsonConvert.DeserializeObject<IntegrationEvent>(body);

                if (eventType == "ConsultationStarted")
                {
                    _logger.LogInformation($"Processing ConsultationStarted event for Patient {theEvent.PatientId}");

                    bool paymentSuccess = false;

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                        if (paymentService != null)
                        {
                            // Call Payment Service
                            paymentSuccess = await paymentService.ProcessPayment(theEvent.PatientId);
                        }
                    }

                    if (!paymentSuccess)
                    {
                        _logger.LogError($"Payment failed for Patient {theEvent.PatientId}. Publishing rollback event.");
                        await PublishEventAsync(new IntegrationEvent { PatientId = theEvent.PatientId }, "ConsultationFailed");
                    }
                }

                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing message: {ex.Message}");
                await args.AbandonMessageAsync(args.Message);
            }
        }

        private async Task PublishEventAsync(IntegrationEvent integrationEvent, string eventType)
        {
            try
            {
                var sender = _client.CreateSender("saga-compensation-queue");
                var message = new ServiceBusMessage(JsonConvert.SerializeObject(integrationEvent))
                {
                    Subject = eventType,
                    ContentType = "application/json"
                };

                await sender.SendMessageAsync(message);
                _logger.LogInformation($"Published {eventType} event for Patient {integrationEvent.PatientId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error publishing event: {ex.Message}");
            }
        }

        private Task Processor_ErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError($"Service Bus Processor Error: {args.Exception}");
            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Service Bus Processor...");
            await _processor.StartProcessingAsync(stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Service Bus Processor...");
            await _processor.StopProcessingAsync(cancellationToken);
        }
    }
}
