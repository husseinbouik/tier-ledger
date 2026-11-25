using FluentAssertions;
using TierLedger.Domain.Common;
using TierLedger.Domain.Processes;

namespace TierLedger.UnitTests.Domain;

public sealed class PaymentProcessTests
{
    private static PaymentProcess Start(decimal totalDue = 300m) =>
        PaymentProcess.Start(Guid.NewGuid(), Guid.NewGuid(), totalDue, new DateOnly(2026, 3, 1));

    [Fact]
    public void Start_SetsAwaitingPayment_WithFullAmountOutstanding()
    {
        var process = Start();

        process.Status.Should().Be(PaymentProcessStatus.AwaitingPayment);
        process.OutstandingAmount.Should().Be(300m);
        process.IsFullyPaid.Should().BeFalse();
        process.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void ApplyPayment_Partial_ReturnsFalse_Full_ReturnsTrue()
    {
        var process = Start();

        process.ApplyPayment(120m).Should().BeFalse();
        process.OutstandingAmount.Should().Be(180m);

        process.ApplyPayment(180m).Should().BeTrue();
        process.IsFullyPaid.Should().BeTrue();
    }

    [Fact]
    public void Complete_BeforeFullPayment_Throws_AndAfter_Succeeds()
    {
        var process = Start();
        process.ApplyPayment(100m);

        var act = () => process.Complete();
        act.Should().Throw<DomainException>().WithMessage("*not fully paid*");

        process.ApplyPayment(200m);
        process.Complete();

        process.Status.Should().Be(PaymentProcessStatus.Completed);
        process.CompletedOnUtc.Should().NotBeNull();
    }

    [Fact]
    public void Abort_SetsAborted_AndCompletedProcess_CannotBeAborted()
    {
        var process = Start();
        process.Abort();
        process.Status.Should().Be(PaymentProcessStatus.Aborted);

        var completed = Start(50m);
        completed.ApplyPayment(50m);
        completed.Complete();
        var act = () => completed.Abort();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkOverdue_SetsFlagOnce()
    {
        var process = Start();
        var now = DateTime.UtcNow;

        process.MarkOverdue(now);
        process.IsOverdue.Should().BeTrue();
        process.OverdueSinceUtc.Should().Be(now);

        process.MarkOverdue(now.AddDays(1));
        process.OverdueSinceUtc.Should().Be(now);
    }
}
