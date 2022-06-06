// See https://aka.ms/new-console-template for more information

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

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
        string key = (string)resource["key"];
        string key_id = (string)resource["key_id"];

        var secretValue = System.Text.Encoding.UTF8.GetBytes("mySecret");
        var publicKey = Convert.FromBase64String(key);

        var sealedPublicKeyBox = Sodium.SealedPublicKeyBox.Create(secretValue, publicKey);

        Console.WriteLine(Convert.ToBase64String(sealedPublicKeyBox));

        dynamic secret = new JObject();
        secret.encrypted_value = Convert.ToBase64String(sealedPublicKeyBox);
        secret.key_id = key_id;

        using (HttpContent httpContent = new StringContent(secret.ToString(Formatting.None)))
        {
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage response2 = client.PutAsync("/repos/cantest-nospam/mytest/actions/secrets/AUTO_SECRET", httpContent).Result;
            Console.WriteLine(response2.StatusCode);
        }
    }

}

