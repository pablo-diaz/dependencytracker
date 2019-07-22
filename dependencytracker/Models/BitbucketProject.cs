using System.Collections.Generic;

namespace Models
{
    public class BitbucketProject
    {
        public string Name { get; set; }
        public string Key { get; set; }

        public List<Repository> Repositories { get; set; }

        public BitbucketProject()
        {
            this.Repositories = new List<Repository>();
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
