using UnityEngine;

public class ControllAnimator : MonoBehaviour
{
    public Animator tutor1;



    public void TriggerAnimation(string[] input)
    {
        string instruction = input[0].ToLower();
        string action = input[1].ToLower();

        string triggerName = "";

        switch (action)
        {
            case " I ":
                triggerName = "I";
                break;
            case " can ":
                triggerName = "can";
                break;
            case " sign ":
                triggerName = "sign";
                break;
            case " swim ":
                triggerName = "swim";
                break;

        }

        Animator targetAnimator = tutor1;
        targetAnimator.Play(triggerName, 0, 0f);


    }
}