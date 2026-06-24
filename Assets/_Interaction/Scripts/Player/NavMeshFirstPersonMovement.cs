using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class NavMeshFirstPersonMovement : FirstPersonMovement
{
    private const float MinInputSqr = 0.0001f;
    private const float MinHorizontalMoveSqr = 0.000001f;

    [Header("NavMesh Movement")]
    [SerializeField, Min(0.01f)] private float navMeshSampleRadius = 1.5f;
    [SerializeField] private int navMeshAreaMask = -1;
    [SerializeField] private bool preserveInitialHeightOffset = true;
    [SerializeField] private bool moveToBoundaryWhenBlocked = true;
    [SerializeField, Min(0f)] private float maxVerticalSnapHeight = 0.35f;
    [SerializeField, Min(0f)] private float maxSlopeVerticalDeltaPerMeter = 1.5f;

    private Vector3 navMeshVelocity;
    private bool heightOffsetInitialized;
    private float navMeshHeightOffset;
    private bool hasLastNavMeshPosition;
    private Vector3 lastNavMeshPosition;
    private bool missingNavMeshWarningShown;

    public override void TeleportTo(Transform target)
    {
        base.TeleportTo(target);
        ResetNavMeshBinding();
    }

    public override void TeleportTo(Vector3 targetPosition, Quaternion targetRotation, float cameraPitch)
    {
        base.TeleportTo(targetPosition, targetRotation, cameraPitch);
        ResetNavMeshBinding();
    }

    public override void ResetMovementState()
    {
        base.ResetMovementState();
        navMeshVelocity = Vector3.zero;
    }

    protected override void HandleMovement()
    {
        CharacterController characterController = MovementController;
        if (characterController == null || !characterController.enabled)
            return;

        Vector2 input = Vector2.ClampMagnitude(GetEffectiveMoveInput(), 1f);
        if (input.sqrMagnitude <= MinInputSqr)
        {
            navMeshVelocity = Vector3.zero;
            return;
        }

        Vector3 desiredDirection = GetDesiredDirection(input);
        Vector3 desiredVelocity = desiredDirection * moveSpeed;
        navMeshVelocity = Vector3.MoveTowards(
            navMeshVelocity,
            desiredVelocity,
            MovementAcceleration * Time.deltaTime);

        Vector3 currentPosition = transform.position;
        Vector3 desiredPosition = currentPosition + navMeshVelocity * Time.deltaTime;

        Vector3 horizontalDelta = desiredPosition - currentPosition;
        horizontalDelta.y = 0f;
        if (horizontalDelta.sqrMagnitude <= MinHorizontalMoveSqr)
            return;

        if (!TryConstrainToNavMesh(currentPosition, desiredPosition, out Vector3 constrainedPosition))
        {
            navMeshVelocity = Vector3.zero;
            WarnMissingNavMesh();
            return;
        }

        characterController.Move(constrainedPosition - currentPosition);
    }

    protected override void ApplyGrounding()
    {
        // Vertical placement is driven by the NavMesh surface when there is horizontal movement.
    }

    private Vector3 GetDesiredDirection(Vector2 input)
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return (forward * input.y + right * input.x).normalized;
    }

    private bool TryConstrainToNavMesh(Vector3 currentPosition, Vector3 desiredPosition, out Vector3 constrainedPosition)
    {
        constrainedPosition = currentPosition;

        if (!TryGetCurrentNavMeshPosition(currentPosition, out Vector3 currentNavMeshPosition))
            return false;

        Vector3 navMeshDesiredPosition = new Vector3(
            desiredPosition.x,
            currentNavMeshPosition.y,
            desiredPosition.z);

        Vector3 navMeshPosition;
        if (NavMesh.Raycast(currentNavMeshPosition, navMeshDesiredPosition, out NavMeshHit blockHit, navMeshAreaMask))
        {
            if (!moveToBoundaryWhenBlocked)
                return false;

            navMeshPosition = blockHit.position;
        }
        else
        {
            if (!NavMesh.SamplePosition(navMeshDesiredPosition, out NavMeshHit targetHit, navMeshSampleRadius, navMeshAreaMask))
                return false;

            navMeshPosition = targetHit.position;
        }

        if (!IsContinuousNavMeshStep(currentNavMeshPosition, navMeshPosition))
        {
            navMeshVelocity = Vector3.zero;
            return true;
        }

        constrainedPosition = ToBodyPosition(navMeshPosition);
        CacheLastNavMeshPosition(navMeshPosition);
        return true;
    }

    private bool TryGetCurrentNavMeshPosition(Vector3 currentPosition, out Vector3 navMeshPosition)
    {
        navMeshPosition = default;

        if (!NavMesh.SamplePosition(currentPosition, out NavMeshHit currentHit, navMeshSampleRadius, navMeshAreaMask))
        {
            if (hasLastNavMeshPosition)
            {
                navMeshPosition = lastNavMeshPosition;
                return true;
            }

            return false;
        }

        Vector3 candidatePosition = currentHit.position;
        if (hasLastNavMeshPosition && !IsContinuousNavMeshStep(lastNavMeshPosition, candidatePosition))
        {
            navMeshPosition = lastNavMeshPosition;
            return true;
        }

        if (!heightOffsetInitialized)
        {
            navMeshHeightOffset = preserveInitialHeightOffset ? currentPosition.y - candidatePosition.y : 0f;
            heightOffsetInitialized = true;
        }

        navMeshPosition = candidatePosition;
        CacheLastNavMeshPosition(candidatePosition);
        missingNavMeshWarningShown = false;
        return true;
    }

    private Vector3 ToBodyPosition(Vector3 navMeshPosition)
    {
        return new Vector3(
            navMeshPosition.x,
            navMeshPosition.y + navMeshHeightOffset,
            navMeshPosition.z);
    }

    private bool IsContinuousNavMeshStep(Vector3 from, Vector3 to)
    {
        float horizontalDistance = Vector2.Distance(
            new Vector2(from.x, from.z),
            new Vector2(to.x, to.z));

        float verticalDistance = Mathf.Abs(to.y - from.y);
        float allowedVerticalDistance = maxVerticalSnapHeight + maxSlopeVerticalDeltaPerMeter * horizontalDistance;
        return verticalDistance <= allowedVerticalDistance;
    }

    private void CacheLastNavMeshPosition(Vector3 navMeshPosition)
    {
        lastNavMeshPosition = navMeshPosition;
        hasLastNavMeshPosition = true;
    }

    private void ResetNavMeshBinding()
    {
        heightOffsetInitialized = false;
        hasLastNavMeshPosition = false;
        missingNavMeshWarningShown = false;
        navMeshVelocity = Vector3.zero;
    }

    private void WarnMissingNavMesh()
    {
        if (missingNavMeshWarningShown)
            return;

        missingNavMeshWarningShown = true;
        Debug.LogWarning(
            "[MalagaParking] No NavMesh found near the player. Movement is locked until the player is placed on a baked NavMesh.",
            this);
    }
}
