using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace CuriousContraptions;

/// <summary>Owned enum-keyed configuration. Strings exist only at import/export boundaries.</summary>
public abstract class PartParameterValues
{
    private PartParameterValues() { }
    public static PartParameterValues Empty { get; } = new EmptyValues();
    public static PartParameterValues BindEmpty(IReadOnlyDictionary<string,float> fields)
    {
        if(fields.Count!=0) throw new ArgumentException("This part declares no numeric parameters.",nameof(fields));
        return Empty;
    }
    public static PartParameterValues Bind<T>(IReadOnlyDictionary<string,float> fields) where T:struct,Enum
    {
        var values=new Dictionary<T,float>();
        foreach(var parameter in Enum.GetValues<T>()) values.Add(parameter,fields[PartParameterName.Of(parameter)]);
        PartParameterName.RequireExact<T>(fields.Keys);
        return new TypedValues<T>(values);
    }
    public float Read<T>(T parameter) where T:struct,Enum => Require<T>().Read(parameter);
    internal void Write<T>(T parameter,float value) where T:struct,Enum => Require<T>().Write(parameter,value);
    private TypedValues<T> Require<T>() where T:struct,Enum =>
        this as TypedValues<T> ?? throw new ArgumentException("Parameter enum does not match this part's schema.");
    public abstract IReadOnlyDictionary<string,float> Export();
    private sealed class EmptyValues : PartParameterValues
    {
        public override IReadOnlyDictionary<string,float> Export() =>
            new ReadOnlyDictionary<string,float>(new Dictionary<string,float>());
    }
    private sealed class TypedValues<T>(Dictionary<T,float> values) : PartParameterValues where T:struct,Enum
    {
        internal float Read(T parameter) => values.TryGetValue(parameter,out var value)
            ? value : throw new ArgumentOutOfRangeException(nameof(parameter));
        internal void Write(T parameter,float value)
        {
            if(!values.ContainsKey(parameter)) throw new ArgumentOutOfRangeException(nameof(parameter));
            values[parameter]=value;
        }
        public override IReadOnlyDictionary<string,float> Export() =>
            new ReadOnlyDictionary<string,float>(values.ToDictionary(pair=>PartParameterName.Of(pair.Key),pair=>pair.Value));
    }
}
