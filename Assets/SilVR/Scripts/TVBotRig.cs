
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TVBotRig : UdonSharpBehaviour
{
    public GameObject tv_bot;

    public GameObject StartObj;
    public GameObject WhiteObj;
    public GameObject BlackObj;
    public GameObject Thinking;

    int counter = 0;

    bool isInitialized;
    GameObject[] ThinkingTicks;


    bool isThinking = false;

    private int DecodePosition(int encoded_state)
    {
        return encoded_state & 3;
    }
    private bool DecodeThinking(int encoded_state)
    {
        return ((encoded_state >> 2) & 1) == 1;
    }

    public void SetStateFromEncoded(int encoded_state)
    {
        int pos = DecodePosition(encoded_state);
        bool isThinking = DecodeThinking(encoded_state);

        if (pos == 0)
        {
            JumpToStart();
        }
        else if (pos == 1)
        {
            JumpToBlack();
        }
        else if (pos == 2)
        {
            JumpToWhite();
        }
        if (isThinking)
        {
            StartThinking();
        }
        else
        {
            StopThinking();
        }

    }

    public void JumpToStart()
    {
        JumpToObject(StartObj);
    }
    public void JumpToWhite()
    {
        JumpToObject(WhiteObj);
    }
    public void JumpToBlack()
    {
        JumpToObject(BlackObj);
    }
    public void StartThinking()
    {
        Thinking.SetActive(true);
        isThinking = true;
    }
    public void StopThinking()
    {
        Thinking.SetActive(false);
        isThinking = false;
    }

    void JumpToObject(GameObject obj)
    {
        tv_bot.transform.position = obj.transform.position;
        tv_bot.transform.rotation = obj.transform.rotation;
    }

    private void Update()
    {
        if (isInitialized && isThinking)
        {
            ThinkingTicks[(counter + 1) & 7].SetActive(false);
            ThinkingTicks[counter].SetActive(true);
            
            if ( ( Time.frameCount & 31 ) == 0 )
            {
                counter = (counter + 1) & 7;
            }
        }
    }

    void Start()
    {
        ThinkingTicks = new GameObject[8];
        for (int i = 0; i < 8; i++)
        {
            ThinkingTicks[i] = Thinking.transform.GetChild(i).gameObject;
        }
        isInitialized = true;
    }
}
