using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

using Services;
using Utils;

namespace App
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .Build();

                var bitbucketBaseAddress = new Uri(configuration["Services:Bitbucket:BaseAddress"]);
                var bitbucketHelper = new BitbucketHelper(bitbucketBaseAddress);

                var nexusBaseAddress = new Uri(configuration["Services:Nexus:BaseAddress"]);
                var nexusHelper = new NexusHelper(nexusBaseAddress);

                var projects = await new DependencyGrapher(bitbucketHelper, nexusHelper).CreateProjectGraph();

                var repositoriesFilePath = configuration["Output:RepositoriesFilePath"];
                var librariesFilePath = configuration["Output:LibrariesFilePath"];
                await ProjectsFileSaver.SaveProjects(projects, repositoriesFilePath, librariesFilePath);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("Done!");
        }
    }
}
