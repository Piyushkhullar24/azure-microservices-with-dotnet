using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using System.Net.Mime;
using System.Text;
using Wpm.Clinic.DataAccess;
using Wpm.Clinic.ExternalServices;
using static Wpm.Clinic.Controllers.ConsulationController;

namespace Wpm.Clinic.Application
{
    public class ClinicApplicationService(ClinicDBContext clinicDBContext, ManagementService managementService, IConfiguration configuration)
    {
        public async Task<Consulation> Handle(StartConsulationCommand command)
        {
            // Synchronous Communication
            var petInfo = await managementService.GetPetInfo(command.PatientId);

            var newConulation = new Consulation(Guid.NewGuid(), command.PatientId, petInfo.Name, petInfo.Age, DateTime.UtcNow);

            await clinicDBContext.Consulations.AddAsync(newConulation);
            await clinicDBContext.SaveChangesAsync();

            await PublishIntegrationEventAsync(new IntegrationEvent { PatientId = command.PatientId }, configuration["ServiceBus:ConnectionString"], configuration["ServiceBus:Consulation:QueueName"]);

            //SqsPublisher sqsPublisher = new SqsPublisher(configuration);

            //await sqsPublisher.PublishIntegrationEventAsync(new IntegrationEvent { PatientId = command.PatientId });

              return newConulation;

        }

        public async Task<Consulation> GetConsulation(int patientId)
        {
           return clinicDBContext.Consulations.FirstOrDefault((p)=> p.PatientId == patientId);
        }

        private async Task PublishIntegrationEventAsync(IntegrationEvent integrationEvent, string connectionString, string topicName)
        {
            var jsonMessage = JsonConvert.SerializeObject(integrationEvent);
            var body = Encoding.UTF8.GetBytes(jsonMessage);
            var client = new ServiceBusClient(connectionString);
            var sender = client.CreateSender(topicName);
            var message = new ServiceBusMessage
            {
                Body = new BinaryData(body),
                MessageId = new Guid().ToString(),
                ContentType = MediaTypeNames.Application.Json,
                Subject = integrationEvent.GetType().FullName,
            };

            await sender.SendMessageAsync(message);
        }

        public class IntegrationEvent
        {
            public int PatientId { get; set; }
        }
    }
}
