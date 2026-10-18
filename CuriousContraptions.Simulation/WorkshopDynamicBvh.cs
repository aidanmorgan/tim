using System;
using System.Collections.Generic;
using System.Numerics;

namespace CuriousContraptions.Simulation;

public struct BvhNode
{
    public int Id;
    public int Parent;
    public int Left;
    public int Right;
    public Vector3 FatMin;
    public Vector3 FatMax;
    public int Height;
}

public sealed class WorkshopDynamicBvh
{
    private readonly List<BvhNode> _nodes = new(64);
    private int _root = -1;

    public void Clear()
    {
        _nodes.Clear();
        _root = -1;
    }

    private int AllocateNode()
    {
        var id = _nodes.Count;
        _nodes.Add(new BvhNode
        {
            Id = -1,
            Parent = -1,
            Left = -1,
            Right = -1,
            FatMin = Vector3.Zero,
            FatMax = Vector3.Zero,
            Height = 0
        });
        return id;
    }

    private static float Area(Vector3 min, Vector3 max)
    {
        var d = max - min;
        return 2f * (d.X * d.Y + d.Y * d.Z + d.Z * d.X);
    }

    public int InsertLeaf(int colliderIndex, Vector3 fatMin, Vector3 fatMax)
    {
        var leaf = AllocateNode();
        var leafNode = _nodes[leaf];
        leafNode.Id = colliderIndex;
        leafNode.FatMin = fatMin;
        leafNode.FatMax = fatMax;
        leafNode.Height = 0;
        _nodes[leaf] = leafNode;

        if (_root == -1)
        {
            _root = leaf;
            return leaf;
        }

        var index = _root;
        while (_nodes[index].Left != -1)
        {
            var left = _nodes[index].Left;
            var right = _nodes[index].Right;
            var area = Area(_nodes[index].FatMin, _nodes[index].FatMax);
            var combinedMin = Vector3.Min(_nodes[index].FatMin, fatMin);
            var combinedMax = Vector3.Max(_nodes[index].FatMax, fatMax);
            var combinedArea = Area(combinedMin, combinedMax);
            var cost = 2f * combinedArea;
            var inheritanceCost = 2f * (combinedArea - area);

            var leftCombMin = Vector3.Min(_nodes[left].FatMin, fatMin);
            var leftCombMax = Vector3.Max(_nodes[left].FatMax, fatMax);
            float costLeft;
            if (_nodes[left].Left == -1)
                costLeft = Area(leftCombMin, leftCombMax) + inheritanceCost;
            else
                costLeft = Area(leftCombMin, leftCombMax) - Area(_nodes[left].FatMin, _nodes[left].FatMax) + inheritanceCost;

            var rightCombMin = Vector3.Min(_nodes[right].FatMin, fatMin);
            var rightCombMax = Vector3.Max(_nodes[right].FatMax, fatMax);
            float costRight;
            if (_nodes[right].Left == -1)
                costRight = Area(rightCombMin, rightCombMax) + inheritanceCost;
            else
                costRight = Area(rightCombMin, rightCombMax) - Area(_nodes[right].FatMin, _nodes[right].FatMax) + inheritanceCost;

            if (cost < costLeft && cost < costRight) break;
            index = costLeft < costRight ? left : right;
        }

        var best = index;
        var oldParent = _nodes[best].Parent;
        var newParent = AllocateNode();
        var pNode = _nodes[newParent];
        pNode.Parent = oldParent;
        pNode.Left = best;
        pNode.Right = leaf;
        pNode.Height = _nodes[best].Height + 1;
        _nodes[newParent] = pNode;

        var bestNode = _nodes[best];
        bestNode.Parent = newParent;
        _nodes[best] = bestNode;

        leafNode = _nodes[leaf];
        leafNode.Parent = newParent;
        _nodes[leaf] = leafNode;

        if (oldParent == -1)
        {
            _root = newParent;
        }
        else
        {
            var opNode = _nodes[oldParent];
            if (opNode.Left == best) opNode.Left = newParent;
            else opNode.Right = newParent;
            _nodes[oldParent] = opNode;
        }

        var idx = newParent;
        while (idx != -1)
        {
            var curr = _nodes[idx];
            var l = _nodes[curr.Left];
            var r = _nodes[curr.Right];
            curr.FatMin = Vector3.Min(l.FatMin, r.FatMin);
            curr.FatMax = Vector3.Max(l.FatMax, r.FatMax);
            curr.Height = Math.Max(l.Height, r.Height) + 1;
            _nodes[idx] = curr;
            idx = curr.Parent;
        }

        return leaf;
    }

    public void QueryOverlaps(Action<int, int> callback)
    {
        if (_root == -1) return;

        void CheckPair(int nodeA, int nodeB)
        {
            if (nodeA == nodeB) return;
            var a = _nodes[nodeA];
            var b = _nodes[nodeB];
            if (a.FatMin.X > b.FatMax.X || a.FatMax.X < b.FatMin.X ||
                a.FatMin.Y > b.FatMax.Y || a.FatMax.Y < b.FatMin.Y ||
                a.FatMin.Z > b.FatMax.Z || a.FatMax.Z < b.FatMin.Z)
            {
                return;
            }
            if (a.Id != -1 && b.Id != -1)
            {
                if (a.Id < b.Id) callback(a.Id, b.Id);
                else callback(b.Id, a.Id);
                return;
            }
            if (a.Id == -1 && (b.Id != -1 || a.Height >= b.Height))
            {
                CheckPair(a.Left, nodeB);
                CheckPair(a.Right, nodeB);
            }
            else
            {
                CheckPair(nodeA, b.Left);
                CheckPair(nodeA, b.Right);
            }
        }

        void TraverseSelf(int index)
        {
            var node = _nodes[index];
            if (node.Id != -1) return;
            CheckPair(node.Left, node.Right);
            TraverseSelf(node.Left);
            TraverseSelf(node.Right);
        }

        TraverseSelf(_root);
    }
}
