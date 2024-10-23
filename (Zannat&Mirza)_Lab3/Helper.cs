using Amazon.DynamoDBv2;
using Amazon.S3;

namespace _Zannat_Mirza__Lab3
{

    public class Helper
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IAmazonS3 _s3Client;

        public Helper(IConfiguration configuration)
        {
            // Initialize AWS clients using AWS options from configuration
            var awsOptions = configuration.GetAWSOptions();
            _dynamoDbClient = awsOptions.CreateServiceClient<IAmazonDynamoDB>();
            _s3Client = awsOptions.CreateServiceClient<IAmazonS3>();
        }

        // Expose the DynamoDB client
        public IAmazonDynamoDB GetDynamoDbClient()
        {
            return _dynamoDbClient;
        }

        // Expose the S3 client
        public IAmazonS3 GetS3Client()
        {
            return _s3Client;
        }
    }
}
