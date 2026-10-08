namespace CuriousContraptions;

public readonly record struct SceneServoDeclaration(SceneJointKey Joint,double MaximumSpeed,
    double Acceleration,double MaximumEffort,double MaximumPower);
