using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDBMigrations;

namespace MongoDBMigrations.SmokeTests.Promotion;

[TestClass]
public sealed class StepModelTests
{
    sealed class SampleStep : SourceStep
    {
        public override long Id => 1;
        public override string Name => "sample";
        public override Outcome<Unit> Up(StepContext ctx) => TryCatch(() => { });
    }

    [TestMethod]
    public void Step_is_irreversible_by_default()
    {
        var step = new SampleStep();
        Assert.AreEqual(Reversibility.Irreversible, step.Reversibility);
    }

    [TestMethod]
    public void Default_Down_returns_not_reversible_failure()
    {
        var step = new SampleStep();

        var result = step.Down(new StepContext(null!, null!, default));

        Assert.IsTrue(Fail(result, out var e));
        Assert.AreEqual(StepErrors.NotReversibleCode, e?.Code);
    }
}
