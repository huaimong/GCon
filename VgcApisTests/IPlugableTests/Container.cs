using System.Diagnostics;

namespace VgcApisTests.IPlugableTests
{
    public class Container :
        VgcApis.Models.BaseClasses.PlugableComponent<Container>
    {
        public Container() { }

        public string Name() => "Container";

        protected override void Cleanup()
        {
            Debug.WriteLine("Container disposed.");
        }

    }
}
