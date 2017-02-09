Test Priority and Test chaining
===============================

OK, before we start.  Forget if you think chaining tests together is a good thing or an anti-pattern which will lead to a whole world of hurt later on, and dependencies and resulting brittleness becomes a mill stone that takes more effort to maintain than the code you are trying to test!

Personally, in the vast majority of cases, I prefer my tests to be self contained, autonomous and testing a single *thing*.

That said, I have been working for a client recently who is trying to create an automnated UI acceptance test suite for a COTS product that are using, but which they have heavily customised (added new screen, workflows and so forth).  Some of the test cases involve *creating* things, and then *approving them* through multi-approver workflows and so forth.  They want to set this up, so that they can have a *create test* which created the *thing* and a separate *appoval test* which takes the *created thing* and moves it through workflow.  There is no real usable API for this COTS product, and it has a nightmare DB structure, so the usual approaches of *seeding test data* as part of the test startup is not going to work here.  Restoring the DB to a known baseline as part the tests is also a *non-starter*

So, now I have that off my chest, if you are ever in this position and need to deal with it, this is how I have managed it.

Toolset
-------
We are using the following tools.

* c# and .NET
* xunit
* Fluent Assertions
* Selenium Web Driver (note. this is not used in this sample project)

Requirements of this work.
--------------------------
To make this work in the way we need it, there are three key elements.

1. Have a way of passing data to the individual tests (preferably using Excel as the data source)
2. Have a way of controling the order of test execution (no point trying to approve what you have not created!)
3. Have a way of sharing *context* between the tests within a test class (e.g. the ID of the newly created item, needs to be passed to the approval test)

Data Driven Tests
-----------------

The usual requirement here, to be able to implement the logic of a test once, and then execute it many times with a range of inputs and expected results.

xunit provides a number of options here... `MemberData`, `InlineData` and so forth.  These are great solutions for developer focused *unit tests*, or *integration tests*, however, for our *UI acceptance tests* we want to externalise the data from the tests so that they can be maintained by *non-developers* and can be varied per environment if needed.  This lead us to use the `ExcelDataAttribute`, which we borrowed from [the xunit samples](https://github.com/xunit/samples.xunit/blob/master/ExcelDataExample/ExcelDataAttribute.cs "xunit samples on GutHub")

We use the `ExcelDataAttribute` on a specific test (`Theory`) as follows:

`[ExcelData(@"SoNTests\ScenarioData\SoNModerateMajorChangeScenarios.xls", "select * from [Sheet1$A1:E6]")]`

The first parameter is the location of the Excel file (Excel 2003 format), and the second is the range of cells to select from the spreadsheet (including the column headers).  You will end up with a test execution for each row in the input file.

Test Order Execution
--------------------

We need to order the execution of *test methods* within a test, to ensure that when a given test runs, all *upstream dependent tests* have run already (yeah, I know its not a good idea to make tests interdepenant!).

To do this, we use the `TestPriorityAttribute` and `PriorityOrderer` classes, and decorate the test class with the appropriately.

**TestPriorityAttribute** 
We decorate specific test methods within our test class with this attribute to control their order of execution.  The attribute takes a single parameter, the tests priority.  The lower the priority, the earlier the test is executed.  For example, a test of Priority 1, would run before Priority 2

We apply the attribute like so...

```csharp
        [Fact, TestPriority(1)]
        public void RunMeFirstTest()
        {
            ...
        }

        [Fact, TestPriority(2)]
        public void RunMeSecondTest()
        {
            ...
        }
```

**PriorityOrderer** 
No we have the individual tests tagged with a `TestPriorityAttribute` we need to tell xunit how to do interpret this.  The achieve this by using the xunit `TestCaseOrdererAttribute`.  This provides an extensibility point in xunit where we can inject our own ordering class, which we can then use to order the tests within the class (in our case using the `TestPriorityAttribute` class).  To this end, the project provides a `PriorityOrderer` class, to implement the ordering logic.  Here is how we apply this to the test class.

```csharp
    [TestCaseOrderer("xunit.helpers.TestExecutionOrder.PriorityOrderer", "xunit.helpers")]
    public class MyTestsToBeOrdered
    {
        [Fact, TestPriority(1)]
        public void RunMeFirstTest()
        {
            ...
        }
    }

```
You do not need to modify the `PriorityOrderer` class in order to re-use it, you just apply it to your class as per the example above.

Sharing Context between tests within a class
--------------------------------------------

As we know, xunit *news up* an instance of your test class for each test method it executes on the class.  As a result, you cant simply share state between tests in member variables (except for statics).  The recommended way of achieving this within xunit, if via `IClassFixture`.  

A *class fixture* allows you to define a class separate to your *test class* which can store some *state*.  When you use a *class fixture*, xunit will instantiate an instance of your *class fixture* class the first time it sees the associated test class.  Then, when it executes each test method on your test class, it will *new up* an instance of your *test class* for each test method, but when doing so, it passes in as a constructor parameter, the *class fixture*.  As the same *class fixture* is passed in to the constructor for each test in the test class, the *class fixture* can carry context between tests.

Here is an example of the a *class fixture*

```csharp
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
```

This example, provides a cache which different tests can read from / write to.

The *class fixture* is associated with the test class, by the test class implementing the `IClassFixture`, and passing the *class fixture* test as the *generic paramter* as follows:

```csharp
    public class SoNMajorChangeTests : IClassFixture<SoNIDCacheFixture>
    {
        ...
    }
```

We then define the constructor for the *test class* which takes the *class fixture* as a param, like this...

```csharp
    public class SoNMajorChangeTests : IClassFixture<SoNIDCacheFixture>
    {
        private SoNIDCacheFixture sonCache = null;
        private ISoNAPI sut = null;

        public SoNMajorChangeTests(SoNIDCacheFixture sonCache)
        {
            this.sonCache = sonCache;
        }
    ...
    }
```

When the tests are executed, xunit will create a single instance of the `SoNIDCacheFixture` and pass it to the constructor `SoNMajorChangeTests` class each time it instantiates an instance for a test method.  This allows context to flow between the tests as in the following sample.

```csharp
    public class SoNMajorChangeTests : IClassFixture<SoNIDCacheFixture>
    {
        private SoNIDCacheFixture sonCache = null;
        private ISoNAPI sut = null;

        [Theory, TestPriority(1)]
        [ExcelData(@"SoNTests\ScenarioData\SoNCreateMajorChangeScenarios.xls", "select * from [Sheet1$A1:D6]")]
        public void CreateMajorChangeSoN(string tlb, string username, string password, string SoNTitle)
        {
            var sonID = sut.CreateSoN(tlb, SoNTitle);

            // Add the new ID to the cache which is implemented as a "class fixture"
            sonCache.sonIDsByTLB.Add(tlb, sonID);
        }

        [Theory, TestPriority(2)]
        [ExcelData(@"SoNTests\ScenarioData\SoNModerateMajorChangeScenarios.xls", "select * from [Sheet1$A1:E6]")]
        public void ModerateMajorChangeSoN(string tlb, string username1, string password1, string username2, string password2)
        {
            var sonID = sonCache.sonIDsByTLB.SingleOrDefault(tlbSon => tlbSon.Key == tlb);

            // Check in the "class fixture" cache that we have a value...   We should have, as a value was added to the cache by the first test, which has a higher priority.
            sonID.Should().NotBe(default(int));
        }
    }
```

Conclusions
-----------
As stated at the start.  Having inter-test dependencies is not something you should aim for, quite the opposite in fact.  That said, there are times, especially when dealing with COTS systems which are outside of your control, where test dependencies are just the lesser of the evils available to you.  So, use these approaches sparingly and only when you have a good reason!

