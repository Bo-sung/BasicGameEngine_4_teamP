using Unity.FPS.Gameplay;
using UnityEngine;

public class HomingRocket : ProjectileStandard
{
    [Header("Homing Settings")]
    public string TargetTag = "Enemy";
    public float TurnSpeed = 20f;

    Transform target;

    Vector3 m_lastPos;
    Vector3 m_velocity;

    void Start()
    {
        m_lastPos = transform.position;
        FindTarget();
    }

    void LateUpdate()
    {
        m_velocity = (transform.position - m_lastPos) / Time.deltaTime;
        m_lastPos = transform.position;

        if (target == null)
        {
            FindTarget();
            return;
        }

        Vector3 dir = (target.position - transform.position).normalized;

        float speed = m_velocity.magnitude;

        Vector3 newDir = Vector3.Lerp(m_velocity.normalized, dir, TurnSpeed * Time.deltaTime);

        m_velocity = newDir.normalized * speed * Time.deltaTime;

        transform.position += m_velocity;

        transform.forward = m_velocity.normalized;
    }

    void FindTarget()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag(TargetTag);
        if (objs.Length == 0)
        {
            target = null;
            return;
        }

        float closest = Mathf.Infinity;
        Transform closestTarget = null;

        foreach (var obj in objs)
        {
            float dist = Vector3.Distance(transform.position, obj.transform.position);
            if (dist < closest)
            {
                closest = dist;
                closestTarget = obj.transform;
            }
        }

        target = closestTarget;
    }
}
