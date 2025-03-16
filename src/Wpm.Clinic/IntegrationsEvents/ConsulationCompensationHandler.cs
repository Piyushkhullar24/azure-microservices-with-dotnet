using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using Wpm.Clinic.DataAccess;
using static Wpm.Clinic.Application.ClinicApplicationService;

namespace Wpm.Clinic.IntegrationsEvents
{
    public class ConsulationCompensationHandler : BackgroundService
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusProcessor _processor;
        private readonly ClinicDBContext _clinicDBContext;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ConsulationCompensationHandler> _logger;

        public ConsulationCompensationHandler(IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<ConsulationCompensationHandler> logger)
        {
            _scopeFactory = scopeFactory; // Use scope factory to get DB context
            _logger = logger;
            _client = new ServiceBusClient(configuration["ServiceBus:ConnectionString"]);
            _processor = _client.CreateProcessor("saga-compensation-queue");

            _processor.ProcessMessageAsync += Processor_MessageAsync;
            _processor.ProcessErrorAsync += Processor_ErrorAsync;
        }

        private async Task Processor_MessageAsync(ProcessMessageEventArgs args)
        {
            var body = args.Message.Body.ToString();
            var eventType = args.Message.Subject;
            var theEvent = JsonConvert.DeserializeObject<IntegrationEvent>(body);

            if (eventType == "ConsultationFailed")
            {
                _logger.LogWarning($"Rolling back Consultation for Patient {theEvent.PatientId}");

                await CompensateSaga(theEvent.PatientId);
            }

            await args.CompleteMessageAsync(args.Message);
        }

        private async Task CompensateSaga(int patientId)
        {
            // Create a new scope for DB context
            using (var scope = _scopeFactory.CreateScope())
            {
                var clinicDBContext = scope.ServiceProvider.GetRequiredService<ClinicDBContext>();

                var consultation = clinicDBContext.Consulations.FirstOrDefault(c => c.PatientId == patientId);
                if (consultation != null)
                {
                    clinicDBContext.Consulations.Remove(consultation);
                    await clinicDBContext.SaveChangesAsync();
                    _logger.LogInformation($"Rolled back Consultation for Patient {patientId}");
                }
            }
        }

        private Task Processor_ErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception.ToString());
            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _processor.StartProcessingAsync(stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _processor.StopProcessingAsync(cancellationToken);
        }
    }
}
