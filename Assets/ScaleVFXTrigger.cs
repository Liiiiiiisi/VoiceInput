using System.Collections;
using UnityEngine;

public class ScaleToZeroVFX : MonoBehaviour
{
    [Header("Assign a VFX prefab or particle system")]
    public GameObject vfxPrefab;                  // Prefab of the VFX
    public float scaleThreshold = 0.01f;          // <= threshold considered "zero"
    public float spawnDelay = 3f;                 // delay before instantiating VFX
    public bool destroyAfterPlay = true;          // destroy instance after playing
    public float destroyDelayAfterPlay = 3f;      // time to destroy/disable after play

    private bool wasScaledUp = false;
    private GameObject vfxInstance;
    private ParticleSystem vfxParticle;
    private Coroutine delayedSpawner;

    void Update()
    {
        if (vfxPrefab == null) return;

        float currentScale = transform.localScale.magnitude;

        // detect scale up from (near) zero
        if (!wasScaledUp && currentScale > scaleThreshold)
        {
            wasScaledUp = true;
            // start delayed spawn-monitor flow if not already running
            if (delayedSpawner == null)
            {
                delayedSpawner = StartCoroutine(DelayedSpawnAndWaitForZero());
            }
        }

        // detect scale back to zero
        if (wasScaledUp && currentScale <= scaleThreshold)
        {
            // If we have an instance already created => play it now
            if (vfxInstance != null)
            {
                PlayVFX();
            }
            else
            {
                // If the delayed spawn is still waiting to spawn, cancel spawn (object returned to zero quickly)
                if (delayedSpawner != null)
                {
                    StopCoroutine(delayedSpawner);
                    delayedSpawner = null;
                }
                else
                {
                    // No delayed spawner and no instance -> spawn immediately and play
                    EnsureInstance();
                    PlayVFX();
                }
            }

            wasScaledUp = false;
        }
    }

    // Wait spawnDelay seconds (cancel if object returns to zero). After spawn, wait until object scales back to zero to play.
    private IEnumerator DelayedSpawnAndWaitForZero()
    {
        float timer = 0f;
        while (timer < spawnDelay)
        {
            if (this == null) yield break;
            if (transform == null) yield break;

            // if object returned to zero during delay -> cancel spawn
            if (transform.localScale.magnitude <= scaleThreshold)
            {
                delayedSpawner = null;
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // spawn only if still not zero
        if (transform == null) { delayedSpawner = null; yield break; }
        if (transform.localScale.magnitude <= scaleThreshold) { delayedSpawner = null; yield break; }

        EnsureInstance();

        // wait until scale returns to zero, then PlayVFX
        while (transform != null && transform.localScale.magnitude > scaleThreshold)
        {
            yield return null;
        }

        // if object still exists and scale reached zero -> play
        if (transform != null && transform.localScale.magnitude <= scaleThreshold)
        {
            PlayVFX();
        }

        delayedSpawner = null;
    }

    // Ensure a single instance exists (not parented so particle can use World sim space)
    private void EnsureInstance()
    {
        if (vfxInstance == null)
        {
            vfxInstance = Instantiate(vfxPrefab, transform.position, transform.rotation);
            // keep in world space by default (avoid being affected by target's scale)
            vfxInstance.transform.SetParent(null, true);

            vfxParticle = vfxInstance.GetComponent<ParticleSystem>();
            if (vfxParticle != null)
            {
                var main = vfxParticle.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
            }

            vfxInstance.SetActive(false);
        }
        else
        {
            // sync world position to object's position
            vfxInstance.transform.position = transform.position;
            vfxInstance.transform.rotation = transform.rotation;
        }
    }

    private void PlayVFX()
    {
        if (vfxInstance == null)
        {
            EnsureInstance();
        }

        if (vfxInstance == null) return;

        vfxInstance.transform.position = transform.position;
        vfxInstance.transform.rotation = transform.rotation;
        vfxInstance.SetActive(true);

        if (vfxParticle != null)
        {
            vfxParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            vfxParticle.Play();
        }

        if (destroyAfterPlay)
        {
            StartCoroutine(DestroyAfterDelay());
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelayAfterPlay);
        if (vfxInstance != null)
        {
            Destroy(vfxInstance);
            vfxInstance = null;
            vfxParticle = null;
        }
    }

    private void OnDisable()
    {
        if (delayedSpawner != null) StopCoroutine(delayedSpawner);
        delayedSpawner = null;
    }
}