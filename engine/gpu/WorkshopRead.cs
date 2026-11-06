namespace CuriousContraptions.Gpu;

public static class WorkshopReadDiagnostics
{
    /// <summary>The commit moved energy: a store was credited or a source debited. Story 9A.23 D9: it gates only the existing
    /// typed BalanceTransfer diagnostic; no read is reliable or retained, since every
    /// one-shot occurrence (Impact, ElectricalPowered) reaches its consumers through the event ring.</summary>
    public static bool CarriesBalanceTransfer(this WorkshopRead read)
    {
        for (var i = 0; i < read.ContactWorks.Count; i++) if (read.ContactWorks[i].SuppliedEnergy.Value > 0) return true;
        for (var i = 0; i < read.Electrical.Count; i++) if (read.Electrical[i].Debit.Value > 0) return true;
        return false;
    }

}
