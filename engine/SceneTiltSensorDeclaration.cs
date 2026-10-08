using CuriousContraptions.Physics;

namespace CuriousContraptions;

/// <summary>Construction binding of an orientation sensor to an owned body.</summary>
public readonly record struct SceneTiltSensorDeclaration(SceneBodyKey Body,CollisionVector LocalDirection,double ThresholdCosine);
