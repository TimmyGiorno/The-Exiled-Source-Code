using UnityEngine;
using UnityEngine.InputSystem; 

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 7f;        
    public float verticalSpeed = 5f;   

    private Vector2 _moveInputXZ;       
    private PlayerInput _playerInput;   
    private InputAction _ascendAction; 
    private InputAction _descendAction; 

    void Awake() 
    {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput != null)
        {
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

    public void OnMoveInput(InputAction.CallbackContext context)
    {
        _moveInputXZ = context.ReadValue<Vector2>();
    }

    void Update()
    {
        Vector3 movementDirectionXZ = new Vector3(_moveInputXZ.x, 0f, _moveInputXZ.y);

        if (movementDirectionXZ.sqrMagnitude > 1f) 
        {
            movementDirectionXZ.Normalize();
        }
        Vector3 finalDisplacementXZ = movementDirectionXZ * (moveSpeed * Time.deltaTime);

        float verticalInputAmount = 0f;
        if (_ascendAction != null && _ascendAction.IsPressed()) 
        {
            verticalInputAmount += 1f;
        }
        if (_descendAction != null && _descendAction.IsPressed())
        {
            verticalInputAmount -= 1f;
        }
        Vector3 finalDisplacementY = new Vector3(0f, verticalInputAmount * verticalSpeed * Time.deltaTime, 0f);

        transform.Translate(finalDisplacementXZ + finalDisplacementY, Space.World);
    }
}