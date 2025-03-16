using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using static Wpm.Clinic.Application.ClinicApplicationService;

public class SqsPublisher
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;

    public SqsPublisher(IConfiguration configuration)
    {
        var region = configuration["AWS:Region"];
        _sqsClient = new AmazonSQSClient(RegionEndpoint.GetBySystemName(region));
        _queueUrl = configuration["AWS:SQS:Consulation:QueueUrl"]; // SQS Queue URL from config
    }

    public async Task PublishIntegrationEventAsync(IntegrationEvent integrationEvent)
    {
        var jsonMessage = JsonConvert.SerializeObject(integrationEvent);
        var sendMessageRequest = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = jsonMessage,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { "ContentType", new MessageAttributeValue { DataType = "String", StringValue = MediaTypeNames.Application.Json } },
                { "MessageId", new MessageAttributeValue { DataType = "String", StringValue = Guid.NewGuid().ToString() } },
                { "Subject", new MessageAttributeValue { DataType = "String", StringValue = integrationEvent.GetType().FullName } }
            }
        };

        await _sqsClient.SendMessageAsync(sendMessageRequest);
    }
}
