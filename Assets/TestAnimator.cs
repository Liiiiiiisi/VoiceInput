using UnityEngine;

public class TestAnimator : MonoBehaviour
{
    public Animator tutor1;
    private string currentAnimation = "";

    // Smoothly changes to a new animation if it's different
    private void ChangeAnimation(string animationName, float crossfade = 0.2f)
    {
        if (currentAnimation != animationName)
        {
            currentAnimation = animationName;
            tutor1.CrossFade(animationName, crossfade); // ✅ correct method
        }
    }

    // Example use: call this from another script or event
    public void TriggerAnimation(string[] input)
    {
        Debug.Log("the cue is" + input);
        // Example: input = ["I", "can", "sign", "swim"]
        foreach (string word in input)
        {
            string cleanWord = word.Trim().ToLower();
            ChangeAnimation(cleanWord, 0.1f); // shorter crossfade
        }
    }

    private void CheckAnimation()
    {
        AnimatorStateInfo info = tutor1.GetCurrentAnimatorStateInfo(0);
        if (info.normalizedTime >= 1f && !tutor1.IsInTransition(0))
        {
            Debug.Log("Animation finished: " + currentAnimation);
        }
    }
}
