// See https://aka.ms/new-console-template for more information

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Google.Apis.Util.Store;
using TestApp;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2;
using Google.Apis.YouTube.v3;
using static Google.Apis.Auth.OAuth2.Web.AuthorizationCodeWebApp;

public class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        Console.WriteLine("==================================================");
        Console.WriteLine("This is a test");
        Console.WriteLine(Environment.GetEnvironmentVariable("TEMP_SECRET"));
        Console.WriteLine(Environment.GetEnvironmentVariable("AUTO_SECRET").Substring(2));
        Task.WaitAll(ExecuteAsync());
        Console.WriteLine("This was a test");
    }

    public static async Task ExecuteAsync()
    {
     

        string authJson = @"{
                ""installed"": {
                ""client_id"": ""573861238622-2qjkd0bq0n8d4ii3gpj1ipun3sk2s2ra.apps.googleusercontent.com"",
                ""project_id"": ""iconic-apricot-351114"",
                ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
                ""token_uri"": ""https://oauth2.googleapis.com/token"",
                ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
                ""client_secret"": ""{CLIENT_SECRET}"",
                ""redirect_uris"": [ ""http://localhost"" ]
              }
            }";

        authJson = authJson.Replace("{CLIENT_SECRET}", Environment.GetEnvironmentVariable("CLIENT_SECRET"));
        byte[] jsonArray = Encoding.ASCII.GetBytes(authJson);

        MemoryStore memStore = new MemoryStore();


        FileDataStore credStore = new FileDataStore("cred");
        MemoryStream jsonStream = new MemoryStream(jsonArray);


        GoogleAuthorizationCodeFlow flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = GoogleClientSecrets.FromStream(jsonStream).Secrets,
            Scopes = new[] { YouTubeService.Scope.YoutubeForceSsl }
        });

        Google.Apis.Auth.OAuth2.Web.AuthorizationCodeWebApp webapp = new Google.Apis.Auth.OAuth2.Web.AuthorizationCodeWebApp(flow, "https://localhost", "");
        AuthResult auth = await webapp.AuthorizeAsync("opensource@aswglobal.com", CancellationToken.None);

        Console.WriteLine(auth.RedirectUri);
        Thread.Sleep(100);
        
        if (Environment.GetEnvironmentVariable("TOKEN_RESPONSE_CODE") != null)
        {
            Console.WriteLine("Response code found.");
            TokenResponse tokenRes = await flow.ExchangeCodeForTokenAsync("opensource@aswglobal.com", Environment.GetEnvironmentVariable("TOKEN_RESPONSE_CODE"), "https://localhost", CancellationToken.None);

            UserCredential cred1 = new UserCredential(flow, "opensource@aswglobal.com", tokenRes);
        }
    }

}
