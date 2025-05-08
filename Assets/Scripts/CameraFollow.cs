using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // 玩家（小球）的 Transform 组件
    public Vector3 offset = new Vector3(8f, 12f, -8f); // 相机相对于目标的固定偏移量
    // X负值: 左边, Y正值: 上方, Z负值: 后方 (形成左上后方的视角)

    void LateUpdate() // 使用 LateUpdate 确保目标对象已经完成了它在当前帧的所有移动计算
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: Target (小球) 没有被设定!");
            return;
        }

        // 直接计算并设置相机的位置
        transform.position = target.position + offset;

        // 使相机始终朝向目标 (小球)
        transform.LookAt(target);
    }
}