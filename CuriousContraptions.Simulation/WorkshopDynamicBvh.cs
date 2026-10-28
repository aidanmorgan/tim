using System;
using System.Numerics;

namespace CuriousContraptions.Simulation;

internal struct BvhNode
{
    public int Id;
    public int Parent;
    public int Left;
    public int Right;
    public Vector3 FatMin;
    public Vector3 FatMax;
    public int Height;
    /// <summary>The subtree holds a leaf of a moving body. A pair of subtrees without one can only produce static-static pairs.</summary>
    public bool HasDynamic;
}

internal sealed class WorkshopDynamicBvh
{
    // Story 9R.2: nodes live in an array read and written by reference (a list indexer copied the whole node on every read). The
    // insertion costs, refit and traversal order are unchanged, so the tree and the overlap order are the same for any scene.
    private BvhNode[] _nodes = new BvhNode[64];
    private int _count;
    private int _root = -1;

    public void Clear()
    {
        _count = 0;
        _root = -1;
    }

    public void Reset() { _nodes = new BvhNode[64]; Clear(); }

    private int AllocateNode()
    {
        if (_count + 1L > 3L * _nodes.Length / 4)
        {
            var required = checked(_count + 1);
            if (required > Array.MaxLength) throw new CuriousContraptions.Gpu.WorkshopStorageException("BVH demand exceeds representable array storage.");
            var capacity = (int)Math.Min(Array.MaxLength, Math.Max((4L * required + 2) / 3, 2L * _nodes.Length));
            if (capacity != _nodes.Length) Array.Resize(ref _nodes, capacity);
        }
        var id = _count++;
        _nodes[id] = new BvhNode { Id = -1, Parent = -1, Left = -1, Right = -1, FatMin = Vector3.Zero, FatMax = Vector3.Zero, Height = 0 };
        return id;
    }

    private static float Area(Vector3 min, Vector3 max)
    {
        var d = max - min;
        return 2f * (d.X * d.Y + d.Y * d.Z + d.Z * d.X);
    }

    public int InsertLeaf(int colliderIndex, Vector3 fatMin, Vector3 fatMax, bool dynamic)
    {
        var leaf = AllocateNode();
        ref var leafNode = ref _nodes[leaf];
        leafNode.Id = colliderIndex;
        leafNode.FatMin = fatMin;
        leafNode.FatMax = fatMax;
        leafNode.Height = 0;
        leafNode.HasDynamic = dynamic;

        if (_root == -1)
        {
            _root = leaf;
            return leaf;
        }

        var index = _root;
        while (_nodes[index].Left != -1)
        {
            ref readonly var node = ref _nodes[index];
            ref readonly var leftNode = ref _nodes[node.Left];
            ref readonly var rightNode = ref _nodes[node.Right];
            var area = Area(node.FatMin, node.FatMax);
            var combinedMin = Vector3.Min(node.FatMin, fatMin);
            var combinedMax = Vector3.Max(node.FatMax, fatMax);
            var combinedArea = Area(combinedMin, combinedMax);
            var cost = 2f * combinedArea;
            var inheritanceCost = 2f * (combinedArea - area);

            var leftCombMin = Vector3.Min(leftNode.FatMin, fatMin);
            var leftCombMax = Vector3.Max(leftNode.FatMax, fatMax);
            float costLeft;
            if (leftNode.Left == -1)
                costLeft = Area(leftCombMin, leftCombMax) + inheritanceCost;
            else
                costLeft = Area(leftCombMin, leftCombMax) - Area(leftNode.FatMin, leftNode.FatMax) + inheritanceCost;

            var rightCombMin = Vector3.Min(rightNode.FatMin, fatMin);
            var rightCombMax = Vector3.Max(rightNode.FatMax, fatMax);
            float costRight;
            if (rightNode.Left == -1)
                costRight = Area(rightCombMin, rightCombMax) + inheritanceCost;
            else
                costRight = Area(rightCombMin, rightCombMax) - Area(rightNode.FatMin, rightNode.FatMax) + inheritanceCost;

            if (cost < costLeft && cost < costRight) break;
            index = costLeft < costRight ? node.Left : node.Right;
        }

        var best = index;
        var oldParent = _nodes[best].Parent;
        var newParent = AllocateNode();
        ref var pNode = ref _nodes[newParent];
        pNode.Parent = oldParent;
        pNode.Left = best;
        pNode.Right = leaf;
        pNode.Height = _nodes[best].Height + 1;

        _nodes[best].Parent = newParent;
        _nodes[leaf].Parent = newParent;

        if (oldParent == -1)
        {
            _root = newParent;
        }
        else
        {
            ref var opNode = ref _nodes[oldParent];
            if (opNode.Left == best) opNode.Left = newParent;
            else opNode.Right = newParent;
        }

        var idx = newParent;
        while (idx != -1)
        {
            ref var curr = ref _nodes[idx];
            ref readonly var l = ref _nodes[curr.Left];
            ref readonly var r = ref _nodes[curr.Right];
            curr.FatMin = Vector3.Min(l.FatMin, r.FatMin);
            curr.FatMax = Vector3.Max(l.FatMax, r.FatMax);
            curr.Height = Math.Max(l.Height, r.Height) + 1;
            curr.HasDynamic = l.HasDynamic | r.HasDynamic;
            idx = curr.Parent;
        }

        return leaf;
    }

    /// <summary>Every overlapping leaf pair with at least one moving leaf, in traversal order. Subtree pairs holding only static
    /// leaves are skipped: every pair inside them is static-static, which no caller accepts.</summary>
    public void QueryOverlaps(Action<int, int> callback)
    {
        if (_root == -1) return;

        void CheckPair(int nodeA, int nodeB)
        {
            if (nodeA == nodeB) return;
            ref readonly var a = ref _nodes[nodeA];
            ref readonly var b = ref _nodes[nodeB];
            if (!a.HasDynamic && !b.HasDynamic) return;
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
            ref readonly var node = ref _nodes[index];
            if (node.Id != -1 || !node.HasDynamic) return;
            CheckPair(node.Left, node.Right);
            TraverseSelf(node.Left);
            TraverseSelf(node.Right);
        }

        TraverseSelf(_root);
    }
}
