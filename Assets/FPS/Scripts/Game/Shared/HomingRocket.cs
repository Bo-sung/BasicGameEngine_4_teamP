using Unity.FPS.Gameplay;
using UnityEngine;

public class HomingRocket : ProjectileStandard
{
    [Header("Homing Settings")]
    public string targetTag = "Enemy";
    public float turnSpeed = 20f;
    public float detectionAngle = 45f;
    public float detectionRange = 50f;

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

        Vector3 newDir = Vector3.Lerp(m_velocity.normalized, dir, turnSpeed * Time.deltaTime);

        m_velocity = newDir.normalized * speed * Time.deltaTime;

        transform.position += m_velocity;

        transform.forward = m_velocity.normalized;
    }

    void FindTarget()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag(targetTag);
        if (objs.Length == 0)
        {
            target = null;
            return;
        }

        float closest = Mathf.Infinity;
        Transform closestTarget = null;

        foreach (var obj in objs)
        {
            Vector3 toTarget = obj.transform.position - transform.position;
            float dist = toTarget.magnitude;

            if (dist > detectionRange)
                continue;

            //각도 제한
            float angle = Vector3.Angle(transform.forward, toTarget);
            if (angle > detectionAngle)
                continue;

            if (dist < closest)
            {
                closest = dist;
                closestTarget = obj.transform;
            }
        }

        target = closestTarget;
    }
}
