using System;

namespace CuriousContraptions.Simulation;

/// <summary>Pivoted elimination for a finite square system. Inputs are disposable correction scratch.</summary>
internal static class DenseLinearSystem
{
    public static bool Solve(Span<float> matrix, Span<float> rhs, Span<float> solution, float minimumPivot = 0)
    {
        var count = rhs.Length;
        if (matrix.Length != checked(count * count) || solution.Length != count) throw new ArgumentException("Invalid square system.");
        for (var column = 0; column < count; column++)
        {
            var pivot = column;
            for (var row = column + 1; row < count; row++)
                if (MathF.Abs(matrix[row * count + column]) > MathF.Abs(matrix[pivot * count + column])) pivot = row;
            var diagonal = matrix[pivot * count + column];
            if (diagonal == 0 || !float.IsFinite(diagonal) || MathF.Abs(diagonal) < minimumPivot) return false;
            if (pivot != column)
            {
                for (var k = 0; k < count; k++)
                    (matrix[column * count + k], matrix[pivot * count + k]) = (matrix[pivot * count + k], matrix[column * count + k]);
                (rhs[column], rhs[pivot]) = (rhs[pivot], rhs[column]);
            }
            for (var row = column + 1; row < count; row++)
            {
                var factor = matrix[row * count + column] / matrix[column * count + column];
                for (var k = column; k < count; k++) matrix[row * count + k] -= factor * matrix[column * count + k];
                rhs[row] -= factor * rhs[column];
            }
        }
        for (var row = count - 1; row >= 0; row--)
        {
            var value = rhs[row];
            for (var k = row + 1; k < count; k++) value -= matrix[row * count + k] * solution[k];
            solution[row] = value / matrix[row * count + row];
            if (!float.IsFinite(solution[row])) return false;
        }
        return true;
    }
}
