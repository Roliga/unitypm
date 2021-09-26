using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityUtils.UnityPM.Sources
{
    public class SimpleTestSource : ISource, ISourceUnique
    {
        public string Name { get; } = "Simple Test SOurce";

        public List<Package> GetPackages()
        {
            return new List<Package>()
            {
                new Package()
                {
                    name = "Somple test package",
                    files = new List<File>()
                    {
                        new SimpleFile("Some simple test file", new Uri(@"C:\Users\Roliga\Desktop\hmm.txt"), "SimpleTestSource hmm.txt")
                    }
                }
            };
        }
    }
}