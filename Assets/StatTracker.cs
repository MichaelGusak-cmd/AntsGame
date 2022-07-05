using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Stats
{
    public float[] values;
    public int numVals; //same as values.size

    public Stats(int n)
    {
        values = new float[n];
        numVals = n;
        //Stats: { 0=speed, 1=radius, 2=spawnRate, 3=visionRange }

        //gurantee the vals are 0 on init
        for (int i = 0; i < numVals; i++)
            values[i] = 0;
    }

    public void add(Stats s)
    {
        for (int i = 0; i < numVals; i++)
        {
            float val = s.values[i];
            values[i] += val;
        }
    }

    public string toString()
    {
        string[] str = { "Speed: ",
                         "Radius: ",
                         "Spawn Rate: ",
                         "Vision Range: " };

        string output = "";
        for (int i = 0; i < numVals; i++)
            output += formatString(str[i], values[i]);

        return output;
    }

    private string formatString(string str, float val)
    {
        if (val > 0.01) str += "+" + val.ToString("0.0") + "\n";
        else if (val < -0.01) str += val.ToString("0.0") + "\n";
        else str = "";
        return str;
    }
}
public class StatTracker : MonoBehaviour
{
    public Stats queenStats;
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        queenStats = new Stats(4);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
