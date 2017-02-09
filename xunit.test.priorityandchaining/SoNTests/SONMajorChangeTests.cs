
using FluentAssertions;
using SoN.API;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using xunit.test.priorityandchaining.SoNTests.TestClassFixtures;
using Xunit;

namespace xunit.test.priorityandchaining.SoNTests
{
    [TestCaseOrderer("xunit.helpers.TestExecutionOrder.PriorityOrderer", "xunit.helpers")]
    public class SoNMajorChangeTests : IClassFixture<SoNIDCacheFixture>
    {
        private SoNIDCacheFixture sonCache = null;
        private ISoNAPI sut = null;

        public SoNMajorChangeTests(SoNIDCacheFixture sonCache)
        {
            // This injected param is an instance of the "ClassFixture" which is created once by xunit for the class
            // and then passed in here for the execution of each test.  We use it here, in conjunction with the TestPriorityAttribute to
            // allow context to be passed across the tests, and not lost.  In this case, test 1 created a set of IDs which are passed from test to test inside this fixture.
            this.sonCache = sonCache;
            this.sut = new SoNFacade();
        }

        [Theory, TestPriority(1)]
        [ExcelData(@"SoNTests\ScenarioData\SoNCreateMajorChangeScenarios.xls", "select * from [Sheet1$A1:D6]")]
        public void CreateMajorChangeSoN(string tlb, string username, string password, string SoNTitle)
        {
            // Arrange

            // Act
            var sonID = sut.CreateSoN(tlb, SoNTitle);
            sonCache.sonIDsByTLB.Add(tlb, sonID);

            // Assert
            sonID.Should().BeGreaterThan(0);
            //Debug.WriteLine(string.Format("tlb: {0}, user: {1}, pwd: {2}, SoN: {3} - Created with ID: {4}", tlb, username, password, SoNTitle, sonID));
        }


        [Theory, TestPriority(2)]
        [ExcelData(@"SoNTests\ScenarioData\SoNModerateMajorChangeScenarios.xls", "select * from [Sheet1$A1:E6]")]
        public void ModerateMajorChangeSoN(string tlb, string username1, string password1, string username2, string password2)
        {
            // Arrange

            // Act
            var sonID = sonCache.sonIDsByTLB.SingleOrDefault(tlbSon => tlbSon.Key == tlb);

            // Assert
            sonID.Should().NotBe(default(int));
            //Debug.WriteLine(string.Format("tlb: {0}, user: {1}, pwd: {2}, user2: {3}, pwd2: {4} - Created with ID: {5}", tlb, username1, password1, username2, password2, sonID));
        }
    }
}
