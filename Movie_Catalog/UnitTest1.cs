using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;
using Newtonsoft.Json;
using Movie_Catalog.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;


namespace Movie_Catalog
{
    [TestFixture]
    public class Movie_CatalogTests
    {
        private RestClient client;
        private static string lastCreatedMovieId;

        private const string BaseUrl = "http://144.91.123.158:5000";

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiIyY2QyZjJiNC05OTdhLTQyMjQtOTBjNi02ODk2NmM5MzdlZGQiLCJpYXQiOiIwNC8xOC8yMDI2IDA2OjQ3OjQ4IiwiVXNlcklkIjoiZmE5NWI1ZDYtOGIxOC00MTgyLTYyN2UtMDhkZTc2OTcxYWI5IiwiRW1haWwiOiJ0cy5yZWd1bGFyZXhhbUBnbWFpbC5jb20iLCJVc2VyTmFtZSI6IkNlY2lFeGFtIiwiZXhwIjoxNzc2NTE2NDY4LCJpc3MiOiJNb3ZpZUNhdGFsb2dfQXBwX1NvZnRVbmkiLCJhdWQiOiJNb3ZpZUNhdGFsb2dfV2ViQVBJX1NvZnRVbmkifQ.eOZRgpXRn_OciQa3PbpdyvD0owZgGS20ABQWIDsXD_g";
        private const string LoginEmail = "ts.regularexam@gmail.com";
        private const string LoginPassword = "123456";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken),
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempCLient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempCLient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token from the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateMovie_WithRequiredFields_ShouldReturnSuccess() 
        {
            var movieRequest = new MovieDTO
            {
                Title = "Test CECI Movie",
                Description = "description",
                Id = ""
                
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieRequest);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(createResponse, Is.Not.Null);
            Assert.That(createResponse.Movie, Is.Not.Null);
            Assert.That(createResponse.Movie.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(createResponse.Msg, Is.EqualTo("Movie created successfully!"));

            lastCreatedMovieId = createResponse.Movie.Id;
        }

        [Order(2)]
        [Test]
        public void EditExistingMovie_ShouldReturnSuccess()
        {
            Assert.That(lastCreatedMovieId, Is.Not.Null.And.Not.Empty);

            var editRequest = new MovieDTO
            {
                Title = "Edited Test CECI Movie",
                Description = "Edited description"
            };

            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", lastCreatedMovieId);
            request.AddJsonBody(editRequest);

            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse, Is.Not.Null);
            Assert.That(editResponse.Msg, Is.EqualTo("Movie edited successfully!"));
        }

        [Order(3)]
        [Test]
        public void GetAllMovies_ShouldReturnListOfMovies()
        {
            var request = new RestRequest("/api/Catalog/All", Method.Get);
            var response = this.client.Execute(request);

            var movies = JsonSerializer.Deserialize<List<MovieDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(movies, Is.Not.Null);
            Assert.That(movies.Count, Is.GreaterThan(0));
        }
        [Order(4)]
        [Test]
        public void DeleteMovie_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", lastCreatedMovieId);

            var response = this.client.Execute(request);
            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(deleteResponse.Msg, Is.EqualTo("Movie deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void CreateMovie_WithoutRequiredFields_ShouldReturnBadRequest() 
        {
            var movieRequest = new MovieDTO
            {
                Title = "",
                Description = "",
                Id = ""

            };
            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void EditNonExistingMovie_ShouldReturnBadRequest() 
        {
            string nonExistingMovieId = "123";  
            var editRequest = new MovieDTO
            {
                Title = "Edited Non Existing CECI Movie",
                Description = "This is an updated test idea description for a non-existing movie"
            };
            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", nonExistingMovieId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(editResponse.Msg,Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }
        [Order(7)]
        [Test]
        public void DeleteNonExistingMovie_ShouldReturnBadRequest()
        {
            string nonExistingMovieId = "123";

            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", nonExistingMovieId);

            var response = this.client.Execute(request);
            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(deleteResponse, Is.Not.Null);
            Assert.That(deleteResponse.Msg,Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
        }


        [OneTimeTearDown]
        public void Teardown()
        {
            this.client?.Dispose();
        }
    }
}
