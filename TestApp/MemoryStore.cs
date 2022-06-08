using Google.Apis.Util.Store;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace TestApp
{
    public class MemoryStore : IDataStore
    {
        private const string XdgDataHomeSubdirectory = "google-filedatastore";
        private static readonly Task CompletedTask = Task.FromResult(0);

        readonly string folderPath;
        /// <summary>Gets the full folder path.</summary>
        public string FolderPath { get { return folderPath; } }

        public MemoryStore()
        {
        }

        private string GetHomeDirectory()
        {
            string appData = Environment.GetEnvironmentVariable("APPDATA");
            if (!string.IsNullOrEmpty(appData))
            {
                // This is almost certainly windows.
                // This path must be the same between the desktop FileDataStore and this netstandard FileDataStore.
                return appData;
            }
            string home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
            {
                // This is almost certainly Linux or MacOS.
                // Follow the XDG Base Directory Specification: https://specifications.freedesktop.org/basedir-spec/latest/index.html
                // Store data in subdirectory of $XDG_DATA_HOME if it exists, defaulting to $HOME/.local/share if not set.
                string xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                if (string.IsNullOrEmpty(xdgDataHome))
                {
                    xdgDataHome = Path.Combine(home, ".local", "share");
                }
                return Path.Combine(xdgDataHome, XdgDataHomeSubdirectory);
            }
            throw new PlatformNotSupportedException("Relative FileDataStore paths not supported on this platform.");
        }


        public Task StoreAsync<T>(string keyToStore, T value)
        {
            if (string.IsNullOrEmpty(keyToStore))
            {
                throw new ArgumentException("Key MUST have a value");
            }

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://api.github.com");

            client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("TestApp", "1.0"));
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", Environment.GetEnvironmentVariable("ACCESS_TOKEN"));

            Task<HttpResponseMessage> response = client.GetAsync("/repos/cantest-nospam/mytest/actions/secrets/public-key");
            var resource = Newtonsoft.Json.Linq.JObject.Parse(response.Result.Content.ReadAsStringAsync().Result);
            foreach (var property in resource.Properties())
            {
                Console.WriteLine("{0} - {1}", property.Name, property.Value);
            }
            string key = (string)resource["key"];
            string key_id = (string)resource["key_id"];

            string stringValue = (string)(object)value;

            var secretValue = System.Text.Encoding.UTF8.GetBytes(stringValue);
            var publicKey = Convert.FromBase64String(key);

            var sealedPublicKeyBox = Sodium.SealedPublicKeyBox.Create(secretValue, publicKey);

            Console.WriteLine(Convert.ToBase64String(sealedPublicKeyBox));

            dynamic secret = new JObject();
            secret.encrypted_value = Convert.ToBase64String(sealedPublicKeyBox);
            secret.key_id = key_id;

            using (HttpContent httpContent = new StringContent(secret.ToString(Formatting.None)))
            {
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage response2 = client.PutAsync("/repos/cantest-nospam/mytest/actions/secrets/" + keyToStore.ToUpper(), httpContent).Result;
                Console.WriteLine(response2.StatusCode);
            }
            return CompletedTask;
        }

        /// <summary>
        /// Deletes the given key. It deletes the <see cref="GenerateStoredKey"/> named file in 
        /// <see cref="FolderPath"/>.
        /// </summary>
        /// <param name="key">The key to delete from the data store.</param>
        public Task DeleteAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key MUST have a value");
            }

            var filePath = Path.Combine(folderPath, GenerateStoredKey(key, typeof(T)));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return CompletedTask;
        }

        public Task<T> GetAsync<T>(string key)
        {
            Console.WriteLine("GetAsync: " + typeof(T));
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key MUST have a value");
            }

            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

            return tcs.Task;
        }

        public Task ClearAsync()
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
                Directory.CreateDirectory(folderPath);
            }

            return CompletedTask;
        }

        /// <summary>Creates a unique stored key based on the key and the class type.</summary>
        /// <param name="key">The object key.</param>
        /// <param name="t">The type to store or retrieve.</param>
        public static string GenerateStoredKey(string key, Type t)
        {
            return string.Format("{0}-{1}", t.FullName, key);
        }
    }
}
