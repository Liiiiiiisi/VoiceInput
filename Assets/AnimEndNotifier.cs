using UnityEngine;

// 挂到响应动画对应的 State（非 Rest）
// OnStateEnter -> 通知动画开始（传递 state hash）
// OnStateExit  -> 状态被脱离时通知动画结束（传递 state hash）
public class AnimEndNotifier : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int stateHash = stateInfo.shortNameHash;
        var vm = animator.GetComponentInParent<VoiceManager>();
        if (vm != null)
        {
            Debug.Log("[AnimEndNotifier] OnStateEnter -> NotifyAnimationStarted stateHash=" + stateHash);
            vm.NotifyAnimationStarted(stateHash);
            return;
        }

        vm = Object.FindObjectOfType<VoiceManager>();
        if (vm != null)
        {
            Debug.Log("[AnimEndNotifier] OnStateEnter -> NotifyAnimationStarted (fallback) stateHash=" + stateHash);
            vm.NotifyAnimationStarted(stateHash);
            return;
        }

        Debug.LogWarning("[AnimEndNotifier] OnStateEnter: VoiceManager not found");
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int stateHash = stateInfo.shortNameHash;
        var vm = animator.GetComponentInParent<VoiceManager>();
        if (vm != null)
        {
            Debug.Log("[AnimEndNotifier] OnStateExit -> NotifyAnimationEnded stateHash=" + stateHash);
            vm.NotifyAnimationEnded(stateHash);
            return;
        }

        vm = Object.FindObjectOfType<VoiceManager>();
        if (vm != null)
        {
            Debug.Log("[AnimEndNotifier] OnStateExit -> NotifyAnimationEnded (fallback) stateHash=" + stateHash);
            vm.NotifyAnimationEnded(stateHash);
            return;
        }

        Debug.LogWarning("[AnimEndNotifier] OnStateExit: VoiceManager not found");
    }
}