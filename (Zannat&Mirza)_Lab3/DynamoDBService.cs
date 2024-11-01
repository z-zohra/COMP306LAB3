using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Configuration;
using _Zannat_Mirza__Lab3.Models;
using Amazon.S3;

namespace _Zannat_Mirza__Lab3
{
    public class DynamoDBService
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IAmazonS3 _s3Client;

        public DynamoDBService(Helper helper)
        {
            _dynamoDbClient = helper.GetDynamoDbClient(); // Get DynamoDB client from Helper
            _s3Client = helper.GetS3Client();
        }

        // Method to get items from a DynamoDB table

        public async Task<List<Movie>> GetMoviesAsync()
        {
            var request = new ScanRequest
            {
                TableName = "MovieDB"
            };

            var response = await _dynamoDbClient.ScanAsync(request);

            var movies = response.Items.Select(item => new Movie
            {
                MovieID = item["MovieID"].S,
                Title = item["Title"].S,
                Genre = item["Genre"].S,
                AverageRating = item.ContainsKey("AverageRating") ? float.Parse(item["AverageRating"].N) : 0,
                ReleaseDate = item.ContainsKey("ReleaseDate") ? DateTime.Parse(item["ReleaseDate"].S) : (DateTime?)null // Handle optional ReleaseDate

            }).ToList();

            return movies;
        }

        // Method to insert data into DynamoDB table
        public async Task AddMovie(Movie movie)
        {
            var request = new PutItemRequest
            {
                TableName = "MovieDB",
                Item = new Dictionary<string, AttributeValue>
        {
            { "MovieID", new AttributeValue { S = movie.MovieID } },
            { "Title", new AttributeValue { S = movie.Title } },
            { "Genre", new AttributeValue { S = movie.Genre } },
            { "UserID", new AttributeValue { S = movie.UserID } },
            { "AverageRating", new AttributeValue { N = movie.AverageRating.ToString() } },
            { "RatingCount", new AttributeValue { N = movie.RatingCount.ToString() } },
            { "ReleaseDate", new AttributeValue { S = movie.ReleaseDate?.ToString("yyyy-MM-dd") } },
            //{ "PreSignedUrl", new AttributeValue { S = movie.PreSignedUrl } },
            { "Directors", new AttributeValue { L = movie.Directors.Select(d => new AttributeValue { S = d }).ToList() } },
                { "Metadata", new AttributeValue {
                M = new Dictionary<string, AttributeValue> {
                    { "Country", new AttributeValue { S = movie.Metadata.Country } },
                    { "Duration", new AttributeValue { N = movie.Metadata.Duration.ToString() } },
                    { "Language", new AttributeValue { S = movie.Metadata.Language } }
                }
            } },
            { "Comments", new AttributeValue { M = movie.Comments.ToDictionary(
                kvp => kvp.Key,
                kvp => new AttributeValue {
                    M = new Dictionary<string, AttributeValue> {
                        { "Comment", new AttributeValue { S = kvp.Value.Comments } },
                        { "Rating", new AttributeValue { N = kvp.Value.Rating.ToString() } },
                        { "Timestamp", new AttributeValue { S = kvp.Value.Timestamp.ToString("o") } }
                    }
                })
            }}
        }
            };

            await _dynamoDbClient.PutItemAsync(request);
        }

        //Delete Movie function for DynamoDB
        public async Task DeleteMovieAsyncDynamoDB(string movieId)
        {
            var request = new DeleteItemRequest
            {
                TableName = "MovieDB",
                Key = new Dictionary<string, AttributeValue>
        {
            { "MovieID", new AttributeValue { S = movieId } }
        }
            };

            await _dynamoDbClient.DeleteItemAsync(request);
        }


        // Function to List Movies by Rating using RatingIndex
        public async Task<List<Movie>> ListMoviesByRating(float minRating)
        {
            var request = new QueryRequest
            {
                TableName = "MovieDB",
                IndexName = "RatingIndex",
                KeyConditionExpression = "AverageRating >= :minRating",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
            { ":minRating", new AttributeValue { N = minRating.ToString() } }
        }
            };

            var response = await _dynamoDbClient.QueryAsync(request);
            return response.Items.Select(MapToMovie).ToList();
        }

        //Function to List Movies by Genre using GenreIndex
        public async Task<List<Movie>> ListMoviesByGenre(string genre)
        {
            var request = new QueryRequest
            {
                TableName = "MovieDB",
                IndexName = "GenreIndex",
                KeyConditionExpression = "Genre = :genre",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
            { ":genre", new AttributeValue { S = genre } }
        }
            };

            var response = await _dynamoDbClient.QueryAsync(request);
            return response.Items.Select(MapToMovie).ToList();
        }

        // Helper Fucntion to map DynamoDB items to the Movie model.
        private Movie MapToMovie(Dictionary<string, AttributeValue> item)
        {
            return new Movie
            {
                MovieID = item["MovieID"].S,
                Title = item["Title"].S,
                Genre = item["Genre"].S,
                UserID = item.ContainsKey("UserID") ? item["UserID"].S : null, // Handle missing UserID
                AverageRating = float.Parse(item["AverageRating"].N),
                ReleaseDate = DateTime.Parse(item["ReleaseDate"].S),
                Directors = item["Directors"].L.Select(d => d.S).ToList(),
                RatingCount = int.Parse(item["RatingCount"].N),
                Metadata = new Metadata
                {
                    Country = item["Metadata"].M["Country"].S,
                    Duration = int.Parse(item["Metadata"].M["Duration"].N),
                    Language = item["Metadata"].M["Language"].S
                },
                Comments = item["Comments"].M.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new Comment
                    {
                        Email = kvp.Key,
                        Comments = kvp.Value.M["Comment"].S,
                        Rating = float.Parse(kvp.Value.M["Rating"].N),
                        Timestamp = DateTime.Parse(kvp.Value.M["Timestamp"].S)
                    }
                )
            };
        }

        // Funtion to get movie by id (Helper Fucntion)
        public async Task<Movie> GetMovieByIdAsync(string movieId)
        {
            var request = new GetItemRequest
            {
                TableName = "MovieDB",
                Key = new Dictionary<string, AttributeValue>
        {
            { "MovieID", new AttributeValue { S = movieId } }
        }
            };

            var response = await _dynamoDbClient.GetItemAsync(request);

            if (response.Item == null || !response.Item.Any())
                return null; // No movie found

            // Deserialize the response to a Movie object
            var movie = new Movie
            {
                MovieID = response.Item["MovieID"].S,
                UserID = response.Item.ContainsKey("UserID") ? response.Item["UserID"].S : null,
                
            };

            return movie;
        }





    }
}
