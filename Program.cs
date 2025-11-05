using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GitHubActivityCLI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: dotnet run <github-username>");
                return;
            }

            string username = args[0];
            Console.WriteLine($"Fetching recent GitHub activity for user: {username}\n");

            var events = await FetchGitHubActivity(username);
            if (events == null || events.Count == 0)
            {
                Console.WriteLine("No activity found or failed to fetch data.");
                return;
            }

            foreach (var activity in ParseActivity(events))
            {
                Console.WriteLine($"- {activity}");
            }
        }

        static async Task<List<JsonElement>> FetchGitHubActivity(string username)
        {
            string url = $"https://api.github.com/users/{username}/events";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "CSharpApp");

            try
            {
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to fetch data (Status: {response.StatusCode})");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var events = JsonSerializer.Deserialize<List<JsonElement>>(json);
                return events;
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("Network error: Could not reach GitHub API.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }

            return null;
        }

        static List<string> ParseActivity(List<JsonElement> events)
        {
            var descriptions = new List<string>();

            foreach (var activity in events)
            {
                string type = activity.GetProperty("type").GetString() ?? "Unknown";
                string repoName = activity.TryGetProperty("repo", out var repoElement)
                    ? repoElement.GetProperty("name").GetString() ?? ""
                    : "";

                string description = type switch
                {
                    "PushEvent" => $"Pushed commits to {repoName}",
                    "IssuesEvent" => $"Opened/modified an issue in {repoName}",
                    "WatchEvent" => $"Starred {repoName}",
                    "CreateEvent" => $"Created something in {repoName}",
                    _ => $"Performed {type}" + (string.IsNullOrEmpty(repoName) ? "" : $" in {repoName}")
                };

                descriptions.Add(description);
            }

            return descriptions;
        }
    }
}
