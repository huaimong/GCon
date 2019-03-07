using System.Diagnostics;

namespace VgcApisTests.IPlugableTests
{
    public class CompLv2 :
        VgcApis.Models.BaseClasses.PlugableComponent<CompLv1>
    {
        public CompLv2() { }

        public string Name() => "Component lv2";

        protected override void Cleanup()
        {
            Debug.WriteLine("Comp lv2 disposed.");
            base.Cleanup();
        }

    }
}
