namespace CuriousContraptions;

/// <summary>Construction binding of a passive aperture sensor to its owned rigid frame.</summary>
public readonly record struct ScenePassageSensorDeclaration(SceneBodyKey Frame,double Radius,double RearmClearance);
