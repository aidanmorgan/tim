namespace CuriousContraptions;

public readonly record struct SceneLatchedSpringDeclaration(SceneJointKey Guide,SceneJointKey Transmission,
    double Stiffness,double Stroke);
