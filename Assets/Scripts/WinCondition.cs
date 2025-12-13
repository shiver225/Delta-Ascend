using System.Collections;
using UnityEngine;

public class WinCondition : MonoBehaviour
{
    public timer timerScript;
    public GameObject fireworksPrefab;
    public GameObject winStand;
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
            winStand.SetActive(true);
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
                                    Random.Range(-radius + 15f, radius + 15f),
                                    Random.Range(10f, radius + 15f),
                                    Random.Range(-radius + 15f, radius + 15f)
                                );

            GameObject fw = Instantiate(fireworksPrefab, randomPos, Quaternion.identity);
            SetRandomFireworkColor(fw);

            StartCoroutine(PlaySoundWithDelay(fw, 0.5f));

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
    IEnumerator PlaySoundWithDelay(GameObject fw, float delay)
    {
        yield return new WaitForSeconds(delay);

        AudioSource audio = fw.GetComponent<AudioSource>();
        if (audio != null)
            audio.Play();
    }


}
