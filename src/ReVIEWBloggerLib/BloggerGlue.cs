using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ReVIEWBlogger
{
    public class BloggerGlue
    {
        public class ConfigFile
        {
            // a bit confusing. all other settings are used by GoogleOAuth2Client, but this one goes into BloggerClient.
            // could be better to separate or organize into structure
            public string BlogId { get; set; }
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }

            [JsonIgnore]
            public string ConfigPath { get; private set; }

            [JsonIgnore]
            public bool FileExists { get; private set; }

            public static ConfigFile Load(string filePath)
            {
                ConfigFile config;
                if (File.Exists(filePath))
                {
                    config = JsonConvert.DeserializeObject<ConfigFile>(File.ReadAllText(filePath, Encoding.UTF8));
                    config.FileExists = true;
                }
                else
                {
                    config = new ConfigFile();
                    config.FileExists = false;
                }
                config.ConfigPath = filePath;
                return config;
            }

            public bool Save()
            {
                try
                {
                    File.WriteAllText(this.ConfigPath, JsonConvert.SerializeObject(this, Formatting.None), Encoding.UTF8);
                    this.FileExists = true;
                }
                catch
                {
                    return false;
                }
                return true;
            }
        }

        class Blog
        {
            public string Uri { get; private set; }
            public string Id { get; set; }
        }

        [DataContract]
        class BlogEntry
        {
            public Blog BlogRef { get; set; }

            [DataMember(Name = "id", EmitDefaultValue = false)]
            public string PostId { get; set; }

            [DataMember(Name = "title", EmitDefaultValue = false)]
            public string Title { get; set; }

            [DataMember(Name = "content", EmitDefaultValue = false)]
            public string Content { get; set; }
        }

        const string CONFIG_FILE_NAME = ".review-blogger-config.json";

        const string AUTH_BASE_URI = "https://accounts.google.com/o/oauth2/";
        const string BLOGGER_BASE_URI = "https://www.googleapis.com/blogger/v3/";
        const string OAUTH2_SCOPE_BLOGGER = "https://www.googleapis.com/auth/blogger";
        const int REVIEW_COMPILE_TIMEOUT_MS = 30000;

        string GetHomeDirectory()
        {
            var homeEnvKey = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "USERPROFILE" : "HOME";
            return Environment.GetEnvironmentVariable(homeEnvKey);
        }

        public string GetAuthUri(ConfigFile config)
        {
            var escapedScope = Uri.EscapeDataString(OAUTH2_SCOPE_BLOGGER);
            return $"{AUTH_BASE_URI}auth?client_id={config.ClientId}&response_type=code&redirect_uri=urn:ietf:wg:oauth:2.0:oob&scope={escapedScope}";
        }

        void PromptAuth(ConfigFile config)
        {
            var url = this.GetAuthUri(config);
            Console.WriteLine($"Request a access code:\n{url}");
        }

        public class GoogleOAuth2Client
        {
            ConfigFile config;
            HttpClient client;

            internal GoogleOAuth2Client(ConfigFile config)
            {
                this.config = config;
                this.client = new HttpClient();
                if (!string.IsNullOrEmpty(this.config.AccessToken))
                {
                    this.SetClientAccessToken(this.config.AccessToken);
                }
            }

            public bool AutoRefresh { get; set; } = true;

            void SetClientAccessToken(string token)
            {
                const string HEADER_KEY = "Authorization";
                var headersRef = this.client.DefaultRequestHeaders;
                if (headersRef.Contains(HEADER_KEY))
                {
                    headersRef.Remove(HEADER_KEY);
                }
                this.client.DefaultRequestHeaders.Add(HEADER_KEY, $"Bearer {this.config.AccessToken}");
            }

            internal async Task<HttpResponseMessage> PerformRequest(string uri, HttpMethod method, HttpContent content = null)
            {
                Func<Task<HttpResponseMessage>> action;
                // prepare request actions depending on request type
                if (method == HttpMethod.Get)
                {
                    action = async () => await this.client.GetAsync(uri);
                }
                else
                {
                    var sendingContent = content == null ? new FormUrlEncodedContent(new Dictionary<string, string>()) : content;
                    var req = new HttpRequestMessage(method, uri) { Content = sendingContent };
                    action = async () => await this.client.SendAsync(req);
                }

                // perform actual request
                var response = await action();
                if (response.StatusCode == HttpStatusCode.Unauthorized && this.AutoRefresh)
                {
                    // retry if new token is acquired
                    if (await this.ConsumeRefreshToken() == 0)
                    {
                        response = await action();
                        return response;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                else
                {
                    return response;
                }
            }

            private async Task<HttpResponseMessage> PerformTokenRequest(FormUrlEncodedContent content)
            {
                var tokenFetchUri = $"{AUTH_BASE_URI}token";
                return await this.client.PostAsync(tokenFetchUri, content);
            }

            internal async Task<int> RedeemCode(string accessCode)
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"client_id", this.config.ClientId},
                    {"client_secret", this.config.ClientSecret},
                    {"redirect_uri", "urn:ietf:wg:oauth:2.0:oob"},
                    {"code", accessCode},
                    {"grant_type", "authorization_code"}
                });
                var responseMessage = await PerformTokenRequest(content);
                if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine($"some error!!! code: {responseMessage.StatusCode.ToString()}");
                    return 1;
                }
                var responseString = await responseMessage.Content.ReadAsStringAsync();
                Console.WriteLine($"response string is: {responseString}");
                var responseParsed = JsonConvert.DeserializeAnonymousType(responseString, new
                {
                    access_token = "",
                    token_type = "",
                    expires_in = 0,
                    refresh_token = "",
                    error = ""
                });
                if (responseParsed == null || string.IsNullOrEmpty(responseParsed.access_token) || !string.IsNullOrEmpty(responseParsed.error))
                {
                    Console.WriteLine($"fetching access token failed. response: {responseString}");
                    return 1;
                }
                Console.WriteLine($"seems good. token = {responseParsed.access_token}");
                this.config.AccessToken = responseParsed.access_token;
                this.config.RefreshToken = responseParsed.refresh_token;
                this.config.Save();
                this.SetClientAccessToken(this.config.AccessToken);

                return 0;
            }

            internal async Task<int> ConsumeRefreshToken()
            {
                Console.WriteLine("trying to refresh access token.");
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"client_id", this.config.ClientId},
                    {"client_secret", this.config.ClientSecret},
                    {"refresh_token", this.config.RefreshToken},
                    {"grant_type", "refresh_token"}
                });
                var responseMessage = await PerformTokenRequest(content);
                if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine($"error refreshing token. code: {responseMessage.StatusCode.ToString()}");
                    return 1;
                }
                var responseString = await responseMessage.Content.ReadAsStringAsync();
                var responseParsed = JsonConvert.DeserializeAnonymousType(responseString, new { access_token = "", token_type = "", expires_in = 0 });
                this.config.AccessToken = responseParsed.access_token;
                this.config.Save();
                this.SetClientAccessToken(this.config.AccessToken);
                return 0;
            }
        }

        class BloggerClient
        {
            GoogleOAuth2Client client;
            string blogId;

            public BloggerClient(GoogleOAuth2Client client, string blogId)
            {
                this.client = client;
                this.blogId = blogId;
            }

            async Task<HttpResponseMessage> PerformRequest(string endpoint, HttpMethod method, Dictionary<string, string> contentSource)
            {
                return await this.PerformRequest(endpoint, HttpMethod.Post, new FormUrlEncodedContent(contentSource));
            }

            async Task<HttpResponseMessage> PerformRequest(string endpoint, HttpMethod method, HttpContent content = null)
            {
                return await this.client.PerformRequest($"{BLOGGER_BASE_URI}{endpoint}", method, content);
            }

            public async Task<string> GetBlogUri()
            {
                var response = await this.PerformRequest($"blogs/{this.blogId}", HttpMethod.Get);
                var result = JsonConvert.DeserializeAnonymousType(await response.Content.ReadAsStringAsync(), new
                {
                    kind = "",
                    id = "",
                    name = "",
                    description = "",
                    published = new DateTime(),
                    updated = new DateTime(),
                    url = ""
                });
                return result.url;
            }

            async Task<bool> Revert(BlogEntry entry)
            {
                try
                {
                    await PerformRequest($"blogs/{this.blogId}/posts/{entry.PostId}?revert=true", new HttpMethod("PATCH"));
                }
                catch (System.Exception)
                {
                    return false;
                }
                return true;
            }

            async Task<bool> Update(BlogEntry entry)
            {
                HttpResponseMessage response;
                try
                {
                    response = await PerformRequest($"blogs/{this.blogId}/posts/{entry.PostId}?alt=json&fetchBody=false",
                        new HttpMethod("PUT"), new StringContent(JsonConvert.SerializeObject(entry), Encoding.UTF8, "application/json"));
                }
                catch (System.Exception)
                {
                    Console.WriteLine("Ran into an exception on updating an entry");
                    return false;
                }
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("target URI: " + $"blogs/{this.blogId}/posts/{entry.PostId}?alt=json&fetchBody=false");
                    Console.WriteLine("data body: " + JsonConvert.SerializeObject(entry));
                    Console.WriteLine("response detail");
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                    return false;
                }
                return true;
            }

            public async Task<bool> SaveDraft(string title, string uri, string content)
            {
                // create new entity
                var entry = new BlogEntry();
                // save w/ dummy title
                entry.Title = uri;
                Console.WriteLine($"temporarily setting title to {entry.Title}");
                var response = await this.PerformRequest($"blogs/{this.blogId}/posts?alt=json&fetchBody=false",
                    HttpMethod.Post, new StringContent(JsonConvert.SerializeObject(entry), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("creating new post failed.");
                    Console.WriteLine("path:" + $"blogs/{this.blogId}/posts?fetchBody=false");
                    Console.WriteLine("data body: " + JsonConvert.SerializeObject(entry));
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                    return false;
                }
                var createResult = JsonConvert.DeserializeAnonymousType(await response.Content.ReadAsStringAsync(), new
                {
                    id = ""
                });
                Console.WriteLine($"post ID: {createResult.id}");
                entry.PostId = createResult.id;
                // revert publishing status
                if (!await this.Revert(entry))
                {
                    Console.WriteLine("revert failed");
                    return false;
                }
                // update title & fill in content
                entry.Title = "(draft)" + title;
                entry.Content = content;
                Console.WriteLine($"changing title to {entry.Title}");
                if (!await this.Update(entry))
                {
                    Console.WriteLine("update failed");
                    return false;
                }
                return true;
            }
        }

        public string FilenameToDocId(string filename)
        {
            return new Regex("^(\\d{2})(\\d{2})_(.+)\\.re$").Replace(filename, "20$1/$2/$3");
        }

        public string ExtractIdFromFileName(string filename)
        {
            return new Regex("^(\\d{2})(\\d{2})_(.+)\\.re$").Replace(filename, "$3");
        }

        public async Task<int> Execute(string[] args)
        {
            var configPath = Path.Combine(GetHomeDirectory(), CONFIG_FILE_NAME);
            var config = ConfigFile.Load(configPath);
            if (!config.FileExists)
            {
                Console.WriteLine($"config file was not found. Will make one in {configPath}");
            }

            if (args.Length == 0)
            {
                Console.WriteLine("usage: review-blogger 1505_target-file.re");
                return 1;
            }
            if (args.Length == 1 && !File.Exists(args[0]))
            {
                Console.WriteLine($"target file \"{args[0]}\" not found");
            }
            var c = new GoogleOAuth2Client(config);
            if (args.Length == 2)
            {
                if (args[1] == "auth")
                {
                    PromptAuth(config);
                }
                else
                {
                    var ret = await c.RedeemCode(args[1]);
                    if (ret != 0)
                    {
                        return ret;
                    }
                }
                return 0;
            }
            var reviewFilename = args[0];

            // TODO: refresh token if needed
            var bloggerClient = new BloggerClient(c, config.BlogId);
            var siteUri = await bloggerClient.GetBlogUri();
            if (string.IsNullOrEmpty(siteUri))
            {
                // possible token expire
                Console.WriteLine("token expired. trying to consume refresh-token for fetching new one.");
                await c.ConsumeRefreshToken();
                siteUri = await bloggerClient.GetBlogUri();
                if (string.IsNullOrEmpty(siteUri))
                {
                    Console.WriteLine("refresh-token seems expired also. please run \"auth\" subcommand for re-authorizing.");
                    return 1;
                }
            }
            Console.WriteLine($"blog URI: {siteUri}");
            var docId = FilenameToDocId(reviewFilename);
            var entryUri = $"{siteUri}{docId}.html";

            var conv = new ReVIEWConverter();
            conv.CompileDocument(reviewFilename, REVIEW_COMPILE_TIMEOUT_MS);
            var decoratedContent = conv.DecorateForBlogger(siteUri, entryUri, docId.Replace('/', '_'));
            var entry = conv.ExtractTitleAndContent();
            await bloggerClient.SaveDraft(entry.Item1, ExtractIdFromFileName(reviewFilename), $"<div class=\"review-post\">{entry.Item2}</div>");

            return 0;
        }
    }
}
