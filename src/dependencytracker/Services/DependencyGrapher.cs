using System.Collections.Generic;
using System.Threading.Tasks;

using Models;
using Models.Extensions;
using Utils;

namespace Services
{
    public class DependencyGrapher
    {
        private readonly BitbucketHelper _bitbucketHelper;
        private readonly NexusHelper _nexusHelper;

        private readonly Dictionary<string, Library> _dependenciesCache = new Dictionary<string, Library>();

        public DependencyGrapher(BitbucketHelper bitbucketHelper, NexusHelper nexusHelper)
        {
            this._bitbucketHelper = bitbucketHelper;
            this._nexusHelper = nexusHelper;
        }

        public async Task<List<BitbucketProject>> CreateProjectGraph()
        {
            var projects = await GetBitbucketProjects();
            projects = await SetRepositoriesAndDependencies(projects);
            projects = await SetupDependencyGraph(projects);

            return projects;
        }

        private async Task<List<BitbucketProject>> GetBitbucketProjects() =>
            await this._bitbucketHelper.GetAllBitbucketProjects();

        private async Task<List<BitbucketProject>> SetRepositoriesAndDependencies(List<BitbucketProject> projects)
        {
            foreach (var project in projects)
            {
                project.Repositories = await this._bitbucketHelper.GetAllRepositoriesForProject(project);
                foreach (var repository in project.Repositories)
                {
                    repository.Branches.Add(new Branch() {
                        Name = "master",
                        Projects = await this._bitbucketHelper.GetCSharpProjectsForRepository(project, repository, "master")
                    });

                    repository.Branches.Add(new Branch()
                    {
                        Name = "develop",
                        Projects = await this._bitbucketHelper.GetCSharpProjectsForRepository(project, repository, "develop")
                    });
                }
            }

            return projects;
        }

        private async Task<List<BitbucketProject>> SetupDependencyGraph(List<BitbucketProject> projects)
        {
            foreach(var bitbucketProject in projects)
                foreach(var repository in bitbucketProject.Repositories)
                    foreach(var branch in repository.Branches)
                        foreach(var cSharpProject in branch.Projects)
                        {
                            var newLibraryList = new List<Library>();
                            foreach(var library in cSharpProject.Dependencies)
                            {
                                if (!library.IsItOneOfOurInterest())
                                    continue;
                                var newLibrary = await SetDependenciesForLibrary(library);
                                newLibraryList.Add(newLibrary);
                            }

                            cSharpProject.Dependencies = newLibraryList;
                        }

            return projects;
        }

        private async Task<Library> SetDependenciesForLibrary(Library library)
        {
            if (_dependenciesCache.ContainsKey(library.Key))
                return _dependenciesCache[library.Key];

            var newDependencies = new List<Library>();
            foreach (var dependency in await this._nexusHelper.GetDependenciesForLibrary(library))
            {
                if (!dependency.IsItOneOfOurInterest())
                    continue;
                var newLibrary = await SetDependenciesForLibrary(dependency);
                newDependencies.Add(newLibrary);
            }
            library.Dependencies = newDependencies;

            _dependenciesCache[library.Key] = library;

            return library;
        }
    }
}
