using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameSelection : MonoBehaviour
{
    public int numbuttons = 2; //must be >= 2
    public int numModes = 3;
    public int mode = 0;

    public GameObject modeText;
    public GameObject buttonPrefab;
    public GameObject infoText;

    private GameObject sceneManager;
    private GameObject statTracker;
    private List<GameObject> buttons;

    private bool[] toggles;
    private Stats[] stats;
    private Stats applyStats;

    private bool[,] tempSet;
    private Stats[] tempStats;
    private Stats applyTemp;

    private const int numStats = 5;
    private const int numWinCons = 4;

    //Stats: { 0=speed, 1=radius, 2=spawnLimit, 3=visionRange, 4=RespawnTime }
    private string[] statName = { "Speed", "Radius", "Spawn Limit", "Vision Range", "Respawn Time" };
    private float[] minStat = { -10.0f, -5.0f, -5.0f, -1.0f, -0.05f };
    private float[] maxStat = {  10.0f,  5.0f, 10.0f,  1.0f,  0.05f };

    //Win Conditions:
    private string[] winConText = { "Warrior:\nEliminate enemies with melee combat\nSize Up + Speed Up\nRepels Enemies (Hunter Playstyle)",
                                    "Summoner:\nEliminate enemies with drones\nHealth Down + Speed Down\nAttracts Enemies (Survival/Drone Management Playerstyle)",
                                    "Pulsor: \nEliminate enemies with pulses in X intervals\nHealth Down + Speed Up\nAttracts Enemies (Survival/Timing Management Playstyle)",
                                    "Turret: \nEliminate enemies from range without getting hit\nHealth Down + Size down + Speed 0\nAttracts Enemies (Survival/Aim Management Playstyle)"};


    // Start is called before the first frame update
    void Start()
    {
        mode = 0;
        sceneManager = GameObject.FindWithTag("SceneManager");
        statTracker = GameObject.FindWithTag("StatTracker");
        applyStats = new Stats(numStats);
        applyTemp = new Stats(numStats);


        GenerateModeMessage();
        GenerateInfoText(-1);
        GenerateButtons(Random.Range(4, 7));
    }

    public void GenerateButtons(int numButtons)
    {
        buttons = new List<GameObject>();
        toggles = new bool[numButtons - 1];

        if (mode == numModes - 1) //last mode before play starts
        {
            tempSet = new bool[numButtons - 1, numStats];
            tempStats = new Stats[numButtons - 1];
            for (int i = 0; i < numButtons; i++)
            {
                Stats tempStat = new Stats(numStats);
                /* private bool[] tempSet;
                   private Stats[] tempStats;
                   private Stats applyTemp; */

                //"Warrior:\nEliminate enemies with melee combat\nSize Up + Speed Up\nRepels Enemies (Hunter Playstyle)",
                //"Summoner:\nEliminate enemies with drones\nHealth Down + Speed Down\nAttracts Enemies (Survival/Drone Management Playerstyle)",
                //"Pulsor: \nEliminate enemies with pulses in X intervals\nHealth Down + Speed Up\nAttracts Enemies (Survival/Timing Management Playstyle)",
                //"Turret: \nEliminate enemies from range without getting hit\nHealth Down + Size down + Speed 0\nAttracts Enemies (Survival/Aim Management Playstyle)"};
                switch (i - 1)
                { //Stats: { 0=speed, 1=radius, 2=spawnLimit, 3=visionRange, 4=RespawnTime }

                    //ADD "ATTRACT" AS A STAT?

                    case 0: //Warrior: Size+, Speed+, Attract=false
                        tempStat.values[0] = +40;
                        tempStat.values[1] = +40;
                        //attract = false;
                        break;
                    case 1: //Summoner: Hp-, Speed-, Spawns+, RespawnTime-, Attract=true
                        tempStat.values[0] = -20;
                        tempStat.values[2] = +2000;
                        tempStat.values[4] = +0.005f; tempSet[i-1,4] = true;
                        //attract = true;
                        break;
                    case 2: //Pulsor: 
                        //implement 'pulse' mechanic
                        tempStat.values[0] = +40;
                        tempStat.values[1] = +20;
                        //attract = false (longer range than warrior)
                        break;
                }

                var go = GenerateButton(i, numButtons);
                if (i > 0)
                {
                    tempStats[i - 1] = tempStat;
                    addToButtonText(go, winConText[i - 1]);
                }
                buttons.Add(go); //add button reference to list
            }
        }
        else
        {
            stats = new Stats[numButtons - 1];

            for (int i = 0; i < numButtons; i++)
            {
                Stats stat = new Stats(numStats);
                if (i > 0)
                { //generate stats
                    toggles[i - 1] = false;
                    int numRandoms = Random.Range(0, stat.numVals);
                    for (int j = 0; j < numRandoms; j++)
                    {
                        int statIndex = Random.Range(0, stat.numVals);
                        stat.values[statIndex] += Random.Range(minStat[statIndex], maxStat[statIndex]);
                    }

                    stat.values[Random.Range(0, stat.numVals)] += Random.Range(3.0f, 5.0f);
                    stat.values[Random.Range(0, stat.numVals)] += Random.Range(-2.0f, -1.0f);
                }

                var go = GenerateButton(i, numButtons);
                if (i > 0)
                {
                    stats[i - 1] = stat;
                    addToButtonText(go, stat.toString());
                }
                buttons.Add(go); //add button reference to list
            }
        }
    }

    private void addToButtonText(GameObject go, string str)
    {
        var button = go.GetComponent<Button>();
        button.GetComponentInChildren<Text>().text += str;
    }

    private GameObject GenerateButton(int index, int numButtons)
    {
        var go = (GameObject)Instantiate(buttonPrefab);
        go.transform.SetParent(this.transform, false);

        //resizing buttons: https://forum.unity.com/threads/how-to-make-buttons-resize-to-their-child-text-while-auto-aligning-inside-a-panel.515010/
        //not implemented, used <Text>.BestFit toggle instead (opposite of resizing button to text)
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(Mathf.Clamp(Screen.width / 3, 150, 500), Screen.height / numButtons);
        go.transform.position = new Vector3(Screen.width / 2, rect.sizeDelta.y / 2 + index * rect.sizeDelta.y, 0);

        //Set the button vars/details
        var button = go.GetComponent<Button>(); //button can't be null
        int temp = index;
        button.onClick.AddListener(() => Selection(temp));

        if (index == 0) //first button is the 'confirm selection' button
            button.GetComponentInChildren<Text>().text = "Confirm";
        else
        {
            button.GetComponentInChildren<Text>().text = "Button #" + index + "\n";
        }
        return go;
    }

    private void GenerateModeMessage()
    {
        string modeMessage = (mode + 1) + "/" + numModes;
        modeText.GetComponent<Text>().text = modeMessage;
    }

    private void GenerateInfoText(int index)
    {
        string msg = "Stats:\n";
        for (int i = 0; i < numStats; i++)
        {
            float currValue = statTracker.GetComponent<StatTracker>().queenStats.values[i] + applyStats.values[i];
            string line = statName[i] + ": "+currValue.ToString("0.00") + " ";

            if (mode == numModes - 1)
            {
                string temp = tempStats[index].formatString("(", tempStats[index].values[i], true);
                if (temp != "") temp += ")"; //if temp not empty, close bracket
                line += temp;
            }
            else if (index >= 0)
                line = stats[index].formatString(line, stats[index].values[i], false);
            msg += line + "\n";
        }
        infoText.GetComponent<Text>().text = msg;
    }

    private void Selected(int n)
    { //the choice has been made
        if (mode < numModes-1)
        {
            applyStats.add(stats[n - 1], false);
            GenerateInfoText(-1);
            GenerateModeMessage();

            mode++;
            //Next selection:
            for (int i = 0; i < buttons.Count; i++)
            {
                Destroy(buttons[i]);
            }

            if (mode == numModes - 1)
                GenerateButtons(numWinCons+1);
            else
                GenerateButtons(Random.Range(6, 9));
        }
        else
        {
            mode = 0;
            //apply choices, then goto game
            applyTemp = tempStats[n - 1];

            StatTracker tracker = statTracker.GetComponent<StatTracker>();
            tracker.queenStats.add(applyStats, true);
            tracker.tempStats.set(applyTemp);
            bool[] tempBool = new bool[numStats];
            for (int i = 0; i < numStats; i++)
                tempBool[i] = tempSet[n - 1, i];

            
            tracker.tempBool = tempBool;
            tracker.winCon = n - 1;
            ChangeToScene("Slime");
        }
    }

    private void Selection(int n)
    {
        //Debug.Log("Pressed: " + n);
        if (n > 0)
        {
            toggles[n - 1] = !toggles[n - 1]; //flip toggle
            buttons[n].GetComponent<Button>().interactable = !toggles[n-1];
            GenerateInfoText(n - 1);
            for (int i = 1; i < buttons.Count; i++) //O(n) time solution
            { //fix to O(1) with storing which index was previously flipped
                if (i != n && toggles[i - 1])
                {
                    toggles[i - 1] = !toggles[i - 1]; //flip toggle
                    buttons[i].GetComponent<Button>().interactable = !toggles[i - 1];
                }
            }
        }
        else if (n == 0)
        {
            bool stop = false;
            for (int i = 1; i < buttons.Count && !stop; i++)
            { //same thing here, just save the index ffs
                if (toggles[i - 1])
                {
                    stop = true;
                    Selected(i);
                }
            }
        }
    }

    private void ChangeToScene(string scene)
    {
        sceneManager.GetComponent<SceneChanger>().ChangeScene(scene);
    }

    // Update is called once per frame
    void Update()
    {

    }
}