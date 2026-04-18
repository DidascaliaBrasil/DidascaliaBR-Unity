using UnityEngine;

[System.Serializable]
public class ClassroomData
{
    public int numBoys;
    public int numGirls;
    public int numDesks;
    public int shape;
    public int rows;
    public int cols;
    public float radius;
    public int maxDesksInSemiCircle;

    // A NOSSA MOCHILA ESTÁTICA
    public static ClassroomData DadosCarregados;

    public ClassroomData() { }
}