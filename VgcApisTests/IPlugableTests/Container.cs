using System.Diagnostics;

namespace VgcApisTests.IPlugableTests
{
    public class Container :
        VgcApis.Models.BaseClasses.Plugable<Container>
    {
        public Container() { }

        public string Name() => "Container";

        protected override void Cleanup()
        {
            Debug.WriteLine("Container disposed.");
        }

    }
}
