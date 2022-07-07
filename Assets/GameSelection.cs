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

    private GameObject sceneManager;
    private GameObject statTracker;
    private List<GameObject> buttons;
    private bool[] toggles;
    private Stats[] stats;
    private Stats applyStats;

    private const int numStats = 5;

    // Start is called before the first frame update
    void Start()
    {
        mode = 0;
        sceneManager = GameObject.FindWithTag("SceneManager");
        statTracker = GameObject.FindWithTag("StatTracker");
        applyStats = new Stats(numStats); //get current stats

        GenerateModeMessage();
        GenerateButtons(numbuttons + 1);
    }

    public void GenerateButtons(int numButtons)
    {
        buttons = new List<GameObject>();
        toggles = new bool[numButtons - 1];
        stats = new Stats[numButtons - 1];
        for (int i = 0; i < numButtons; i++)
        {
            Stats stat = new Stats(numStats);
            if (i > 0)
            {
                toggles[i - 1] = false;
                stat.values[Random.Range(0, stat.numVals)] += Random.Range(3.0f, 5.0f);
                stat.values[Random.Range(0, stat.numVals)] += Random.Range(-2.0f, -1.0f);
            }
            var go = (GameObject)Instantiate(buttonPrefab);
            go.transform.SetParent(this.transform, false);

            //resizing buttons: https://forum.unity.com/threads/how-to-make-buttons-resize-to-their-child-text-while-auto-aligning-inside-a-panel.515010/
            //not implemented, used <Text>.BestFit toggle instead (opposite of resizing button to text)
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(Mathf.Clamp(Screen.width/3, 150, 500), Screen.height / numButtons);
            go.transform.position = new Vector3(Screen.width/2, rect.sizeDelta.y/2 + i * rect.sizeDelta.y, 0);

            //Set the button vars/details
            var button = go.GetComponent<Button>(); //button can't be null
            int temp = i;
            button.onClick.AddListener(() => Selection(temp));

            if (i == 0) //first button is the 'confirm selection' button
                button.GetComponentInChildren<Text>().text = "Confirm";
            else
            {
                stats[i - 1] = stat;
                button.GetComponentInChildren<Text>().text = "Button #" + i+"\n"+stat.toString();
            }
            buttons.Add(go); //add button reference to list
        }
    }
    private void GenerateModeMessage()
    {
        string modeMessage = (mode + 1) + "/" + numModes;
        modeText.GetComponent<Text>().text = modeMessage;
    }

    private void Selected(int n)
    { //the choice has been made, now what
        if (mode < numModes)
        {
            applyStats.add(stats[n - 1]);
            //STATS
            //  Struct

            //NON-STATS
            //  prime factorization?
            //  list of nums?
            //  list of bools?

            GenerateModeMessage();
            //Next selection:
            for (int i = 0; i < buttons.Count; i++)
            {
                Destroy(buttons[i]);
            }
            GenerateButtons(Random.Range(6, 9));
        }
        else
        {
            mode = 0;
            //apply choices, then goto game
            //APPLY(applyStats);
            statTracker.GetComponent<StatTracker>().queenStats.add(applyStats);
            ChangeToScene("Slime");
        }
    }

    private void Selection(int n)
    {
        Debug.Log("Pressed: " + n);
        if (n > 0)
        {
            toggles[n - 1] = !toggles[n - 1]; //flip toggle
            buttons[n].GetComponent<Button>().interactable = !toggles[n-1];
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
                    mode++;
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