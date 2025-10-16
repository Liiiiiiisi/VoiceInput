using UnityEngine;
using Oculus.Voice;
using TMPro;

public class VoiceManager : MonoBehaviour
{
    public OVRHand ovrHand;
    public AppVoiceExperience appVoice;
    public GameObject uiTextObject;
    public TMP_Text transcriptText;

    private int tapCount = 0;
    private bool lastThumbTap = false;

    void Update()
    {
        if (ovrHand != null)
        {
            var microGesture = ovrHand.GetMicrogestureType();
            bool isThumbTap = microGesture == OVRHand.MicrogestureType.ThumbTap;

            // 检测到 ThumbTap 的上升沿
            if (isThumbTap && !lastThumbTap)
            {
                tapCount++;
                Debug.Log("ThumbTap tapCount: " + tapCount);

                if (tapCount == 1)
                {
                    ActivateVoiceAndUI();
                }
                else if (tapCount == 2)
                {
                    DeactivateVoiceAndUI();
                    tapCount = 0; // 重置计数
                }
            }
            lastThumbTap = isThumbTap;
        }
    }

    private void ActivateVoiceAndUI()
    {
        Debug.Log("Voice + UI Activated!");
        if (appVoice != null) appVoice.Activate();
        if (uiTextObject != null) uiTextObject.SetActive(true);
    }

    private void DeactivateVoiceAndUI()
    {
        Debug.Log("Voice + UI Deactivated!");
        if (appVoice != null) appVoice.Deactivate();
        if (uiTextObject != null) uiTextObject.SetActive(false);
    }
}