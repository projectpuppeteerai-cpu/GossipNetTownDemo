using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 3.5f;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private Rigidbody2D _rigidbody;
    private Vector2 _moveInput;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            _moveInput = Vector2.zero;
            return;
        }

        float x = 0f;
        float y = 0f;
        if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) x -= 1f;
        if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) x += 1f;
        if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed) y += 1f;
        if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) y -= 1f;

        _moveInput = new Vector2(x, y).normalized;

        if (_spriteRenderer != null && Mathf.Abs(x) > 0.01f)
            _spriteRenderer.flipX = x < 0f;
    }

    private void FixedUpdate()
    {
        _rigidbody.MovePosition(_rigidbody.position + _moveInput * _moveSpeed * Time.fixedDeltaTime);
    }
}
