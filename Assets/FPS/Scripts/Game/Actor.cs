using UnityEngine;

/// <summary>
/// 게임 내 액터(플레이어, 적)의 기본 정보를 관리하는 컴포넌트. 소속과 조준점을 정의하여 AI 탐지 및 적/아군 판별에 사용.
/// </summary>
public class Actor : MonoBehaviour
{
    // Represents the affiliation (or team) of the actor. Actors of the same affiliation are friendly to each other
    /// <summary>
    /// 액터 소속
    /// </summary>
    [Tooltip("Actor's team or affiliation")]
    public int affiliation;

    // Represents point where other actors will aim when they attack this actor
    /// <summary>
    /// 조준 포인트
    /// </summary>
    [Tooltip("Point where enemies aim when attacking")]
    public Transform aimPoint;

    ActorsManager m_ActorsManager;

    void Start()
    {
        m_ActorsManager = GameObject.FindFirstObjectByType<ActorsManager>();
        DebugUtility.HandleErrorIfNullFindObject<ActorsManager, Actor>(m_ActorsManager, this);

        // 액터 등록
        if (!m_ActorsManager.Actors.Contains(this))
        {
            m_ActorsManager.Actors.Add(this);
        }
    }

    void OnDestroy()
    {
        // 액터 등록 해제
        if (m_ActorsManager)
        {
            m_ActorsManager.Actors.Remove(this);
        }
    }
}