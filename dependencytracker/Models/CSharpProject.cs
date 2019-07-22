using System.Collections.Generic;

namespace Models
{
    public class CSharpProject
    {
        public string Name { get; set; }
        public List<Library> Dependencies { get; set; }

        public CSharpProject()
        {
            this.Dependencies = new List<Library>();
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
