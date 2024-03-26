using UnityEngine;

public struct Cell {
    
    public enum Type {
        Invalid, // ��һ��ö��ֵ����Ĭ�ϸ�ֵ
        Empty,
        Mine,
        Number,
    }

    public Vector3Int position;
    public Type type;
    public int number;
    public bool revealed;// ��ʾ
    public bool flagged;// ���
    public bool exploded;// ����
}
