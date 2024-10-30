using Amazon.DynamoDBv2.DataModel;

namespace _Zannat_Mirza__Lab3.Models
{
    [DynamoDBTable("MovieDB")]
    public class Movie
    
    {
        [DynamoDBHashKey]
        public string MovieID { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public float AverageRating { get; set; }
        public string UserID { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string PreSignedUrl { get; set; }
        public List<string> Directors { get; set; }
        public Dictionary<string, Comment> Comments { get; set; }
        public Metadata Metadata { get; set; }
        public int RatingCount { get; set; }
    }

    public class Comment
    {
        public string Email { get; set; }
        public string Comments { get; set; }
        public float Rating { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class Metadata
    {
        public string Country { get; set; }
        public int Duration { get; set; }
        public string Language { get; set; }
    }
}
