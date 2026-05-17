using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float stopDistance = 0.35f;

    [Header("Visual Model")]
    public Transform modelTransform;
    public Animator modelAnimator;

    private Vector3 targetPosition;
    private bool moving = false;

    void Start()
    {
        targetPosition = transform.position;

        if (modelAnimator == null && modelTransform != null)
            modelAnimator = modelTransform.GetComponent<Animator>();

        UpdateAnimation(false, 0f);
    }

    void Update()
    {
        HandleMovementInput();
        MoveToTarget();
    }

    void HandleMovementInput()
    {
        if (MenuTabController.IsMenuOpen)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                targetPosition = ray.GetPoint(distance);
                moving = true;
            }
        }
    }

    void MoveToTarget()
    {
        if (!moving)
        {
            UpdateAnimation(false, 0f);
            return;
        }

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance > stopDistance)
        {
            Vector3 moveDirection = direction.normalized;
            transform.position += moveDirection * moveSpeed * Time.deltaTime;

            RotateVisual(moveDirection);
            UpdateAnimation(true, moveSpeed);
        }
        else
        {
            StopMovement();
        }
    }

    public void StopMovement()
    {
        moving = false;
        targetPosition = transform.position;
        UpdateAnimation(false, 0f);
    }

    public void RotateVisual(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        if (modelTransform != null)
            modelTransform.rotation = targetRotation;
        else
            transform.rotation = targetRotation;
    }

    void UpdateAnimation(bool isMoving, float speed)
    {
        if (modelAnimator == null)
            return;

        modelAnimator.SetBool("IsMoving", isMoving);
        modelAnimator.SetFloat("MoveSpeed", speed);
    }
}