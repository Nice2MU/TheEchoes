using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyControl : MonoBehaviour
{
    public bool loop = true;
    public Transform waypointContainer;
    public Animator animator;

    [Header("Animation Names")]
    public string moveBoolName = "";

    public enum EnemyType { Land, Water }

    [Header("Enemy Type")]
    public EnemyType enemyType = EnemyType.Land;

    [Header("Water Clamp")]
    public bool enableWaterClamp = false;
    public float waterMinY = -5f;
    public float waterMaxY = 5f;

    private string forcedAnimBool = null;
    private Rigidbody2D rb2d;
    private float originalGravity;
    private bool originalKinematic;

    [System.Serializable]
    public class WaypointData
    {
        public Transform pointTransform;
        public float moveSpeed = 2f;
        public float waitTime = 1f;

        [Header("Animation")]
        public bool useWalkAnimation = false;

        [Header("Facing")]
        public Direction facingDirection = Direction.Right;

        [Header("Execution")]
        public ExecutionMode executionMode = ExecutionMode.WhileMoving;

        [Header("Water Option")]
        public bool followY = false;

        public WaypointAction[] actions;
    }

    public enum ExecutionMode { WhileMoving, Sequential }
    public enum Direction { Right, Left }

    [System.Serializable]
    public class WaypointAction
    {
        public enum ActionType { None, PlayAnimation, PlaySound }
        public ActionType type;
        public string animationBoolName;
        public string soundName;
        public float duration = 0.5f;
    }

    public List<WaypointData> waypoints = new List<WaypointData>();
    private int currentWaypointIndex = 0;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            originalGravity = rb2d.gravityScale;
            originalKinematic = rb2d.isKinematic;
        }

        if (waypoints.Count == 0 && waypointContainer != null)
            AutoGenerateWaypoints();

        StartCoroutine(FollowPath());
    }

    void AutoGenerateWaypoints()
    {
        waypoints.Clear();
        foreach (Transform child in waypointContainer)
        {
            WaypointData wp = new WaypointData();
            wp.pointTransform = child;
            waypoints.Add(wp);
        }
    }

    IEnumerator FollowPath()
    {
        while (true)
        {
            if (waypoints.Count == 0) yield break;

            WaypointData wp = waypoints[currentWaypointIndex];
            FlipSprite(wp.facingDirection);

            if (wp.useWalkAnimation) PlayWalk();
            else StopWalk();

            if (wp.executionMode == ExecutionMode.WhileMoving)
            {
                StartCoroutine(ExecuteActions(wp, wp.useWalkAnimation));
                yield return StartCoroutine(MoveTo(wp));
            }
            else
            {
                yield return StartCoroutine(MoveTo(wp));
                yield return StartCoroutine(ExecuteActions(wp, wp.useWalkAnimation));
            }

            if (wp.waitTime > 0)
                yield return new WaitForSeconds(wp.waitTime);

            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
            {
                if (loop)
                    currentWaypointIndex = 0;
                else
                    break;
            }
        }
    }

    IEnumerator MoveTo(WaypointData wp)
    {
        Transform target = wp.pointTransform;
        float threshold = 0.1f;

        bool tempOverrideWater = false;
        if (enemyType == EnemyType.Water && wp.followY && rb2d != null)
        {
            rb2d.linearVelocity = Vector2.zero;
            rb2d.gravityScale = 0f;
            rb2d.isKinematic = true;
            tempOverrideWater = true;
        }

        float lockedWaterY = transform.position.y;
        bool lockWaterY = (enemyType == EnemyType.Water && !wp.followY);

        while (true)
        {
            Vector3 targetPos = target.position;

            if (enemyType == EnemyType.Water)
            {
                if (wp.followY)
                {
                    if (enableWaterClamp)
                        targetPos.y = Mathf.Clamp(targetPos.y, waterMinY, waterMaxY);
                }
                else
                {
                    targetPos = new Vector3(target.position.x, lockedWaterY, transform.position.z);
                }
            }

            if (Vector3.Distance(transform.position, targetPos) <= threshold)
            {
                transform.position = targetPos;
                break;
            }

            Vector3 newPos = Vector3.MoveTowards(transform.position, targetPos, wp.moveSpeed * Time.deltaTime);
            if (lockWaterY) newPos.y = lockedWaterY;
            transform.position = newPos;

            yield return null;
        }

        if (tempOverrideWater && rb2d != null)
        {
            rb2d.isKinematic = originalKinematic;
            rb2d.gravityScale = originalGravity;
        }

        if (wp.executionMode != ExecutionMode.WhileMoving && wp.useWalkAnimation)
            StopWalk();
    }

    IEnumerator ExecuteActions(WaypointData wp, bool shouldReturnToWalk)
    {
        if (wp.actions != null && wp.actions.Length > 0)
        {
            if (wp.executionMode == ExecutionMode.Sequential)
            {
                foreach (var a in wp.actions)
                {
                    HandleAction(a);
                    if (a.duration > 0)
                        yield return new WaitForSeconds(a.duration);
                }
            }
            else
            {
                foreach (var a in wp.actions)
                    HandleAction(a);
            }
        }

        if (wp.executionMode != ExecutionMode.WhileMoving)
        {
            if (shouldReturnToWalk) PlayWalk();
            else StopWalk();
        }
    }

    void HandleAction(WaypointAction a)
    {
        switch (a.type)
        {
            case WaypointAction.ActionType.PlayAnimation:
                if (animator != null && !string.IsNullOrEmpty(a.animationBoolName))
                {
                    ClearForcedAnimation();
                    animator.SetBool(a.animationBoolName, true);
                    forcedAnimBool = a.animationBoolName;
                }
                break;
            case WaypointAction.ActionType.PlaySound:
                if (SoundManager.instance != null && !string.IsNullOrEmpty(a.soundName))
                    SoundManager.instance.PlaySFX(a.soundName);
                break;
        }
    }

    void PlayWalk()
    {
        if (animator == null) return;
        ClearForcedAnimation();
        if (!string.IsNullOrEmpty(moveBoolName))
            animator.SetBool(moveBoolName, true);
    }

    void StopWalk()
    {
        if (animator == null) return;
        if (!string.IsNullOrEmpty(moveBoolName))
            animator.SetBool(moveBoolName, false);
    }

    void ClearForcedAnimation()
    {
        if (animator != null && !string.IsNullOrEmpty(forcedAnimBool))
            animator.SetBool(forcedAnimBool, false);
        forcedAnimBool = null;
    }

    void FlipSprite(Direction dir)
    {
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (dir == Direction.Left ? -1 : 1);
        transform.localScale = s;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        Gizmos.color = Color.red;

        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i].pointTransform == null) continue;

            Gizmos.DrawSphere(waypoints[i].pointTransform.position, 0.2f);

            if (i < waypoints.Count - 1 && waypoints[i + 1].pointTransform != null)
                Gizmos.DrawLine(waypoints[i].pointTransform.position,
                                waypoints[i + 1].pointTransform.position);
            else if (loop && waypoints[0].pointTransform != null)
                Gizmos.DrawLine(waypoints[i].pointTransform.position,
                                waypoints[0].pointTransform.position);
        }
    }
#endif
}