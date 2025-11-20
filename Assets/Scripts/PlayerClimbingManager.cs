using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;
using UnityEditor.Rendering.LookDev;



public class PlayerClimbingManager : MonoBehaviour
{
    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.2f;
    public bool isGrounded;

    [Header("Climbing")]
    private int climbingHands = 0;
    public bool isClimbing => climbingInteractors.Count > 0;
    public float stamina = 100f;
    public float maxStamina = 100f;
    public float staminaDrainRate = 10f; // per second
    public ClimbProvider climbProvider;
    private List<IXRSelectInteractor> climbingInteractors = new List<IXRSelectInteractor>();


    [Header("Fall Damage")]
    public float fallHeightThreshold = 3.0f;
    private float highestPoint;
    public int maxHearts = 3;
    public int currentHearts = 3;

    [Header("Fade Panel")]
    
    public float fadeDuration = 1.5f;
    public bool isDead = false;

    [Header("Stamina Regeneration")]
    public float staminaRegenRate = 15f; // per second

    [Header("Haptics")]
    public float lowStaminaThreshold = 20f;
    public float vibrationIntensity = 0.3f; // 0–1
    public float vibrationDuration = 0.1f; // seconds
    private float nextHapticTime = 0f;



    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        highestPoint = transform.position.y;
    }

    void Update()
    {
        UpdateGroundCheck();
        HandleClimbingStamina();
        TrackFallHeight();
    }

    void UpdateGroundCheck()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    public Slider staminaSlider;

    void HandleClimbingStamina()
    {
        if (isClimbing && !isGrounded)
        {
            stamina -= staminaDrainRate * Time.deltaTime;
            stamina = Mathf.Clamp(stamina, 0, maxStamina);

            // HAPTICS WHEN LOW
            if (stamina <= lowStaminaThreshold)
                SendLowStaminaHaptics();

            // Auto-fall when stamina hits zero
            if (stamina <= 0f)
                StartCoroutine(AutoFall());
        }
        else
        {
            stamina += staminaRegenRate * Time.deltaTime;
        }

        stamina = Mathf.Clamp(stamina, 0f, 100f);

        // Update stamina slider (0–1)
        if (staminaSlider != null)
            staminaSlider.value = stamina / 100f;
    }
    void SendLowStaminaHaptics()
    {
        if (Time.time < nextHapticTime)
            return;

        foreach (var interactor in climbingInteractors)
        {
            // Convert to controller
            var controller = (interactor as XRBaseInteractor)?.GetComponent<XRBaseController>();
            if (controller != null)
                controller.SendHapticImpulse(vibrationIntensity, vibrationDuration);
        }

        nextHapticTime = Time.time + 0.3f; // limit vibration to every 0.3 sec
    }


    public RawImage heartLeft;
    public RawImage heartMiddle;
    public RawImage heartRight;

    void TrackFallHeight()
    {
        if (!isGrounded)
        {
            highestPoint = Mathf.Max(highestPoint, transform.position.y);
        }
        else
        {
            float fallDistance = highestPoint - transform.position.y;

            if (fallDistance > fallHeightThreshold)
            {
                if(currentHearts == 3)
                {
                    currentHearts = Mathf.Max(currentHearts - 1, 0);
                    heartLeft.gameObject.SetActive(false);
                    Debug.Log("Fall damage! Hearts left: " + currentHearts);
                }
                else if(currentHearts == 2)
                {
                    currentHearts = Mathf.Max(currentHearts - 1, 0);
                    heartMiddle.gameObject.SetActive(false);
                    Debug.Log("Fall damage! Hearts left: " + currentHearts);
                }
                else
                {
                    currentHearts = Mathf.Max(currentHearts - 1, 0);
                    heartRight.gameObject.SetActive(false);
                    Debug.Log("Fall damage! Hearts left: " + currentHearts);
                    StartCoroutine(DeathSequence());
                }
                
            }

            highestPoint = transform.position.y;
        }
    }
    private System.Collections.IEnumerator DeathSequence()
    {
        isDead = true;

        // Fade out
        float t = 0;
        

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
           
            yield return null;
        }

        // Reload scene (or load any other)
        SceneManager.LoadScene("DeathScene");
    }

    // Call this from your climbing grab script:
    public void SetClimbing(bool climbing)
    {
        if (climbing)
            climbingHands++;
        else
            climbingHands = Mathf.Max(climbingHands - 1, 0);
    }
    public void RegisterClimb(IXRSelectInteractor interactor)
    {
        if (!climbingInteractors.Contains(interactor))
            climbingInteractors.Add(interactor);
    }

    public void UnregisterClimb(IXRSelectInteractor interactor)
    {
        climbingInteractors.Remove(interactor);
    }

    IEnumerator AutoFall()
    {
        stamina = 0f;

        // Make a copy so we can edit the list safely
        var interactorsCopy = new List<IXRSelectInteractor>(climbingInteractors);

        foreach (var interactor in interactorsCopy)
        {
            // Convert IXRSelectInteractor → XRBaseInteractor to access interactionManager
            var baseInteractor = interactor as XRBaseInteractor;
            if (baseInteractor != null && baseInteractor.interactionManager != null)
            {
                // Correct modern call:
                baseInteractor.interactionManager.CancelInteractorSelection(interactor);
            }
        }

        // Fully clear climbing state
        climbingInteractors.Clear();

        // Disable climb so player can't instantly grab again while falling
        if (climbProvider != null)
            climbProvider.enabled = false;

        yield return new WaitForSeconds(0.5f);

        if (climbProvider != null)
            climbProvider.enabled = true;
    }

}

