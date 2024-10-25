﻿using Amazon.DynamoDBv2.Model;
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
                ReleaseDate = item.ContainsKey("ReleaseDate") ? DateTime.Parse(item["ReleaseDate"].S) : (DateTime?)null // Handle optional ReleaseDate

            }).ToList();

            return movies;
        }

        // Method to insert data into DynamoDB table
        public async Task AddMovieAsync(string email, string movieId, string title, string genre)
        {
            var request = new PutItemRequest
            {
                TableName = "MovieDB",
                Item = new Dictionary<string, AttributeValue>
        {
            { "UserID", new AttributeValue { S = email } }, 
            { "MovieID", new AttributeValue { S = movieId } },
            { "Title", new AttributeValue { S = title } },
            { "Genre", new AttributeValue { S = genre } }
        }
            };

            await _dynamoDbClient.PutItemAsync(request);
        }
    }
}
