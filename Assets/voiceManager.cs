using UnityEngine;
using Oculus.Voice;

public class voiceManager : MonoBehaviour
{
    public AppVoiceExperience appVoice;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space pressed!"); // Test if detected
            appVoice.Activate();
        }

        if(OVRInput.GetDown(OVRInput.Button.One))
        {
            appVoice.Activate();
        }
    }
}

