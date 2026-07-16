// ──────────────────────────────────────────────
// TheSprouty | Scripts/Save/SerializableTypes.cs
// Helper structs that make Unity types serializable by JsonUtility.
// ──────────────────────────────────────────────
using System;
using UnityEngine;

/// <summary>JsonUtility-serializable version of Vector3.</summary>
[Serializable]
public struct SerializableVector3
{
    public float x, y, z;

    public SerializableVector3(float x, float y, float z)
    {
        this.x = x; this.y = y; this.z = z;
    }

    public SerializableVector3(Vector3 v) : this(v.x, v.y, v.z) { }

    public Vector3 ToVector3() => new Vector3(x, y, z);

    public static implicit operator SerializableVector3(Vector3 v)  => new SerializableVector3(v);
    public static implicit operator Vector3(SerializableVector3 v)  => v.ToVector3();

    public override string ToString() => $"({x:F2}, {y:F2}, {z:F2})";
}

/// <summary>JsonUtility-serializable version of Vector3Int.</summary>
[Serializable]
public struct SerializableVector3Int
{
    public int x, y, z;

    public SerializableVector3Int(int x, int y, int z)
    {
        this.x = x; this.y = y; this.z = z;
    }

    public SerializableVector3Int(Vector3Int v) : this(v.x, v.y, v.z) { }

    public Vector3Int ToVector3Int() => new Vector3Int(x, y, z);

    public static implicit operator SerializableVector3Int(Vector3Int v) => new SerializableVector3Int(v);
    public static implicit operator Vector3Int(SerializableVector3Int v) => v.ToVector3Int();

    public override string ToString() => $"({x}, {y}, {z})";
}
