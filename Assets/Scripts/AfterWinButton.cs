using UnityEngine;
using UnityEngine.SceneManagement;

public class AfterWinButton : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger entered by: " + other.name);

        if (other.CompareTag("playerHand"))
        {
            Debug.Log("Hand detected. Changing scene...");
            SceneManager.LoadScene("StartScene");
        }
    }
}
