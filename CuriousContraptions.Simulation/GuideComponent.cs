using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal readonly record struct GuideEquation(SimJoint Joint, int Row);

/// <summary>A connected dynamic component's finite mass-whitened guide response and reactions.</summary>
internal sealed class GuideComponent
{
    public readonly List<SimBody> Bodies = new();
    public readonly List<GuideEquation> Equations = new();
    public int Generation;
    private readonly Dictionary<SimBody, int> _indices = new();
    private readonly List<GuideVector> _columns = new();
    private readonly List<GuideVector> _columnsPool = new();
    private readonly List<GuideVector> _original = new();
    private readonly List<GuideVector> _originalPool = new();
    private readonly List<GuideVector> _u = new();
    private readonly List<GuideVector> _triangular = new();
    private readonly GuideQr _q = new();
    private readonly GuideVector _left = new(), _right = new(), _coordinates = new(), _reaction = new();
    private readonly List<float> _scales = new();
    private readonly Dictionary<int, GuidePreparedRow> _prepared = new();
    private readonly List<GuidePreparedRow> _preparedPool = new();
    private int _preparedCount;

    public void Clear()
    {
        Bodies.Clear(); Equations.Clear(); _indices.Clear(); _columns.Clear(); _original.Clear(); _scales.Clear();
        _reaction.Clear();
        _prepared.Clear(); _preparedCount = 0;
        Generation++;
    }

    public void AddBody(SimBody body)
    {
        if (body.Motion != RigidMotionKind.Dynamic || _indices.ContainsKey(body)) return;
        _indices.Add(body, Bodies.Count);
        Bodies.Add(body);
        body.Guide = this;
    }

    public void Prepare(bool useBias, bool clearReactions)
    {
        GuideQr.Ensure(_columnsPool, Equations.Count);
        GuideQr.Ensure(_originalPool, Equations.Count);
        for (var i = 0; i < Equations.Count; i++)
        {
            var equation = Equations[i];
            var joint = equation.Joint;
            ref readonly var row = ref joint.Rows[equation.Row];
            var column = _columnsPool[i];
            column.Clear();
            AddWrench(column, joint.A, row.La, row.Aa);
            AddWrench(column, joint.B, row.Lb, row.Ab);
            _originalPool[i].Copy(column);
            _original.Add(_originalPool[i]);
            var scale = column.Norm();
            _scales.Add(scale);
            if (scale > 0f) column.Scale(1f / scale);
            _columns.Add(column);
            if (clearReactions) joint.Impulses[equation.Row] = 0f;
        }
        _q.Factor(_columns);
        FactorReactionMap();
        CorrectVelocity(useBias);
    }

    // QR of A transpose in fixed Q-coordinate order. A retains every original equation.
    private void FactorReactionMap()
    {
        GuideQr.Ensure(_u, _q.Rank);
        GuideQr.Ensure(_triangular, _q.Rank);
        for (var j = 0; j < _q.Rank; j++)
        {
            var column = _u[j]; column.Clear();
            var triangle = _triangular[j]; triangle.Clear();
            for (var i = 0; i < _columns.Count; i++) column.Set(i, _q.Basis(j).Dot(_columns[i]));
            for (var pass = 0; pass < 2; pass++)
                for (var i = 0; i < j; i++)
                {
                    var coefficient = _u[i].Dot(column);
                    triangle.Set(i, triangle[i] + coefficient);
                    column.Add(_u[i], -coefficient);
                }
            var norm = column.Norm();
            triangle.Set(j, norm);
            column.Scale(1f / norm);
        }
    }

    private void CorrectVelocity(bool useBias)
    {
        _left.Clear();
        for (var i = 0; i < Equations.Count; i++)
        {
            var equation = Equations[i];
            var joint = equation.Joint;
            ref readonly var row = ref joint.Rows[equation.Row];
            var velocity = ConstraintBodyImpulse.ConstraintVelocity(joint.A, row.La, row.Aa)
                + ConstraintBodyImpulse.ConstraintVelocity(joint.B, row.Lb, row.Ab);
            var target = row.VelocityTarget - (useBias ? row.Bias * row.Error : 0f);
            if (_scales[i] > 0f) _left.Set(i, (target - velocity) / _scales[i]);
        }
        _coordinates.Clear();
        for (var i = 0; i < _q.Rank; i++) _coordinates.Set(i, _u[i].Dot(_left));
        for (var i = _q.Rank - 1; i >= 0; i--)
        {
            var value = _coordinates[i];
            for (var j = i + 1; j < _q.Rank; j++) value -= _triangular[j][i] * _coordinates[j];
            _coordinates.Set(i, value / _triangular[i][i]);
        }
        _left.Clear();
        for (var i = 0; i < _q.Rank; i++) _left.Add(_q.Basis(i), _coordinates[i]);
        var scale = CorrectionScale(_left);
        if (!(scale > 0f)) return;
        ApplyVelocity(_left, scale);
        AccountReaction(_coordinates, scale);
    }

    private float CorrectionScale(GuideVector correction)
    {
        var scale = 1f;
        foreach (var body in Bodies)
        {
            ReadVelocity(correction, body, out var linear, out var angular);
            var v = linear.Length(); var w = angular.Length();
            if (!float.IsFinite(v) || !float.IsFinite(w)) return 0f;
            scale = MathF.Min(scale, VelocityScale(body.LinearVelocity, linear, v, WorkshopConstraintSolver.MaxLinearSpeed));
            scale = MathF.Min(scale, VelocityScale(body.AngularVelocity, angular, w, WorkshopConstraintSolver.MaxAngularSpeed));
        }
        return scale;
    }

    private static float VelocityScale(Vector3 velocity, Vector3 correction, float length, float limit)
    {
        if (length == 0f) return 1f;
        var along = Vector3.Dot(velocity, correction / length);
        var perpendicular = MathF.Max(0f, velocity.LengthSquared() - along * along);
        var available = limit * limit - perpendicular;
        if (available < 0f) return 0f;
        return Math.Clamp((MathF.Sqrt(available) - along) / length, 0f, 1f);
    }

    public void FlushReactions()
    {
        AccountReaction(_reaction, 1f);
        _reaction.Clear();
    }

    public float Mass(in ImpulseJacobian a, in ImpulseJacobian b) =>
        Prepared(a).Projected.Dot(Prepared(b).Projected);

    public void Apply(in ImpulseJacobian row, float impulse)
    {
        if (impulse == 0f) return;
        var prepared = Prepared(row);
        ApplyVelocity(prepared.Response, impulse);
        _reaction.Add(prepared.Removed, -impulse);
    }

    private GuidePreparedRow Prepared(in ImpulseJacobian row)
    {
        var key = GuidePreparedRow.Key(row);
        _prepared.TryGetValue(key, out var head);
        for (var current = head; current is not null; current = current.Next)
            if (current.Matches(row)) return current;
        if (_preparedCount == _preparedPool.Count) _preparedPool.Add(new GuidePreparedRow());
        var prepared = _preparedPool[_preparedCount++];
        prepared.Capture(row);
        Gather(prepared.Projected, row);
        _q.ProjectResponse(prepared.Projected, prepared.Response, prepared.Removed, _original);
        prepared.Next = head;
        _prepared[key] = prepared;
        return prepared;
    }

    private void Gather(GuideVector vector, in ImpulseJacobian row)
    {
        vector.Clear();
        for (var i = 0; i < row.Count; i++)
        {
            row.Get(i, out var body, out var linear, out var angular);
            if (body.Guide == this) AddWrench(vector, body, linear, angular);
        }
    }

    private void AddWrench(GuideVector vector, SimBody body, Vector3 linear, Vector3 angular)
    {
        if (!_indices.TryGetValue(body, out var index)) return;
        var start = index * 6;
        var mass = MathF.Sqrt(body.InvMass);
        vector.Set(start, vector[start] + mass * linear.X);
        vector.Set(start + 1, vector[start + 1] + mass * linear.Y);
        vector.Set(start + 2, vector[start + 2] + mass * linear.Z);
        vector.Set(start + 3, vector[start + 3] + MathF.Sqrt(body.InvI.X) * Vector3.Dot(body.PrincipalX, angular));
        vector.Set(start + 4, vector[start + 4] + MathF.Sqrt(body.InvI.Y) * Vector3.Dot(body.PrincipalY, angular));
        vector.Set(start + 5, vector[start + 5] + MathF.Sqrt(body.InvI.Z) * Vector3.Dot(body.PrincipalZ, angular));
    }

    private void ReadVelocity(GuideVector vector, SimBody body, out Vector3 linear, out Vector3 angular)
    {
        var start = _indices[body] * 6;
        linear = new Vector3(vector[start], vector[start + 1], vector[start + 2]) * MathF.Sqrt(body.InvMass);
        angular = body.PrincipalX * (vector[start + 3] * MathF.Sqrt(body.InvI.X))
            + body.PrincipalY * (vector[start + 4] * MathF.Sqrt(body.InvI.Y))
            + body.PrincipalZ * (vector[start + 5] * MathF.Sqrt(body.InvI.Z));
    }

    private void ApplyVelocity(GuideVector vector, float impulse)
    {
        foreach (var body in Bodies)
        {
            ReadVelocity(vector, body, out var linear, out var angular);
            body.Vx += linear.X * impulse; body.Vy += linear.Y * impulse; body.Vz += linear.Z * impulse;
            body.Wx += angular.X * impulse; body.Wy += angular.Y * impulse; body.Wz += angular.Z * impulse;
        }
    }

    private void AccountReaction(GuideVector coordinates, float scale)
    {
        _right.Clear();
        for (var i = 0; i < _q.Rank; i++)
        {
            var value = coordinates[i];
            for (var j = 0; j < i; j++) value -= _triangular[i][j] * _right[j];
            _right.Set(i, value / _triangular[i][i]);
        }
        for (var i = 0; i < Equations.Count; i++)
        {
            if (_scales[i] == 0f) continue;
            var reaction = 0f;
            for (var j = 0; j < _q.Rank; j++) reaction += _u[j][i] * _right[j];
            var equation = Equations[i];
            equation.Joint.Impulses[equation.Row] += scale * reaction / _scales[i];
        }
    }
}
