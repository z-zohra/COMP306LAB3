using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Configuration;
using _Zannat_Mirza__Lab3.Models;

namespace _Zannat_Mirza__Lab3
{
    public class DynamoDBHelper
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;

        public DynamoDBHelper(IConfiguration configuration)
        {
            var awsOptions = configuration.GetAWSOptions();
            _dynamoDbClient = awsOptions.CreateServiceClient<IAmazonDynamoDB>();
        }

        // Method to get items from a DynamoDB table

        public async Task<List<Movie>> GetMoviesAsync(string email)
        {
            var request = new ScanRequest
            {
                TableName = "MovieDB",
                FilterExpression = "UserID = :email",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":email", new AttributeValue { S = email } }
        }
            };

            var response = await _dynamoDbClient.ScanAsync(request);

            var movies = response.Items.Select(item => new Movie
            {
                MovieID = item["MovieID"].S,
                Title = item["Title"].S,
                Genre = item["Genre"].S,
                AverageRating = item.ContainsKey("AverageRating") ? float.Parse(item["AverageRating"].N) : 0,
                ReleaseDate = item.ContainsKey("ReleaseDate") ? DateTime.Parse(item["ReleaseDate"].S) : (DateTime?)null,
                Directors = item.ContainsKey("Directors") ? item["Directors"].L.Select(d => d.S).ToList() : new List<string>(),
                Comments = item.ContainsKey("Comments") ? item["Comments"].M.ToDictionary(
                    commentKey => commentKey.Key, // UID003, UID004
                    commentValue => new Comment
                    {
                        Value = commentValue.Value.M["Comment"].S,
                        Rating = float.Parse(commentValue.Value.M["Rating"].N),
                        Timestamp = DateTime.Parse(commentValue.Value.M["Timestamp"].S)
                    }
                ) : new Dictionary<string, Comment>(),
                Metadata = item.ContainsKey("Metadata") ? new Metadata
                {
                    Country = item["Metadata"].M["Country"].S,
                    Duration = int.Parse(item["Metadata"].M["Duration"].N),
                    Language = item["Metadata"].M["Language"].S
                } : null,
                RatingCount = item.ContainsKey("RatingCount") ? int.Parse(item["RatingCount"].N) : 0
            }).ToList();

            return movies;
        }

        //public async Task<List<Dictionary<string, AttributeValue>>> GetMoviesAsync(string email)
        //{
        //    var request = new ScanRequest
        //    {
        //        TableName = "MovieDB",
        //        FilterExpression = "UserID = :email",
        //        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
        //{
        //    { ":email", new AttributeValue { S = email } }
        //}
        //    };

        //    var response = await _dynamoDbClient.ScanAsync(request);
        //    return response.Items;
        //}



        // Method to insert data into DynamoDB table
        public async Task AddMovieAsync(string email, string movieId, string title, string genre)
        {
            var request = new PutItemRequest
            {
                TableName = "MovieDB",
                Item = new Dictionary<string, AttributeValue>
        {
            { "UserID", new AttributeValue { S = email } }, // Store email as UserID
            { "MovieID", new AttributeValue { S = movieId } },
            { "Title", new AttributeValue { S = title } },
            { "Genre", new AttributeValue { S = genre } }
        }
            };

            await _dynamoDbClient.PutItemAsync(request);
        }
    }
}
