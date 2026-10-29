using System;
using System.Globalization;
using System.Runtime.InteropServices.JavaScript;

namespace CuriousContraptions.Gpu;

/// <summary>One external projection boundary: exact identifiers are decimal strings, enums are integers and f32 values numbers.</summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
internal static partial class BrowserWorkshopObservation
{
    [JSImport("registerObservation", "workshopObservation")]
    private static partial void Register(string session,
        [JSMarshalAs<JSType.Function<JSType.Number, JSType.Object>>] Func<int, JSObject> query,
        [JSMarshalAs<JSType.Function<JSType.String, JSType.Object>>] Func<string, JSObject> receipts,
        [JSMarshalAs<JSType.Function<JSType.String, JSType.Object>>] Func<string, JSObject> diagnostics,
        [JSMarshalAs<JSType.Function<JSType.Object>>] Func<JSObject> open,
        [JSMarshalAs<JSType.Function<JSType.String, JSType.Object>>] Func<string, JSObject> read,
        [JSMarshalAs<JSType.Function<JSType.String, JSType.Object>>] Func<string, JSObject> close,
        [JSMarshalAs<JSType.Function<JSType.Object>>] Func<JSObject> terminal);
    [JSImport("observationObject", "workshopObservation")] private static partial JSObject Object();
    [JSImport("observationArray", "workshopObservation")] private static partial JSObject Array();
    [JSImport("observationPush", "workshopObservation")] private static partial void Push(JSObject array, JSObject value);

    internal static async System.Threading.Tasks.Task Start(IWorkshopObservation port)
    {
        using var document = JSHost.GlobalThis.GetPropertyAsJSObject("document")
            ?? throw new InvalidOperationException("A browser document is required.");
        var baseUri = document.GetPropertyAsString("baseURI")
            ?? throw new InvalidOperationException("The document resource origin is unavailable.");
        await JSHost.ImportAsync("workshopObservation", new Uri(new Uri(baseUri), "workshop-observation.js").AbsoluteUri);
        Install(port);
    }

    internal static void Install(IWorkshopObservation port) => Register(
        Exact(port.Session.Low) + ":" + Exact(port.Session.High),
        kind => Query(port, kind), after => Receipts(port, after), after => Diagnostics(port, after),
        () => { var result = port.OpenEvents(port.Session); var dto = Result(result.Refusal);
            if (result.Refusal == WorkshopObservationRefusal.None) dto.SetProperty("value", Exact(result.Value.Value));
            return dto; },
        cursor => Events(port, cursor),
        cursor => Result(ulong.TryParse(cursor, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? port.CloseEvents(new(port.Session, id)) : WorkshopObservationRefusal.InvalidCursor),
        () => { var dto = Result(WorkshopObservationRefusal.None); if (port.Terminal is { } terminal) dto.SetProperty("value", terminal.Detail); return dto; });

    private static string Exact(ulong value) => value.ToString(CultureInfo.InvariantCulture);
    private static JSObject Result(WorkshopObservationRefusal refusal)
    { var value = Object(); value.SetProperty("refusal", WorkshopObservationProjection.EnumValue(refusal)); return value; }

    private static JSObject Query(IWorkshopObservation port, int kind)
    {
        var result = port.Query(port.Session, (WorkshopObservationQuery)kind);
        var dto = Result(result.Refusal);
        if (result.Refusal != WorkshopObservationRefusal.None) return dto;
        using var value = WorkshopObservationProjection.Response<BrowserObject>(result.Value).Value;
        dto.SetProperty("value", value);
        return dto;
    }

    private static JSObject Diagnostics(IWorkshopObservation port, string after)
    {
        if (!ulong.TryParse(after, NumberStyles.None, CultureInfo.InvariantCulture, out var ordinal))
            return Result(WorkshopObservationRefusal.InvalidCursor);
        var result = port.Diagnostics(port.Session, new(ordinal)); var dto = Result(result.Refusal);
        if (result.Refusal != WorkshopObservationRefusal.None) return dto;
        using var values = Array();
        foreach (var receipt in result.Value!)
        {
            using var row = Object(); using var record = WorkshopObservationProjection.Diagnostic<BrowserObject>(receipt.Record).Value;
            row.SetProperty("ordinal", Exact(receipt.Ordinal.Value)); row.SetProperty("record", record); Push(values, row);
        }
        dto.SetProperty("value", values); return dto;
    }


    private sealed class BrowserObject(JSObject value) : IWorkshopObservationObject<BrowserObject>
    {
        internal JSObject Value { get; } = value;
        public static BrowserObject Object() => new(BrowserWorkshopObservation.Object());
        public static BrowserObject Array() => new(BrowserWorkshopObservation.Array());
        public void Push(BrowserObject item) => BrowserWorkshopObservation.Push(Value, item.Value);
        public void SetProperty(string name, BrowserObject item) => Value.SetProperty(name, item.Value);
        public void SetProperty(string name, string item) => Value.SetProperty(name, item);
        public void SetProperty(string name, bool item) => Value.SetProperty(name, item);
        public void SetProperty(string name, int item) => Value.SetProperty(name, item);
        public void SetProperty(string name, uint item) => Value.SetProperty(name, item);
        public void SetProperty(string name, float item) => Value.SetProperty(name, item);
        public void SetMilliseconds(double item) => Value.SetProperty("milliseconds", item);
        public void Dispose() => Value.Dispose();
    }

    private static JSObject Receipts(IWorkshopObservation port, string after)
    {
        if (!ulong.TryParse(after, NumberStyles.None, CultureInfo.InvariantCulture, out var ordinal))
            return Result(WorkshopObservationRefusal.InvalidCursor);
        var result = port.Receipts(port.Session, new(ordinal)); var dto = Result(result.Refusal);
        if (result.Refusal != WorkshopObservationRefusal.None) return dto;
        using var values = Array();
        foreach (var receipt in result.Value!)
        {
            using var row = Object();
            row.SetProperty("ordinal", Exact(receipt.Ordinal.Value)); row.SetProperty("sequence", Exact(receipt.Sequence.Value));
            row.SetProperty("kind", WorkshopObservationProjection.EnumValue(receipt.Kind)); row.SetProperty("outcome", WorkshopObservationProjection.EnumValue(receipt.Result.Outcome));
            row.SetProperty("reason", WorkshopObservationProjection.EnumValue(receipt.Result.Reason)); row.SetProperty("appliedToPresentation", receipt.AppliedToPresentation);
            Push(values, row);
        }
        dto.SetProperty("value", values); return dto;
    }

    private static JSObject Events(IWorkshopObservation port, string cursor)
    {
        if (!ulong.TryParse(cursor, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            return Result(WorkshopObservationRefusal.InvalidCursor);
        var result = port.ReadEvents(new(port.Session, id)); var dto = Result(result.Refusal);
        if (result.Refusal != WorkshopObservationRefusal.None) return dto;
        using var value = Object(); using var records = Array();
        value.SetProperty("epoch", Exact(result.Value!.Epoch)); value.SetProperty("skipped", Exact(result.Value.Skipped));
        foreach (var record in result.Value.Records)
        {
            using var row = Object();
            row.SetProperty("sequence", Exact(record.Sequence)); row.SetProperty("tick", Exact(record.Tick));
            row.SetProperty("kind", WorkshopObservationProjection.EnumValue(record.Kind)); row.SetProperty("domain", WorkshopObservationProjection.EnumValue(record.Domain));
            row.SetProperty("source", Exact(record.Source)); row.SetProperty("other", Exact(record.Other));
            row.SetProperty("ordinal", record.Ordinal); row.SetProperty("phase", record.Phase);
            row.SetProperty("magnitude", record.Magnitude); row.SetProperty("count", record.Count);
            row.SetProperty("level", record.Level); Push(records, row);
        }
        value.SetProperty("records", records); dto.SetProperty("value", value); return dto;
    }
}
