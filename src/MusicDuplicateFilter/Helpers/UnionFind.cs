namespace MusicDuplicateFilter.Helpers;

/// <summary>
/// 并查集（Union-Find）数据结构，用于文件分组
/// </summary>
public sealed class UnionFind
{
    private readonly int[] _parent;
    private readonly int[] _rank;

    public UnionFind(int n)
    {
        _parent = new int[n];
        _rank = new int[n];
        for (var i = 0; i < n; i++)
            _parent[i] = i;
    }

    /// <summary>带路径压缩的查找操作</summary>
    public int Find(int x)
    {
        while (_parent[x] != x)
        {
            _parent[x] = _parent[_parent[x]]; // 路径压缩（两步跳）
            x = _parent[x];
        }
        return x;
    }

    /// <summary>按秩合并</summary>
    public void Union(int x, int y)
    {
        var rx = Find(x);
        var ry = Find(y);
        if (rx == ry) return;

        if (_rank[rx] < _rank[ry]) (rx, ry) = (ry, rx);
        _parent[ry] = rx;
        if (_rank[rx] == _rank[ry]) _rank[rx]++;
    }

    /// <summary>判断两个元素是否已连通</summary>
    public bool Connected(int x, int y) => Find(x) == Find(y);
}
