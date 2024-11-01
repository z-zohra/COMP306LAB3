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

        public ApplicationController(IConfiguration configuration, DynamoDBService dynamoDbHelper, S3Service s3Service)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
            _dynamoDbHelper = dynamoDbHelper;
            _s3Service = s3Service;
        }

        // Registration GET Action
        [HttpGet]
        public IActionResult Register()
        {
            return View("~/Views/Home/Register.cshtml");
        }

        // Registration POST Action
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

                    var count = Convert.ToInt32(cmd.ExecuteScalar());

                    if (count == 1)
                    {
                        // Successful login
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

        // Home Action
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
                MovieID = movie.MovieID,
                PreSignedUrl = movieUrls.FirstOrDefault(url => url.Contains(movie.MovieID)) // Map URL to each movie based on MovieID
            }).ToList();

            // Pass the list of Movie objects (with metadata and URLs) to the view
            return View("~/Views/Home/Home.cshtml", movies);
        }

        public async Task<IActionResult> FilterMovies(string genre, float? minRating)
        {
            List<Movie> movies;

            if (!string.IsNullOrEmpty(genre) && minRating.HasValue)
            {
                // Filter by both genre and rating
                movies = (await _dynamoDbHelper.ListMoviesByGenre(genre))
                            .Where(m => m.AverageRating >= minRating)
                            .ToList();
            }
            else if (!string.IsNullOrEmpty(genre))
            {
                // Filter only by genre
                movies = await _dynamoDbHelper.ListMoviesByGenre(genre);
            }
            else if (minRating.HasValue)
            {
                // Filter only by rating
                movies = await _dynamoDbHelper.ListMoviesByRating(minRating.Value);
            }
            else
            {
                // No filter applied, return all movies
                movies = await _dynamoDbHelper.GetMoviesAsync();
            }

            // Pass filtered movies list to the view
            return View("Home", movies);
        }


    }
}

