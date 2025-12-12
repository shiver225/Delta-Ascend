using System.Collections;
using UnityEngine;

public class WinCondition : MonoBehaviour
{
    public timer timerScript;
    public GameObject fireworksPrefab;
    public float fireworkInterval = 0.7f;
    public float fireworkDuration = 10f;
    public float radius = 3f;

    private bool fireworkLoop = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("playerHand"))
        {
            timerScript.StopTimer();
            fireworkLoop = true;
            StartCoroutine(RandomFireworkShow());
            StartCoroutine(StopFireworksAfterDelay());
        }
    }
    private IEnumerator RandomFireworkShow()
    {
        while (fireworkLoop)
        {
            Vector3 randomPos = transform.position +
                                new Vector3(
                                    Random.Range(-radius, radius),
                                    Random.Range(2f, radius + 3f),
                                    Random.Range(-radius, radius)
                                );

            GameObject fw = Instantiate(fireworksPrefab, randomPos, Quaternion.identity);
            SetRandomFireworkColor(fw);

            yield return new WaitForSeconds(fireworkInterval);
        }
    }
    private IEnumerator StopFireworksAfterDelay()
    {
        yield return new WaitForSeconds(fireworkDuration);
        fireworkLoop = false;
    }
    void SetRandomFireworkColor(GameObject fireworkObj)
    {
        // Get the Particle System (main explosion)
        ParticleSystem ps = fireworkObj.GetComponent<ParticleSystem>();
        if (ps == null) return;

        var main = ps.main;
        main.startColor = GetRandomColor();
    }
    Color GetRandomColor()
    {
        // Bright color palette for fireworks
        float h = Random.Range(0f, 1f);      // Hue
        float s = Random.Range(0.8f, 1f);    // Saturation
        float v = Random.Range(0.8f, 1f);    // Brightness

        return Color.HSVToRGB(h, s, v);
    }


}
