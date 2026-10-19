using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Asserts a typed edit refusal and the rule that refused it: most validators refuse for several reasons, so
/// a type-only assertion could pass on the wrong rule.</summary>
internal static class EditRefusal
{
    public static WorkshopEditRejectedException Is(WorkshopEditRejection reason, Action edit)
    {
        var rejection = Assert.Throws<WorkshopEditRejectedException>(edit);
        Assert.Equal(reason, rejection.Reason);
        return rejection;
    }
}
