using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityUtils.UnityPM.Sources
{
    public class VRChatSource : ISource, ISourceUnique
    {
        public string Name { get; } = "VRChat SDKs";

        public List<Package> GetPackages()
        {
            return new List<Package>()
            {
                new Package(this)
                {
                    name = "VRChat SDK3",
                    unityPackages = new List<UnityPackage>()
                    {
                        new SimpleUnityPackage("SDK3 Worlds", new Uri(@"https://vrchat.com/download/sdk3-worlds")),
                        new SimpleUnityPackage("SDK3 Avatars", new Uri(@"https://vrchat.com/download/sdk3-avatars"))
                    }
                },
                new Package(this)
                {
                    name = "VRChat SDK2",
                    unityPackages = new List<UnityPackage>()
                    {
                        new SimpleUnityPackage("SDK2", new Uri(@"https://vrchat.com/download/sdk2"))
                    }
                }
            };
        }
    }
}