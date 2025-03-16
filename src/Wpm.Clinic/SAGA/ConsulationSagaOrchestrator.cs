using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System.Net.Http.Json;
using Wpm.Clinic.DataAccess;
using Wpm.Clinic.ExternalServices;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static Wpm.Clinic.Application.ClinicApplicationService;

namespace Wpm.Clinic.SAGA
{
    public class ConsulationSagaOrchestrator
    {
        private readonly ClinicDBContext _clinicDBContext;
        private readonly ServiceBusClient _client;
        private readonly ILogger<ConsulationSagaOrchestrator> _logger;
        private readonly ManagementService _managementService;

        public ConsulationSagaOrchestrator(ClinicDBContext clinicDBContext, ManagementService managementService, IConfiguration configuration, ILogger<ConsulationSagaOrchestrator> logger)
        {
            _clinicDBContext = clinicDBContext;
            _client = new ServiceBusClient(configuration["ServiceBus:ConnectionString"]);
            _logger = logger;
            _managementService = managementService;
        }

        public async Task StartSaga(int patientId)
        {
            _logger.LogInformation($"Starting Saga for Patient {patientId}");

            // Step 1: Fetch Pet Info
            var petInfo = await _managementService.GetPetInfo(patientId);

            if (petInfo == null)
            {
                _logger.LogError($"Failed to fetch pet info for Patient {patientId}. Cancelling saga.");
                return;
            }

            // Step 2: Create Consultation
            var consultation = new Consulation(Guid.NewGuid(), patientId, petInfo.Name, petInfo.Age, DateTime.UtcNow);
            await _clinicDBContext.Consulations.AddAsync(consultation);
            await _clinicDBContext.SaveChangesAsync();

            // Step 3: Publish event to notify Payment Service
            await PublishEventAsync(new IntegrationEvent { PatientId = patientId }, "ConsultationStarted");
        }


        private async Task PublishEventAsync(IntegrationEvent integrationEvent, string eventType)
        {
            var sender = _client.CreateSender("saga-topic");
            var message = new ServiceBusMessage(JsonConvert.SerializeObject(integrationEvent))
            {
                Subject = eventType,
                ContentType = "application/json"
            };

            await sender.SendMessageAsync(message);
            _logger.LogInformation($"Published {eventType} event for Patient {integrationEvent.PatientId}");
        }
    }
}
