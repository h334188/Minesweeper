using UnityEngine;

public struct Cell {
    
    public enum Type {
        Invalid, // 第一个枚举值，作默认赋值
        Empty,
        Mine,
        Number,
    }

    public Vector3Int position;
    public Type type;
    public int number;
    public bool revealed;// 显示
    public bool flagged;// 标记
    public bool exploded;// 引爆
}
