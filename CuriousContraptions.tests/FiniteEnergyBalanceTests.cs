using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class FiniteEnergyBalanceTests
{
    [Fact]
    public void SmallPaymentsRetainTheUnspentRemainderInsteadOfSpendingBulkUlps()
    {
        // Dyadic inputs make the independent double oracle exact for this bounded arithmetic contract.
        const float work = 1f / (1 << 20);
        var balance = new FiniteEnergyBalance(14400, 0);
        for (var i = 0; i < 4096; i++)
        {
            balance = balance.Debit(work);
            balance.Validate(14400);
            var expected = 14400d - (i + 1) * (double)work;
            var actual = balance.Bulk + (double)balance.Remainder;
            Assert.True(actual <= expected);
            Assert.InRange(expected - actual, 0,
                FiniteEnergyBalance.LossBound(14400, 0, (i + 1) * work, 0, (uint)i + 1));
        }
        Assert.True(balance.Remainder >= 0);
        Assert.Equal(14400d - 1d / 256, balance.Bulk + (double)balance.Remainder);
    }

    [Fact]
    public void RemainderIsSharedByPaymentsAndCreditsAndResetHasCanonicalZeroRemainder()
    {
        var initial = new FiniteEnergyBalance(32, 0);
        const float small = 1f / (1 << 20);
        var first = initial.Debit(small);
        Assert.True(first.Remainder > 0);
        var second = first.Debit(small);
        Assert.Equal(32d - 2d * small, second.Bulk + (double)second.Remainder);
        var recharged = second.Credit(2 * small, 32);
        Assert.Equal(initial, recharged);
        Assert.Equal(initial, new FiniteEnergyBalance(32, 0));
        Assert.Equal(first, first.Debit(0));
        Assert.Throws<ArgumentException>(() => first.Credit(1, 32));
    }

    [Fact]
    public void SubnormalTransactionsRemainPayableAndCannotUnderflowIntoFreeWork()
    {
        var balance = new FiniteEnergyBalance(4 * float.Epsilon, 0);
        for (var i = 3; i >= 0; i--)
        {
            balance = balance.Debit(float.Epsilon);
            Assert.Equal(i * float.Epsilon, balance.Bulk);
            Assert.Equal(0, balance.Remainder);
        }
        Assert.Throws<ArgumentException>(() => balance.Debit(float.Epsilon));
        Assert.Equal(new FiniteEnergyBalance(float.Epsilon, 0), balance.Credit(float.Epsilon, float.Epsilon));
    }

    [Fact]
    public void MaximumFiniteAndTieCasesUseSafeConservativeOperations()
    {
        var maximum = new FiniteEnergyBalance(float.MaxValue, 0);
        maximum.Validate(float.MaxValue);
        var smallPayment = maximum.Debit(1);
        Assert.True(float.IsFinite(smallPayment.Bulk));
        Assert.True(float.IsFinite(smallPayment.Remainder));
        smallPayment.Validate(float.MaxValue);
        Assert.Equal(default, maximum.Debit(float.MaxValue));
        var halfUlp = 1f / (1 << 24);
        var tied = new FiniteEnergyBalance(1, 0).Credit(halfUlp, 2);
        Assert.Equal(1, tied.Bulk);
        Assert.Equal(halfUlp, tied.Remainder);
        Assert.Equal(new FiniteEnergyBalance(1, 0), tied.Debit(halfUlp));
    }

    [Fact]
    public void FullChargeComparisonDoesNotMistakeRoundedDisplayForFullBalance()
    {
        var balance = new FiniteEnergyBalance(14400, 0).Debit(1f / (1 << 20));
        Assert.Equal(14400, balance.Display);
        Assert.Equal(-1, balance.CompareTo(14400));
        Assert.True(balance.Headroom(14400) > 0);
        Assert.Equal(0, balance.Credit(balance.Headroom(14400), 14400).CompareTo(14400));
    }

    [Theory]
    [InlineData(float.NaN, 0f)]
    [InlineData(float.PositiveInfinity, 0f)]
    [InlineData(-1f, 0f)]
    [InlineData(0f, 1f)]
    [InlineData(1f, -1f)]
    [InlineData(1f, 1f)]
    public void InvalidBalancesAreRefused(float bulk, float remainder) =>
        Assert.Throws<ArgumentException>(() => new FiniteEnergyBalance(bulk, remainder).Validate(32));
}
