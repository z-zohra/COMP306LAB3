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
        private readonly DynamoDBHelper _dynamoDbHelper;

        public ApplicationController(IConfiguration configuration, DynamoDBHelper dynamoDbHelper)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
            _dynamoDbHelper = dynamoDbHelper;
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
      
        // Login Action
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
                    //int count = (int)cmd.ExecuteScalar();

                    //if (count == 1)
                    var email = cmd.ExecuteScalar()?.ToString();

                    if (!string.IsNullOrEmpty(email))
                    {
                        // Successful login 
                        return RedirectToAction("DisplayUserMovies", new { email = email });

                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid login attempt.");
                    }
                }
            }

            return View("~/Views/Home/Login.cshtml", model);
        }
        [HttpGet]
        public IActionResult Home()
        {
            return View("~/Views/Home/Home.cshtml");
        }

        public IActionResult DisplayUserMovies(string email)
        {
            var movies = _dynamoDbHelper.GetMoviesAsync(email).Result;
            return View("~/Views/Home/Home.cshtml", movies);
        }
        //public IActionResult DynamoDBConnection()
        //{
        //    var movies = _dynamoDbHelper.GetMoviesAsync().Result; // List movies from DynamoDB
        //    // Pass movies to the view for display
        //    return View(movies);
        //}

    }
}
