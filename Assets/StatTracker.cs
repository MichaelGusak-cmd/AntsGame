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
        //Stats: { 0=speed, 1=radius, 2=spawnLimit, 3=visionRange, 4=RespawnTime }

        //gurantee the vals are 0 on init
        for (int i = 0; i < numVals; i++)
            values[i] = 0;
    }

    public void add(Stats s, bool clamp)
    {
        for (int i = 0; i < numVals; i++)
        {
            float val = s.values[i];
            values[i] += val;
            if (clamp)
                values[i] = Mathf.Clamp(values[i], 0, 1000);
                
            //}
        }
    }

    public void set(Stats s)
    {
        for (int i = 0; i < numVals; i++)
        {
            values[i] = s.values[i];
        }
    }

    public string toString()
    {
        string[] str = { "Speed: ",
                         "Radius: ",
                         "Spawn Limit: ",
                         "Vision Range: ",
                         "Respawn Time: "};

        string output = "";
        for (int i = 0; i < numVals; i++)
        {
            string temp = formatString(str[i], values[i], true);
            if (temp != "") temp += "\n";
            output += temp;
        }

            return output;
    }

    public string formatString(string str, float val, bool nullString)
    {
        if (val > 0.01) str += "+" + val.ToString("0.00");
        else if (val < -0.01) str += val.ToString("0.00");
        else if (nullString) str = "";
        return str;
    }
}
public class StatTracker : MonoBehaviour
{
    public Stats queenStats;
    public Stats tempStats;
    public int winCon;
    public bool[] tempBool;
    private const int numStats = 5;
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        queenStats = new Stats(numStats);
        tempStats = new Stats(numStats);
        tempBool = new bool[numStats];
        winCon = -1;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
