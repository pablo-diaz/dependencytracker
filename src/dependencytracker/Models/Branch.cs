using System.Collections.Generic;

namespace Models
{
    public class Branch
    {
        public string Name { get; set; }
        public List<CSharpProject> Projects { get; set; }

        public Branch()
        {
            this.Projects = new List<CSharpProject>();
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
