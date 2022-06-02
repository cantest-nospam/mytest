// See https://aka.ms/new-console-template for more information

public class Program
{
    static void Main(string[] args)
    {
      Console.WriteLine("Hello, World!");
      Console.WriteLine("==================================================");
      Console.WriteLine("This is a test");
      Console.WriteLine(Environment.GetEnvironmentVariable("TEMP_SECRET"));
      Task.WaitAll(ExecuteAsync());
      Console.WriteLine("This was a test");
    }
  
    public static async Task ExecuteAsync()
    {
        HttpClient client = new HttpClient();
        client.BaseAddress = new Uri("https://api.github.com");

        client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("TestApp", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", Environment.GetEnvironmentVariable("ACCESS_TOKEN"));

        var response = await client.GetAsync("/repos/cantest-nospam/mytest/actions/secrets/public-key");
            
        var resource = Newtonsoft.Json.Linq.JObject.Parse(response.Content.ReadAsStringAsync().Result);
        foreach (var property in resource.Properties())
        {
          Console.WriteLine("{0} - {1}", property.Name, property.Value);
        }
    }

}


