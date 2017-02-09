using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xunit.test.priorityandchaining.SoNTests.TestClassFixtures
{
    /// <summary>
    /// A class fixture is used to share test context across all tests within a test class.
    /// As we know, xunit creates a new instance of a test class for each and every test (fact / theory) inside the class.
    /// This gives good isolation between tests, but does not allow you to share context (statics aside).
    /// Using a ClassFixture (like this one), is instantiated once of the class.  The same instance is then passed to the test class in the constructor
    /// The same instance of the ClassFixture is then effectively shared across all tests within the same test class.
    /// </summary>
    public sealed class SoNIDCacheFixture : IDisposable
    {
        public Dictionary<string, int> sonIDsByTLB;

        public SoNIDCacheFixture()
        {
            sonIDsByTLB = new Dictionary<string, int>();
        }

        public void Dispose()
        {
            sonIDsByTLB.Clear();
        }
    }
}
