using CuriousContraptions.Abstractions;
using System;

namespace CuriousContraptions.Presentation;

public sealed class AnimationServices : IAnimationServices
{
    public IAnimationBatch CreateBatch() => new AnimationBatch();
}
