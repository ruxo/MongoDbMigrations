using LanguageExt;

namespace MongoDBMigrations;

public abstract class MigrationStep
{
    public abstract string Name { get; }
    public virtual Reversibility Reversibility => Reversibility.Irreversible;
    public virtual string? IrreversibleReason => null;

    public abstract Outcome<Unit> Up(StepContext ctx);
    public virtual Outcome<Unit> Down(StepContext ctx) => StepErrors.NotReversible(Name);
}
