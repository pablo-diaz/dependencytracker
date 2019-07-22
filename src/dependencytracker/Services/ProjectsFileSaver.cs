using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

using Models;

namespace Services
{
    public static class ProjectsFileSaver
    {
        public static async Task SaveProjects(List<BitbucketProject> projects, string repositoriesFileName, string librariesFileName)
        {
            var repositoriesFileContent = new List<string>() { "BitbucketProjectName,RepositoryName,BranchName,CSharpProjectName,LibraryName,LibraryVersion,DoesLibraryExistInNexus" };
            var librariesFileContent = new List<string>() { "LibraryName,LibraryVersion,DoesLibraryExistInNexus,DependencyName,DependencyVersion,DoesDependencyExistInNexus" };

            foreach (var bitbucketProject in projects)
                foreach(var repository in bitbucketProject.Repositories)
                    foreach(var branch in repository.Branches)
                        foreach(var cSharpProject in branch.Projects)
                            foreach (var library in cSharpProject.Dependencies)
                            {
                                repositoriesFileContent.Add($"{bitbucketProject.Name},{repository.Name},{branch.Name},{cSharpProject.Name},{library.Name},{library.Version},{library.DoesItExistInNexus}");
                                librariesFileContent.AddRange(GetDependencyLinesForLibrary(library));
                            }

            await File.WriteAllLinesAsync(repositoriesFileName, repositoriesFileContent);
            await File.WriteAllLinesAsync(librariesFileName, librariesFileContent);
        }

        private static List<string> GetDependencyLinesForLibrary(Library library)
        {
            var lines = new List<string>();
            foreach(var dependency in library.Dependencies)
            {
                lines.Add($"{library.Name},{library.Version},{library.DoesItExistInNexus},{dependency.Name},{dependency.Version},{dependency.DoesItExistInNexus}");
                lines.AddRange(GetDependencyLinesForLibrary(dependency));
            }

            return lines;
        }
    }
}
