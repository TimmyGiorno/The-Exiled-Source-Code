using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; 
    public Vector3 offset = new Vector3(8f, 12f, -8f); 

    void LateUpdate() 
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: Target not set!");
            return;
        }

        transform.position = target.position + offset;

        transform.LookAt(target);
    }
}