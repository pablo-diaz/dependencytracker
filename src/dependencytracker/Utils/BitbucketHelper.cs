using System;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

using Models;
using Models.Extensions;

namespace Utils
{
    public class BitbucketHelper
    {
        private readonly HttpClient _bitBucketHttpClient;

        public BitbucketHelper(Uri bitbucketBaseAddress)
        {
            _bitBucketHttpClient = new HttpClient();
            _bitBucketHttpClient.BaseAddress = bitbucketBaseAddress;
        }

        public async Task<List<BitbucketProject>> GetAllBitbucketProjects()
        {
            var projects = new List<BitbucketProject>();
            var result = await _bitBucketHttpClient.GetAsync("/rest/api/1.0/projects?limit=1000");
            result.EnsureSuccessStatusCode();

            var stringResult = await result.Content.ReadAsStringAsync();
            dynamic parsedString = JsonConvert.DeserializeObject(stringResult);
            foreach (dynamic project in parsedString.values)
            {
                var projectInfo = new BitbucketProject() {
                    Key = project.key,
                    Name = project.name
                };
                projects.Add(projectInfo);
            }

            return projects;
        }

        public async Task<List<Repository>> GetAllRepositoriesForProject(BitbucketProject bitbucketProject)
        {
            var repositories = new List<Repository>();
            var result = await _bitBucketHttpClient.GetAsync($"/rest/api/1.0/projects/{bitbucketProject.Key}/repos?limit=1000");
            result.EnsureSuccessStatusCode();

            var stringResult = await result.Content.ReadAsStringAsync();
            dynamic parsedString = JsonConvert.DeserializeObject(stringResult);
            foreach (dynamic repository in parsedString.values)
            {
                var repositoryInfo = new Repository() {
                    Slug = repository.slug,
                    Name = repository.name
                };
                repositories.Add(repositoryInfo);
            }

            return repositories;
        }

        public async Task<List<CSharpProject>> GetCSharpProjectsForRepository(BitbucketProject bitbucketProject, Repository repository, string branchName)
        {
            var projects = new List<CSharpProject>();

            try
            {
                var result = await _bitBucketHttpClient.GetAsync($"/rest/api/1.0/projects/{bitbucketProject.Key}/repos/{repository.Slug}/files?limit=1000&at={branchName}");
                result.EnsureSuccessStatusCode();

                var stringResult = await result.Content.ReadAsStringAsync();
                dynamic parsedString = JsonConvert.DeserializeObject(stringResult);
                foreach (dynamic fp in parsedString.values)
                {
                    string filePath = (string)fp;
                    if (IsItConsideredAsConfigFile(filePath))
                    {
                        var csharpProject = await CreateCSharpProjectFromConfigFile(bitbucketProject, repository, filePath, branchName);
                        projects.Add(csharpProject);
                    }
                }
            }
            catch
            {
            }

            return projects;
        }

        private bool IsItConsideredAsConfigFile(string filePath) => 
            filePath.EndsWith("/packages.config");

        private async Task<CSharpProject> CreateCSharpProjectFromConfigFile(BitbucketProject bitbucketProject, Repository repository, string configFilePath, string branchName)
        {
            var result = await _bitBucketHttpClient.GetAsync($"/rest/api/1.0/projects/{bitbucketProject.Key}/repos/{repository.Slug}/raw/{configFilePath}?limit=1000&at={branchName}");
            result.EnsureSuccessStatusCode();

            var stringResult = await result.Content.ReadAsStringAsync();

            return new CSharpProject() {
                Name = GetCSharpProjectNameFromConfigFile(configFilePath),
                Dependencies = GetDependenciesFromConfigFileContent(stringResult)
                               .Where(lib => lib.IsItOneOfOurInterest())
                               .ToList()
            };
        }

        private string GetCSharpProjectNameFromConfigFile(string configFilePath) =>
            configFilePath.Split('/')[0];

        private List<Library> GetDependenciesFromConfigFileContent(string configFileXMLContent)
        {
            using (var reader = new StringReader(configFileXMLContent))
            {
                return XElement.Load(reader)
                    .Descendants("package")
                    .Select(package => new Library() {
                        Name = package.Attribute("id").Value,
                        Version = package.Attribute("version").Value
                    })
                    .ToList();
            }
        }
    }
}
