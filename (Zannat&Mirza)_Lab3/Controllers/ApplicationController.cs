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

        [HttpPost]
public async Task<IActionResult> AddMovie(Movie model, IFormFile file)
{
    // Always retrieve the list of movies to pass to the view
    List<Movie> movies = await _dynamoDbHelper.GetMoviesAsync();

    // Validate the model state and the file
    if (!ModelState.IsValid || file == null || file.Length == 0)
    {
        // Return the view with the list of movies and the current model state
        return View("~/Views/Home/Home.cshtml", movies);
    }

    try
    {
        // Generate a unique MovieID
        model.MovieID = Guid.NewGuid().ToString();

        // Set the S3 key based on the MovieID for uniqueness
        var s3Key = $"{model.MovieID}.mp4"; // Using MovieID for uniqueness

        // Validate file type
        var validExtensions = new[] { ".mp4", ".mkv", ".avi" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!validExtensions.Contains(extension))
        {
            ModelState.AddModelError("file", "Invalid file type. Please upload a video file.");
            return View("~/Views/Home/Home.cshtml", movies);
        }

        // Save the file to a temporary path
        var tempFilePath = Path.Combine(Path.GetTempPath(), s3Key);
        using (var stream = new FileStream(tempFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Upload the file to S3 and get the pre-signed URL
        await _s3Service.UploadMovieAsyncS3("movie4lab3", s3Key, tempFilePath);
        model.PreSignedUrl = _s3Service.GeneratePreSignedUrl("movie4lab3", s3Key);

        // Save the movie metadata to DynamoDB
        await _dynamoDbHelper.AddMovie(model);

        // Redirect to the Home action
        return RedirectToAction("Home");
    }
    catch (Exception ex)
    {
        // Handle any exceptions and log the error
        ModelState.AddModelError("", "An error occurred while uploading the movie: " + ex.Message);
        return View("~/Views/Home/Home.cshtml", movies);
    }
    finally
    {
        // Clean up the temporary file
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{model.MovieID}.mp4");
        if (System.IO.File.Exists(tempFilePath))
        {
            System.IO.File.Delete(tempFilePath);
        }
    }
}






        // Filter Movie 
        public async Task<IActionResult> FilterMovies(string genre, float? minRating)
        {
            List<Movie> movies;

            if (!string.IsNullOrEmpty(genre) && minRating.HasValue)
            {
                movies = (await _dynamoDbHelper.ListMoviesByGenre(genre))
                            .Where(m => m.AverageRating >= minRating)
                            .ToList();
            }
            else if (!string.IsNullOrEmpty(genre))
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

            // Fetch movie keys from S3 and regenerate pre-signed URLs
            var movieKeys = await _s3Service.ListMoviesAsync("movie4lab3");
            foreach (var movie in movies)
            {
                // Find the correct S3 key that matches the MovieID
                var matchedKey = movieKeys.FirstOrDefault(key => key.Contains(movie.MovieID));
                movie.PreSignedUrl = matchedKey != null
                    ? _s3Service.GeneratePreSignedUrl("movie4lab3", matchedKey)
                    : null; // Set URL to null if no matching key found
            }

            return View("~/Views/Home/Home.cshtml", movies);
        }

    }
}

