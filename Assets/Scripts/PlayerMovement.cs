using UnityEngine;
using UnityEngine.InputSystem; // 确保引入了新的输入系统命名空间

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 7f;        // 水平（XZ平面）移动速度
    public float verticalSpeed = 5f;    // 垂直（Y轴）移动速度

    private Vector2 _moveInputXZ;       // 用于存储来自 OnMoveInput 事件的 XZ 平面输入
    private PlayerInput _playerInput;   // PlayerInput 组件的引用
    private InputAction _ascendAction;  // "Ascend" 输入动作的引用
    private InputAction _descendAction; // "Descend" 输入动作的引用

    void Awake() // Awake 在 Start 之前执行，适合获取组件引用
    {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput != null)
        {
            // "Move" 动作的输入通过 PlayerInput 组件的 Unity Event 调用 OnMoveInput 方法来处理
            // 我们需要获取 "Ascend" 和 "Descend" 动作的引用，以便在 Update 中轮询它们的状态
            // 请确保这里的字符串 "Ascend" 和 "Descend" 与你在 PlayerControls.inputactions 文件中定义的 Action 名称完全一致
            _ascendAction = _playerInput.actions["Ascend"];
            _descendAction = _playerInput.actions["Descend"];

            if (_ascendAction == null || _descendAction == null)
            {
                Debug.LogError("Ascend or Descend action not found in PlayerInput actions. Check action names in PlayerControls asset!");
            }
        }
        else
        {
            Debug.LogError("PlayerInput component not found on this GameObject! Vertical movement will not work.");
        }
    }

    // 这个方法由 PlayerInput 组件为 "Move" 动作配置的 Unity Event 调用
    public void OnMoveInput(InputAction.CallbackContext context)
    {
        _moveInputXZ = context.ReadValue<Vector2>();
    }

    void Update()
    {
        // --- 水平 (XZ平面) 移动 ---
        Vector3 movementDirectionXZ = new Vector3(_moveInputXZ.x, 0f, _moveInputXZ.y);

        // 如果同时按下两个方向键（例如W和A），向量长度会大于1，需要归一化以保证斜向移动速度一致
        if (movementDirectionXZ.sqrMagnitude > 1f) // 使用 sqrMagnitude 效率略高
        {
            movementDirectionXZ.Normalize();
        }
        Vector3 finalDisplacementXZ = movementDirectionXZ * (moveSpeed * Time.deltaTime);

        // --- 垂直 (Y轴) 移动 ---
        float verticalInputAmount = 0f;
        if (_ascendAction != null && _ascendAction.IsPressed()) // IsPressed() 检查按键是否当前被按下
        {
            verticalInputAmount += 1f;
        }
        if (_descendAction != null && _descendAction.IsPressed())
        {
            verticalInputAmount -= 1f;
        }
        Vector3 finalDisplacementY = new Vector3(0f, verticalInputAmount * verticalSpeed * Time.deltaTime, 0f);

        // --- 应用组合后的位移 ---
        // 将水平位移和垂直位移相加，然后应用到 Transform
        transform.Translate(finalDisplacementXZ + finalDisplacementY, Space.World);
    }
}