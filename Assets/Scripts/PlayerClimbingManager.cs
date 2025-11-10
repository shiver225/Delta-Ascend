using UnityEngine;
using UnityEngine.UI;

public class PlayerClimbingManager : MonoBehaviour
{
    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.2f;
    public bool isGrounded;

    [Header("Climbing")]
    public bool isClimbing = false;
    public float stamina = 100f;
    public float staminaDrainRate = 10f; // per second

    [Header("Fall Damage")]
    public float fallHeightThreshold = 3.0f;
    private float highestPoint;
    public int hearts = 3;

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
            stamina = Mathf.Clamp(stamina, 0, 100);
        }

        // Update stamina slider (0–100)
        if (staminaSlider != null)
            staminaSlider.value = stamina / 100f;
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
                if(hearts == 3)
                {
                    hearts = Mathf.Max(hearts - 1, 0);
                    heartLeft.gameObject.SetActive(false);
                    Debug.Log("Fall damage! Hearts left: " + hearts);
                }
                else if(hearts == 2)
                {
                    hearts = Mathf.Max(hearts - 1, 0);
                    heartMiddle.gameObject.SetActive(false);
                    Debug.Log("Fall damage! Hearts left: " + hearts);
                }
                else
                {
                    hearts = Mathf.Max(hearts - 1, 0);
                    heartRight.gameObject.SetActive(false);
                    Debug.Log("Fall damage! Hearts left: " + hearts);
                }
                
            }

            highestPoint = transform.position.y;
        }
    }

    // Call this from your climbing grab script:
    public void SetClimbing(bool climbing)
    {
        isClimbing = climbing;
    }
}
