using System.Collections.Generic;

namespace Models
{
    public class Repository
    {
        public string Slug { get; set; }
        public string Name { get; set; }

        public List<Branch> Branches { get; set; }

        public Repository()
        {
            this.Branches = new List<Branch>();
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
