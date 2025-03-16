using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Wpm.Payment.Api.IntegrationEvents
{
    public class AWSConsulationEventHandler : BackgroundService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly ILogger<ConsulationEventHandler> _logger;
        private readonly string _queueUrl;

        public AWSConsulationEventHandler(IConfiguration configuration, ILogger<ConsulationEventHandler> logger)
        {
            _logger = logger;
            _sqsClient = new AmazonSQSClient();
            _queueUrl = configuration["AWS:SQS:Consulation:QueueUrl"]; // AWS SQS Queue URL
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SQS Listener started...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var request = new ReceiveMessageRequest
                    {
                        QueueUrl = _queueUrl,
                        MaxNumberOfMessages = 10, // Adjust based on need
                        WaitTimeSeconds = 10, // Long polling
                        MessageAttributeNames = new List<string> { "All" }
                    };

                    var response = await _sqsClient.ReceiveMessageAsync(request, stoppingToken);

                    foreach (var message in response.Messages)
                    {
                        await ProcessMessageAsync(message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error receiving messages from SQS.");
                }
            }
        }

        private async Task ProcessMessageAsync(Message message)
        {
            try
            {
                var theEvent = JsonConvert.DeserializeObject<IntegrationEvent>(message.Body);
                _logger.LogInformation("Received message: {Message}", message.Body);

                // Delete message after processing to prevent reprocessing
                await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SQS message.");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SQS Listener stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}
