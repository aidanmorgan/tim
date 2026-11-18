using System;
using System.Collections.Generic;

namespace CuriousContraptions.Simulation;

/// <summary>Actual nonzero fill for mass-whitened guide factorization.</summary>
internal sealed class GuideVector
{
    private readonly SortedList<int, float> _values = new();
    public SortedList<int, float> Values => _values;
    public float this[int index] => _values.TryGetValue(index, out var value) ? value : 0f;
    public void Clear() => _values.Clear();
    public void Set(int index, float value)
    {
        if (value == 0f) _values.Remove(index);
        else _values[index] = value;
    }
    public void Copy(GuideVector source)
    {
        Clear();
        foreach (var pair in source.Values) Set(pair.Key, pair.Value);
    }
    public void Add(GuideVector source, float scale)
    {
        if (scale == 0f) return;
        foreach (var pair in source.Values) Set(pair.Key, this[pair.Key] + scale * pair.Value);
    }
    public void Scale(float scale)
    {
        for (var i = 0; i < _values.Count; i++) _values.SetValueAtIndex(i, _values.GetValueAtIndex(i) * scale);
    }
    public float Dot(GuideVector other)
    {
        var result = 0f;
        foreach (var pair in Values) result += pair.Value * other[pair.Key];
        return result;
    }
    public float Norm()
    {
        var scale = 0f;
        foreach (var pair in Values) scale = MathF.Max(scale, MathF.Abs(pair.Value));
        if (scale == 0f) return 0f;
        var sum = 0f;
        foreach (var pair in Values) { var value = pair.Value / scale; sum += value * value; }
        return scale * MathF.Sqrt(sum);
    }
}

/// <summary>Reusable sparse thin QR. Pivoting is deterministic; all input columns remain represented.</summary>
internal sealed class GuideQr
{
    // Local rank contract for normalized f32 vectors, independent of gameplay position tolerances.
    private const float RankTolerance = 64f * 1.1920929e-7f;
    private readonly List<GuideVector> _basis = new();
    private readonly List<GuideVector> _remaining = new();
    public int Rank { get; private set; }
    public GuideVector Basis(int index) => _basis[index];

    public void Factor(IReadOnlyList<GuideVector> columns)
    {
        Rank = 0;
        Ensure(_remaining, columns.Count);
        for (var i = 0; i < columns.Count; i++) _remaining[i].Copy(columns[i]);
        while (Rank < columns.Count)
        {
            var pivot = -1;
            var largest = RankTolerance;
            for (var i = 0; i < columns.Count; i++)
            {
                var norm = _remaining[i].Norm();
                if (norm > largest) { largest = norm; pivot = i; }
            }
            if (pivot < 0) break;
            Ensure(_basis, Rank + 1);
            var next = _basis[Rank];
            next.Copy(_remaining[pivot]);
            next.Scale(1f / largest);
            Rank++;
            for (var i = 0; i < columns.Count; i++)
                for (var pass = 0; pass < 2; pass++)
                    _remaining[i].Add(next, -next.Dot(_remaining[i]));
        }
    }

    // Evaluate known algebraic zeros without discarding genuinely weak free directions.
    public void ProjectResponse(GuideVector vector, GuideVector response, GuideVector removed,
        IReadOnlyList<GuideVector> original)
    {
        var redundant = vector.Norm() == 0f;
        foreach (var column in original)
            redundant |= SignedCopy(vector, column, 1f) || SignedCopy(vector, column, -1f);
        removed.Clear();
        Project(vector, removed);
        response.Copy(vector);
        ProjectTranspose(response, removed);
        if (redundant) { vector.Clear(); response.Clear(); }
    }

    private static bool SignedCopy(GuideVector vector, GuideVector column, float sign)
    {
        foreach (var pair in vector.Values)
            if (!float.IsFinite(pair.Value) || pair.Value != sign * column[pair.Key]) return false;
        foreach (var pair in column.Values)
            if (!float.IsFinite(pair.Value) || vector[pair.Key] != sign * pair.Value) return false;
        return true;
    }

    public void Project(GuideVector vector, GuideVector? removed = null)
    {
        for (var pass = 0; pass < 2; pass++)
            for (var i = 0; i < Rank; i++)
            {
                var amount = _basis[i].Dot(vector);
                vector.Add(_basis[i], -amount);
                removed?.Set(i, removed[i] + amount);
            }
    }

    public void ProjectTranspose(GuideVector vector, GuideVector? removed = null)
    {
        for (var pass = 0; pass < 2; pass++)
            for (var i = Rank - 1; i >= 0; i--)
            {
                var amount = _basis[i].Dot(vector);
                vector.Add(_basis[i], -amount);
                removed?.Set(i, removed[i] + amount);
            }
    }

    public static void Ensure(List<GuideVector> vectors, int count)
    {
        while (vectors.Count < count) vectors.Add(new GuideVector());
    }
}
