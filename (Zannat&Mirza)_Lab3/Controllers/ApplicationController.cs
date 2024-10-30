using _Zannat_Mirza__Lab3.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;


namespace _Zannat_Mirza__Lab3.Controllers
{
    public class ApplicationController : Controller
    {
        private readonly string connectionString;
        private readonly DynamoDBService _dynamoDbHelper;
        private readonly S3Service _s3Service;

        // Initialize helper classes and configure connection string 
        public ApplicationController(IConfiguration configuration, DynamoDBService dynamoDbHelper, S3Service s3Service)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
            _dynamoDbHelper = dynamoDbHelper;
            _s3Service = s3Service;
        }

        // Register GET Action
        [HttpGet]
        public IActionResult Register()
        {
            return View("~/Views/Home/Register.cshtml");
        }

        // Register POST Action
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "INSERT INTO Users (FullName, Email, Password, CreatedDate) VALUES (@FullName, @Email, @Password, GETDATE())";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@FullName", model.FullName);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Password", model.Password);
                    cmd.ExecuteNonQuery();
                }

                return RedirectToAction("Login");
            }

            return View("~/Views/Home/Register.cshtml", model);
        }

        // Login GET Action
        [HttpGet]
        public IActionResult Login()
        {
            return View("~/Views/Home/Login.cshtml");
        }

        // Login POST Action
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT COUNT(1) FROM Users WHERE Email=@Email AND Password=@Password";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Password", model.Password);


                    //if (count == 1)
                    var email = cmd.ExecuteScalar()?.ToString();

                    if (!string.IsNullOrEmpty(email))
                    {
                        // Set the session with the user's email
                        HttpContext.Session.SetString("Email", model.Email); // Store the email in session
                        return RedirectToAction("Home");
                    }

                    else
                    {
                        ModelState.AddModelError("", "Invalid login attempt.");
                    }
                }
            }

            return View("~/Views/Home/Login.cshtml", model);
        }
        // Home GET Action
        [HttpGet]
        public async Task<IActionResult> Home()
        {
            // List all movie files from the S3 bucket
            var movieKeys = await _s3Service.ListMoviesAsync("movie4lab3");

            // Generate pre-signed URLs for each movie key
           var movieUrls = movieKeys.Select(key => _s3Service.GeneratePreSignedUrl("movie4lab3", key)).ToList();

            // Fetch movie metadata from DynamoDB 
            var moviesMetadata = await _dynamoDbHelper.GetMoviesAsync();

            // Combine the S3 movie keys and metadata 
            var movies = moviesMetadata.Select(movie => new Movie
            {
                Title = movie.Title,
                Genre = movie.Genre,
                ReleaseDate = movie.ReleaseDate,
                AverageRating = movie.AverageRating,
                MovieID = movie.MovieID,
                PreSignedUrl = _s3Service.GeneratePreSignedUrl("movie4lab3", movieKeys.FirstOrDefault(key => key.Contains(movie.MovieID)) ?? movie.MovieID)
 
            }).ToList();
            

            return View("~/Views/Home/Home.cshtml", movies);  // Pass the movies to the view

        }
       
        // Delete Movie POST Action
        [HttpPost]
        public async Task<IActionResult> DeleteMovie(string movieId)
        {
            // Get the logged-in user email
            var loggedInUserEmail = HttpContext.Session.GetString("Email");

            if (string.IsNullOrEmpty(loggedInUserEmail))
            {
                return Unauthorized("User not logged in.");
            }

            // Fetch movie details by MovieID
            var movie = await _dynamoDbHelper.GetMovieByIdAsync(movieId);

            if (movie == null)
            {
                return NotFound("Movie not found.");
            }

            // Check if the logged-in user is authorized to delete
            if (movie.UserID != loggedInUserEmail)
            {
                return Unauthorized("User not authorized to delete this movie.");
            }

            // Proceed with deletion if authorized

            // Delete movie file from S3
            var BucketName = "movie4lab3";  
            var Key = $"{movie.MovieID}.mp4";
            await _s3Service.DeleteMovieAsyncS3(BucketName, Key);

            //Delete document from dynamodb using movieid
            await _dynamoDbHelper.DeleteMovieAsyncDynamoDB(movieId);
            return RedirectToAction("Home");
        }

        // Filter Movie based on rating/genre GET Action
        [HttpGet]
        public async Task<IActionResult> FilterMovies(string genre, float? minRating)
        {
            List<Movie> movies;

            if (!string.IsNullOrEmpty(genre))
            {
                movies = await _dynamoDbHelper.ListMoviesByGenre(genre);
            }
            else if (minRating.HasValue)
            {
                movies = await _dynamoDbHelper.ListMoviesByRating(minRating.Value);
            }
            else
            {
                movies = await _dynamoDbHelper.GetMoviesAsync();
            }

            return View("~/Views/Home/Home.cshtml", movies);
        }

    }

}
