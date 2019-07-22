using System.Collections.Generic;

namespace Models
{
    public class Library
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public bool DoesItExistInNexus { get; set; }
        public string Key { get => $"{Name}__{Version}"; }

        public List<Library> Dependencies { get; set; }

        public Library()
        {
            this.Dependencies = new List<Library>();
            this.DoesItExistInNexus = true;
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
