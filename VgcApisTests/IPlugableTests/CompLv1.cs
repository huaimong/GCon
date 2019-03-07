using System.Diagnostics;

namespace VgcApisTests.IPlugableTests
{
    public class CompLv1 :
        VgcApis.Models.BaseClasses.PlugableComponent<Container>
    {
        public CompLv1() { }

        public string Name() => "Component lv1";

        protected override void Cleanup()
        {
            Debug.WriteLine("Comp lv1 disposed.");
            base.Cleanup();
        }
    }
}
