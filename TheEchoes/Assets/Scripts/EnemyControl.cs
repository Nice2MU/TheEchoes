using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyControl : MonoBehaviour
{
    public bool loop = true;
    public Transform waypointContainer;
    public Animator animator;
    public string moveBoolName = "";

    [System.Serializable]
    public class WaypointData
    {
        public Transform pointTransform;
        public float moveSpeed = 2f;
        public float waitTime = 1f;
        public bool useWalkAnimation = false;
        public string animationTriggerName;
        public string soundName;
        public float duration = 1f;
        public Direction facingDirection = Direction.Right;
        public ExecutionMode executionMode = ExecutionMode.WhileMoving;
        public WaypointAction[] actions;
    }

    public enum ExecutionMode
    {
        WhileMoving,
        Sequential
    }

    public enum Direction
    {
        Right,
        Left
    }

    [System.Serializable]
    public class WaypointAction
    {
        public enum ActionType { None, PlayAnimation, PlaySound }

        public ActionType type;
        public string animationTriggerName;
        public string soundName;
        public float duration = 1f;
    }

    public List<WaypointData> waypoints = new List<WaypointData>();
    private int currentWaypointIndex = 0;

    void Start()
    {
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
            wp.waitTime = 1f;
            wp.useWalkAnimation = false;
            wp.duration = 1f;
            wp.moveSpeed = 2f;
            wp.facingDirection = Direction.Right;
            wp.executionMode = ExecutionMode.Sequential;
            wp.actions = new WaypointAction[0];
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

            if (wp.useWalkAnimation && animator && !string.IsNullOrEmpty(moveBoolName))
                animator.SetBool(moveBoolName, true);

            if (wp.executionMode == ExecutionMode.WhileMoving)
            {
                StartCoroutine(ExecuteActions(wp));
                yield return StartCoroutine(MoveTo(wp));
            }

            else
            {
                yield return StartCoroutine(MoveTo(wp));
                yield return StartCoroutine(ExecuteActions(wp));
            }

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

        while (Vector3.Distance(transform.position, target.position) > threshold)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, wp.moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = target.position;

        if (wp.useWalkAnimation && animator && !string.IsNullOrEmpty(moveBoolName))
            animator.SetBool(moveBoolName, false);
    }

    IEnumerator ExecuteActions(WaypointData wp)
    {
        if (wp.actions != null && wp.actions.Length > 0)
        {
            if (wp.executionMode == ExecutionMode.Sequential)
            {
                foreach (var action in wp.actions)
                {
                    HandleAction(action);
                    yield return new WaitForSeconds(action.duration);
                }
            }

            else
            {
                foreach (var action in wp.actions)
                {
                    HandleAction(action);
                }

                yield return new WaitForSeconds(wp.waitTime);
            }
        }
    }

    void HandleAction(WaypointAction action)
    {
        switch (action.type)
        {
            case WaypointAction.ActionType.PlayAnimation:
                if (animator != null && !string.IsNullOrEmpty(action.animationTriggerName))
                    animator.SetTrigger(action.animationTriggerName);
                break;

            case WaypointAction.ActionType.PlaySound:
                if (SoundManager.instance != null && !string.IsNullOrEmpty(action.soundName))
                    SoundManager.instance.PlaySFX(action.soundName);
                break;
        }
    }

    void FlipSprite(Direction dir)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dir == Direction.Left ? -1 : 1);
        transform.localScale = scale;
    }

#if UNITY_EDITOR
    [ContextMenu("Add Waypoint")]
    void AddWaypoint()
    {
        if (waypointContainer == null)
        {
            GameObject container = new GameObject("Waypoints");
            container.transform.SetParent(transform);
            waypointContainer = container.transform;
        }

        GameObject newPoint = new GameObject("Waypoint " + waypointContainer.childCount);
        newPoint.transform.SetParent(waypointContainer);
        newPoint.transform.position = transform.position;

        WaypointData newData = new WaypointData();
        newData.pointTransform = newPoint.transform;
        newData.waitTime = 1f;
        newData.moveSpeed = 2f;
        newData.facingDirection = Direction.Right;
        newData.executionMode = ExecutionMode.Sequential;
        newData.actions = new WaypointAction[0];
        waypoints.Add(newData);
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        Gizmos.color = Color.red;

        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i].pointTransform == null) continue;
            Gizmos.DrawSphere(waypoints[i].pointTransform.position, 0.2f);

            if (i < waypoints.Count - 1 && waypoints[i + 1].pointTransform != null)
                Gizmos.DrawLine(waypoints[i].pointTransform.position, waypoints[i + 1].pointTransform.position);

            else if (loop && waypoints[0].pointTransform != null)
                Gizmos.DrawLine(waypoints[i].pointTransform.position, waypoints[0].pointTransform.position);
        }
    }

    void OnValidate()
    {
        if (waypointContainer == null) return;

        for (int i = 0; i < waypoints.Count; i++)
        {
            var wp = waypoints[i];
            if (wp.pointTransform == null)
            {
                GameObject point = new GameObject($"Waypoint");
                point.transform.SetParent(waypointContainer);
                point.transform.position = transform.position;
                wp.pointTransform = point.transform;
            }
        }
    }
#endif
}