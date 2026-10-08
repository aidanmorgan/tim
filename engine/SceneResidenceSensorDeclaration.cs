using CuriousContraptions.Physics;

namespace CuriousContraptions;

public readonly record struct SceneResidenceSensorDeclaration(SceneBodyKey Frame,SceneBodyKey Body,
    CollisionBounds Region,double MaximumSpeed,double Dwell);
