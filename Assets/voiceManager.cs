using UnityEngine;
using Oculus.Voice;
using TMPro; // for TextMeshPro UI

public class VoiceManager : MonoBehaviour
{
    public OVRHand ovrHand;                 // Assign your hand (LeftHandAnchor or RightHandAnchor)
    public AppVoiceExperience appVoice;     // Assign your AppVoiceExperience object
    public GameObject uiTextObject;         // UI panel or TextMeshPro object to show/hide
    public TMP_Text transcriptText;         // Optional: to display recognized speech text

    private bool isUIActive = false;

    void OnEnable()
    {
        if (appVoice != null)
        {
            // Listen to Wit.ai transcription events
            appVoice.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
            appVoice.VoiceEvents.OnError.AddListener(OnError);
        }
    }

    void OnDisable()
    {
        if (appVoice != null)
        {
            appVoice.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
            appVoice.VoiceEvents.OnError.RemoveListener(OnError);
        }
    }

    void Update()
    {
        // Manual test (space bar)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space pressed!");
            ActivateVoiceAndUI();
        }

        // Controller input (A button / Button.One)
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            ActivateVoiceAndUI();
        }

        // Hand gesture trigger
        if (ovrHand != null)
        {
            OVRHand.MicrogestureType microGesture = ovrHand.GetMicrogestureType();
            if (microGesture == OVRHand.MicrogestureType.ThumbTap)
            {
                ActivateVoiceAndUI();
            }
        }
    }

    // ✨ Combined function for gesture or button activation
    private void ActivateVoiceAndUI()
    {
        Debug.Log("Voice + UI Activated!");

        // Activate Wit voice recognition
        if (appVoice != null)
            appVoice.Activate();

        // Activate UI element
        if (uiTextObject != null)
        {
            isUIActive = !isUIActive; // toggle on/off if you like
            uiTextObject.SetActive(isUIActive);
        }
    }

    // 🔤 Display recognized text
    private void OnFullTranscription(string text)
    {
        Debug.Log("Recognized speech: " + text);
        if (transcriptText != null)
        {
            transcriptText.text = text;
        }
    }

    // ⚠️ Error handling
    private void OnError(string error, string message)
    {
        Debug.LogError($"Voice error: {error} - {message}");
    }
}
