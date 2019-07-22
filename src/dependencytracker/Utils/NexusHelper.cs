using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

using Models;

namespace Utils
{
    public class NexusHelper
    {
        private class NexusComponentDTO
        {
            public string id { get; set; }
            public string repositoryName { get; set; }
            public string group { get; set; }
            public string name { get; set; }
            public string version { get; set; }
            public string format { get; set; }
        }

        private readonly HttpClient _nexusHttpClient;

        public NexusHelper(Uri nexusBaseAddress)
        {
            _nexusHttpClient = new HttpClient();
            _nexusHttpClient.BaseAddress = nexusBaseAddress;
        }

        public async Task<List<Library>> GetDependenciesForLibrary(Library library)
        {
            var dependencies = new List<Library>();
            var nexusComponentInfo = await GetNexusComponentForLibrary(library);
            library.DoesItExistInNexus = nexusComponentInfo != null;
            if (nexusComponentInfo == null)
                return dependencies;

            var jsonToPost = JsonConvert.SerializeObject(new
            {
                action = "coreui_Component",
                method = "readComponentAssets",
                type = "rpc",
                tid = 29,
                data = new List<object>() {
                    new {
                        page = 1,
                        start = 0,
                        limit = 25,
                        filter = new List<object>() {
                            new {
                                property = "repositoryName",
                                value = "nuget-internal"
                            },
                            new {
                                property = "componentModel",
                                value = JsonConvert.SerializeObject(nexusComponentInfo)
                            }
                        }
                    }
                }
            });
            var postContent = new StringContent(jsonToPost, System.Text.Encoding.Default, "application/json");
            var result = await _nexusHttpClient.PostAsync("/nexus/service/extdirect", postContent);
            result.EnsureSuccessStatusCode();

            var stringResult = await result.Content.ReadAsStringAsync();
            dynamic parsedString = JsonConvert.DeserializeObject(stringResult);

            bool wasItSuccessfull = parsedString.result.success;
            if (!wasItSuccessfull)
                return dependencies;

            string nexusDependencies = parsedString.result.data[0].attributes.nuget.dependencies;
            if(!string.IsNullOrEmpty(nexusDependencies))
            {
                foreach(string nexusDependency in nexusDependencies.Split('|'))
                {
                    var libraryInfo = nexusDependency.Split(':');
                    if (libraryInfo.Length == 2)
                    {
                        var libraryName = libraryInfo[0].Trim();
                        var libraryVersion = libraryInfo[1].Trim().Replace("[", "").Replace("]", "");
                        if (string.IsNullOrEmpty(libraryName) || string.IsNullOrEmpty(libraryVersion))
                            continue;

                        dependencies.Add(new Library() {
                            Name = libraryName,
                            Version = libraryVersion
                        });
                    }
                }
            }

            return dependencies;
        }

        private async Task<NexusComponentDTO> GetNexusComponentForLibrary(Library library)
        {
            var jsonToPost = JsonConvert.SerializeObject(new
            {
                action = "coreui_Search",
                method = "read",
                type = "rpc",
                tid = 36,
                data = new List<object>() {
                    new {
                        page = 1,
                        start = 0,
                        limit = 300,
                        filter = new List<object>() {
                            new {
                                property = "name.raw",
                                value = library.Name
                            },
                            new {
                                property = "version",
                                value = library.Version
                            }
                        }
                    }
                }
            });
            var postContent = new StringContent(jsonToPost, System.Text.Encoding.Default, "application/json");
            var result = await _nexusHttpClient.PostAsync("/nexus/service/extdirect", postContent);
            result.EnsureSuccessStatusCode();

            var stringResult = await result.Content.ReadAsStringAsync();
            dynamic parsedString = JsonConvert.DeserializeObject(stringResult);

            if (parsedString.result.total == 0)
                return null;

            var newSerializedValue = JsonConvert.SerializeObject(parsedString.result.data[0]);
            return JsonConvert.DeserializeObject<NexusComponentDTO>(newSerializedValue);
        }
    }
}
