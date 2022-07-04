using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//[RequireComponent(typeof(UnityEngine.UI.Button))]
public class GameSelection : MonoBehaviour
{
    public int numbuttons = 2; //must be >= 2
    public int numModes = 3;
    public int mode = 0;

    public GameObject modeText;
    public GameObject buttonPrefab;

    private GameObject sceneManager;
    private List<GameObject> buttons;
    private bool[] toggles;
    // Start is called before the first frame update
    void Start()
    {
        mode = 0;
        sceneManager = GameObject.FindWithTag("SceneManager");

        GenerateModeMessage();
        GenerateButtons(numbuttons + 1);
    }

    public void GenerateButtons(int numButtons)
    {
        buttons = new List<GameObject>();
        toggles = new bool[numButtons - 1];
        for (int i = 0; i < numButtons; i++)
        {
            if (i > 0)
                toggles[i - 1] = false;
            var go = (GameObject)Instantiate(buttonPrefab);
            go.transform.SetParent(this.transform, false);
            go.transform.position = go.transform.position + new Vector3(0, i * 30, 0);

            //Set the button vars/details
            var button = go.GetComponent<Button>(); //button can't be null
            int temp = i;
            button.onClick.AddListener(() => Selection(temp));

            if (i == 0) //first button is the 'confirm selection' button
                button.GetComponentInChildren<Text>().text = "Confirm";
            else
            {
                button.GetComponentInChildren<Text>().text = "Button #" + i;
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
            GenerateButtons(n + 2);
        }
        else
        {
            mode = 0;
            //apply choices, then goto game
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