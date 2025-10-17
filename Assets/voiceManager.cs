using System.Collections;
using UnityEngine;
using Oculus.Voice;
using TMPro;

public class VoiceManager : MonoBehaviour
{
    public OVRHand ovrHand;
    public AppVoiceExperience appVoice;
    public GameObject uiTextObject;
    public TMP_Text transcriptText;

    // 可选：在 Inspector 绑定 response Animator（若需要）
    public Animator responseAnimator;

    // 会话与协程引用（用于避免旧事件影响新会话）
    private int currentSessionId = 0;                      // 每次 OnFullTranscription++，用于会话隔离
    private bool animationStarted = false;                 // 当前 Animator 是否处于 Started（由 NotifyAnimationStarted/Ended 控制）
    private bool seenStartedThisSession = false;           // 当前会话内是否见过 Started（用于判断 Started->Ended 是否发生过）
    private bool endedAfterTranscription = false;          // 当前会话内是否出现过 Started->Ended（用于条件 B）
    private int lastEndedStateHash = 0;                    // 记录上一次结束的 state hash（用于条件 B 的匹配）

    private Coroutine waitForStartCoroutine;
    private Coroutine stableStartedCoroutine;
    private Coroutine waitForRestartCoroutine;
    private Coroutine maxAnimGuardCoroutine;

    // 参数（可调整）
    private const float stableStartedDuration = 3f;        // 条件1 的持续要求（5s）
    private const float waitForStartTimeout = 3f;          // OnFullTranscription 后等待动画开始的窗口（3s）
    private const float waitForRestartTimeout = 20f;       // 条件2 的等待重启超时（30s）
    private const float maxAnimDuration = 20f;             // 动画最长保底（30s）防止卡死

    private int tapCount = 0;
    private bool lastThumbTap = false;

    void Start()
    {
        if (uiTextObject != null) uiTextObject.SetActive(false);
        if (transcriptText != null) transcriptText.text = "Start speaking...";
    }

    void OnEnable()
    {
        if (appVoice != null)
        {
            appVoice.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
            appVoice.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
        }
    }

    void OnDisable()
    {
        if (appVoice != null)
        {
            appVoice.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
            appVoice.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateVoiceAndUI();
        }

        if (ovrHand != null)
        {
            var microGesture = ovrHand.GetMicrogestureType();
            bool isThumbTap = microGesture == OVRHand.MicrogestureType.ThumbTap;

            if (isThumbTap && !lastThumbTap)
            {
                tapCount++;
                Debug.Log("[VoiceManager] ThumbTap tapCount: " + tapCount);

                if (tapCount == 1) ActivateVoiceAndUI();
                else if (tapCount == 2)
                {
                    DeactivateVoiceAndUI();
                    tapCount = 0;
                }
            }
            lastThumbTap = isThumbTap;
        }
    }

    private void OnPartialTranscription(string text)
    {
        if (transcriptText != null) transcriptText.text = text;
    }

    // 主逻辑入口：每次完整转写到达都会创建一个新的监控会话
    private void OnFullTranscription(string text)
    {
        if (transcriptText != null) transcriptText.text = text;

        // 新会话 id（用于隔离之前的协程/事件）
        currentSessionId++;
        int session = currentSessionId;

        // 重置会话内标志
        seenStartedThisSession = false;
        endedAfterTranscription = false;
        lastEndedStateHash = 0;

        // 仅在 UI 可见时启动监控
        if (uiTextObject != null && uiTextObject.activeInHierarchy)
        {
            // 取消旧协程（它们会自行检查 session）
            StopAndNullCoroutine(ref waitForStartCoroutine);
            StopAndNullCoroutine(ref stableStartedCoroutine);
            StopAndNullCoroutine(ref waitForRestartCoroutine);
            StopAndNullCoroutine(ref maxAnimGuardCoroutine);

            // 如果当前已经处于动画 started（可能在转写前就已开始）
            if (animationStarted)
            {
                // 启动稳定检测：连续保持 started required 秒则触发 条件1（关闭 UI）
                stableStartedCoroutine = StartCoroutine(HideIfAnimationStableTrueForSession(session, stableStartedDuration));
                // 启动 maxGuard 防止卡死
                maxAnimGuardCoroutine = StartCoroutine(MaxAnimationGuardForSession(session, maxAnimDuration));
            }
            else
            {
                // 等待动画开始的短期窗口（waitForStartTimeout）。若在窗口内 start 了，则转为 stable 检测。
                waitForStartCoroutine = StartCoroutine(WaitForAnimationStartThenMaybeHideForSession(session, waitForStartTimeout));
            }
        }
    }

    // 等待在短窗口内动画开始；若未开始则直接隐藏（认为不会播放动画）
    private IEnumerator WaitForAnimationStartThenMaybeHideForSession(int sessionId, float waitSeconds)
    {
        float timer = 0f;
        while (timer < waitSeconds)
        {
            if (sessionId != currentSessionId) yield break;

            if (animationStarted)
            {
                seenStartedThisSession = true;
                waitForStartCoroutine = null;
                stableStartedCoroutine = StartCoroutine(HideIfAnimationStableTrueForSession(sessionId, stableStartedDuration));
                maxAnimGuardCoroutine = StartCoroutine(MaxAnimationGuardForSession(sessionId, maxAnimDuration));
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        waitForStartCoroutine = null;
        Debug.Log("[VoiceManager] WaitForStart timed out (no animation start) -> Hide UI");
        ClearSessionAndHide(sessionId);
    }

    // 要求在给定时间内一直保持 animationStarted == true 才隐藏；若期间变为 false 则取消（session隔离）
    private IEnumerator HideIfAnimationStableTrueForSession(int sessionId, float requiredSeconds)
    {
        float timer = 0f;
        while (timer < requiredSeconds)
        {
            if (sessionId != currentSessionId) yield break;

            if (!animationStarted)
            {
                stableStartedCoroutine = null;
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        stableStartedCoroutine = null;
        Debug.Log("[VoiceManager] Condition1 met (stable started) -> Hide UI");
        ClearSessionAndHide(sessionId);
    }

    // 条件2 的等待重启协程：在 Ended 后等待下一次 Started（或超时）
    private IEnumerator WaitForRestartThenTimeout(int sessionId, float timeout)
    {
        float timer = 0f;
        while (timer < timeout)
        {
            if (sessionId != currentSessionId) yield break;
            timer += Time.deltaTime;
            yield return null;
        }

        waitForRestartCoroutine = null;
        Debug.Log("[VoiceManager] WaitForRestart timed out -> Hide UI");
        ClearSessionAndHide(sessionId);
    }

    // 动画最长保底，避免卡死。若超时且仍处于 started，则强制隐藏。
    private IEnumerator MaxAnimationGuardForSession(int sessionId, float maxSeconds)
    {
        float timer = 0f;
        while (timer < maxSeconds)
        {
            if (sessionId != currentSessionId)
            {
                maxAnimGuardCoroutine = null;
                yield break;
            }

            if (!animationStarted)
            {
                maxAnimGuardCoroutine = null;
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        maxAnimGuardCoroutine = null;
        animationStarted = false;
        Debug.Log("[VoiceManager] Max animation duration reached -> forcing Hide UI");
        ClearSessionAndHide(sessionId);
    }

    // 外部调用：动画开始（现在带 stateHash）
    public void NotifyAnimationStarted(int stateHash)
    {
        Debug.LogFormat("[VoiceManager] NotifyAnimationStarted called | session={0} stateHash={1} animationStarted(before)={2} seenStartedThisSession={3} endedAfterTranscription={4} uiActive={5}",
            currentSessionId, stateHash, animationStarted, seenStartedThisSession, endedAfterTranscription, uiTextObject != null ? uiTextObject.activeInHierarchy.ToString() : "null");

        animationStarted = true;
        seenStartedThisSession = true;

        // 如果存在等待 start 的协程 -> 切换到稳定检测
        if (waitForStartCoroutine != null)
        {
            StopAndNullCoroutine(ref waitForStartCoroutine);
            if (stableStartedCoroutine != null) StopAndNullCoroutine(ref stableStartedCoroutine);
            stableStartedCoroutine = StartCoroutine(HideIfAnimationStableTrueForSession(currentSessionId, stableStartedDuration));
        }

        // 条件2: 如果在当前会话里之前见过 ended 且 stateHash matches lastEndedStateHash，则满足条件2 -> 隐藏
        if (endedAfterTranscription && lastEndedStateHash != 0 && stateHash == lastEndedStateHash && currentSessionId != 0)
        {
            Debug.LogFormat("[VoiceManager] Condition2 met (ended before & same state restarted) -> ClearSessionAndHide session={0} stateHash={1}", currentSessionId, stateHash);
            ClearSessionAndHide(currentSessionId);
            return;
        }

        if (maxAnimGuardCoroutine != null) StopAndNullCoroutine(ref maxAnimGuardCoroutine);
        maxAnimGuardCoroutine = StartCoroutine(MaxAnimationGuardForSession(currentSessionId, maxAnimDuration));
    }

    // 外部调用：动画结束（现在带 stateHash）
    public void NotifyAnimationEnded(int stateHash)
    {
        Debug.LogFormat("[VoiceManager] NotifyAnimationEnded called | session={0} stateHash={1} animationStarted(before)={2} seenStartedThisSession={3} endedAfterTranscription(before)={4} uiActive={5}",
            currentSessionId, stateHash, animationStarted, seenStartedThisSession, endedAfterTranscription, uiTextObject != null ? uiTextObject.activeInHierarchy.ToString() : "null");

        animationStarted = false;

        if (maxAnimGuardCoroutine != null) { StopAndNullCoroutine(ref maxAnimGuardCoroutine); }

        // 只要动画在会话期内结束，就记录结束的 stateHash 并启动等待重启（条件2）
        if (currentSessionId != 0)
        {
            endedAfterTranscription = true;
            lastEndedStateHash = stateHash;
            Debug.LogFormat("[VoiceManager] Marked endedAfterTranscription=true for session={0} lastEndedStateHash={1}", currentSessionId, lastEndedStateHash);

            if (waitForRestartCoroutine != null) StopAndNullCoroutine(ref waitForRestartCoroutine);
            waitForRestartCoroutine = StartCoroutine(WaitForRestartThenTimeout(currentSessionId, waitForRestartTimeout));
        }

        if (waitForStartCoroutine != null) { StopAndNullCoroutine(ref waitForStartCoroutine); }
        if (stableStartedCoroutine != null) { StopAndNullCoroutine(ref stableStartedCoroutine); }
    }

    private void ClearSessionAndHide(int sessionId)
    {
        Debug.LogFormat("[VoiceManager] ClearSessionAndHide called | sessionArg={0} currentSessionId={1} animationStarted={2} seenStartedThisSession={3} endedAfterTranscription={4} uiActive(before)={5}",
            sessionId, currentSessionId, animationStarted, seenStartedThisSession, endedAfterTranscription, uiTextObject != null ? uiTextObject.activeInHierarchy.ToString() : "null");

        if (sessionId != currentSessionId)
        {
            Debug.LogFormat("[VoiceManager] ClearSessionAndHide aborted: session mismatch (arg {0} != current {1})", sessionId, currentSessionId);
            return;
        }

        StopAndNullCoroutine(ref waitForStartCoroutine);
        StopAndNullCoroutine(ref stableStartedCoroutine);
        StopAndNullCoroutine(ref waitForRestartCoroutine);
        StopAndNullCoroutine(ref maxAnimGuardCoroutine);

        animationStarted = false;
        seenStartedThisSession = false;
        endedAfterTranscription = false;
        lastEndedStateHash = 0;
        currentSessionId = 0;

        HideUIImmediately();
        Debug.Log("[VoiceManager] UI hidden and session cleared");
    }


    private void ActivateVoiceAndUI()
    {
        Debug.Log("[VoiceManager] ActivateVoiceAndUI");
        if (appVoice != null) appVoice.Activate();
        if (uiTextObject != null) uiTextObject.SetActive(true);

        // 重置为默认提示文本，便于每次重新录制时显示 "Start speaking..."
        if (transcriptText != null) transcriptText.text = "Start speaking...";

        StopAndNullCoroutine(ref waitForStartCoroutine);
        StopAndNullCoroutine(ref stableStartedCoroutine);
        StopAndNullCoroutine(ref waitForRestartCoroutine);
        StopAndNullCoroutine(ref maxAnimGuardCoroutine);

        animationStarted = false;
        seenStartedThisSession = false;
        endedAfterTranscription = false;
        lastEndedStateHash = 0;
        currentSessionId = 0;
    }


    private void DeactivateVoiceAndUI()
    {
        Debug.Log("[VoiceManager] DeactivateVoiceAndUI");
        if (appVoice != null) appVoice.Deactivate();

        StopAndNullCoroutine(ref waitForStartCoroutine);
        StopAndNullCoroutine(ref stableStartedCoroutine);
        StopAndNullCoroutine(ref waitForRestartCoroutine);
        StopAndNullCoroutine(ref maxAnimGuardCoroutine);

        animationStarted = false;
        seenStartedThisSession = false;
        endedAfterTranscription = false;
        lastEndedStateHash = 0;
        currentSessionId = 0;

        HideUIImmediately();
    }

    private void HideUIImmediately()
    {
        if (uiTextObject != null) uiTextObject.SetActive(false);
    }

    // 小工具：安全停止协程并置 null
    private void StopAndNullCoroutine(ref Coroutine c)
    {
        if (c != null)
        {
            StopCoroutine(c);
            c = null;
        }
    }
}