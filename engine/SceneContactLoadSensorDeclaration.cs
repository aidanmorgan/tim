using CuriousContraptions.Physics;

namespace CuriousContraptions;

public readonly record struct SceneContactLoadSensorDeclaration(SceneBodyKey Frame,CollisionBounds Region,
    CollisionVector LocalNormal,double MinimumAlignment,double MinimumMass);
