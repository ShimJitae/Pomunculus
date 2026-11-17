using UnityEngine;

/// <summary>메인 카메라 화면 안에 있을 때만 CullingManager에게 CullingTick을 받는 대상의 계약</summary>
public interface ICullingTickable
{
    /// <summary>true면 CullingManager가 매 프레임 판정용 구를 다시 계산하고, false면 등록할 때 한 번만 계산한다</summary>
    bool IsMovingObject { get; }

    /// <summary>화면 안 판정에 쓸 월드 공간의 구를 반환한다</summary>
    BoundingSphere GetCullingSphere();

    /// <summary>화면 안에 있는 동안 매 프레임 호출된다. Update 대신 쓴다</summary>
    void CullingTick(float now);
}
