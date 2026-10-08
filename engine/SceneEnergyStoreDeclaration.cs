namespace CuriousContraptions;

/// <summary>Construction-only binding of a finite reservoir to a declared body.
/// The runtime stores only the shared world's typed body identity.</summary>
public readonly record struct SceneEnergyStoreDeclaration(SceneBodyKey Body,double Capacity,double InitialEnergy);
